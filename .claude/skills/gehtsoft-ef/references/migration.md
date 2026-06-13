# Schema Migration Reference

## UpdateTables — The Core Migration Method

```csharp
var controller = new CreateEntityController(typeof(MyEntity), "myapp");
controller.UpdateTables(connection, CreateEntityController.UpdateMode.Update);
```

The controller discovers all entity classes in the assembly, filtered by scope, then compares
them against the live database schema via `connection.Schema()`.

### Update Modes

| Mode | Creates new tables | Adds new columns | Drops obsolete columns | Drops obsolete tables | Recreates existing |
|------|:-:|:-:|:-:|:-:|:-:|
| `Update` | yes | yes | yes (if DB supports it) | yes | no |
| `CreateNew` | yes | no | no | no | no |
| `Recreate` | yes | n/a | n/a | yes | yes (drops + creates) |

### Per-Entity Mode Overrides

```csharp
var overrides = new Dictionary<Type, CreateEntityController.UpdateMode>
{
    { typeof(TempCache), CreateEntityController.UpdateMode.Recreate }
};
controller.UpdateTables(connection, CreateEntityController.UpdateMode.Update, overrides);
```

**Constraint:** You cannot set Recreate on a parent table while a child table (that references it
via FK) is in Update mode. This would break referential integrity. The controller throws
`EfSqlException` with code `CannotRecreateTable` in this case.

### Async Versions

```csharp
await controller.UpdateTablesAsync(connection, CreateEntityController.UpdateMode.Update);
await controller.CreateTablesAsync(connection);
await controller.DropTablesAsync(connection);
```

## Obsolete Entities (Dropping Tables)

Mark an entity class with `[ObsoleteEntity]` to have its table dropped during migration:

```csharp
// The table "old_cache" will be dropped on next UpdateTables call
[ObsoleteEntity(Table = "old_cache", Scope = "myapp")]
public class OldCache { }
```

The `Table` and `Scope` must match the original `[Entity]` attribute values exactly.

The controller drops obsolete tables in reverse dependency order (children first).
If other active tables have FK references to the obsolete table, the drop may be blocked
depending on whether the database supports column drops.

## Obsolete Properties (Dropping Columns)

Mark a property with `[ObsoleteEntityProperty]` to drop its column:

```csharp
[Entity(Scope = "myapp", Table = "products")]
public class Product
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 256)]
    public string Name { get; set; }

    // This column will be dropped on next UpdateTables
    [ObsoleteEntityProperty(Field = "old_description")]
    public string OldDescription { get; set; }

    // If the obsolete column had an index, specify it:
    [ObsoleteEntityProperty(Field = "legacy_code", Sorted = true)]
    public string LegacyCode { get; set; }

    // If the obsolete column was a foreign key, specify it:
    [ObsoleteEntityProperty(Field = "old_category", ForeignKey = true)]
    public object OldCategory { get; set; }
}
```

The `Field` value must match the original column name. If the column had `Sorted = true` or
`ForeignKey = true`, those flags must be set on the obsolete attribute so the controller
can properly clean up indexes and constraints before dropping.

### Database Support for Column Drops

| Database | Drop column supported | Notes |
|----------|:---------------------:|-------|
| SQL Server | yes | Drops indexes and FK constraints automatically |
| PostgreSQL | yes | Standard DROP COLUMN |
| MySQL | yes | Drops FK constraints before column drop |
| Oracle | yes | Also drops sequences for autoincrement columns |
| **SQLite** | **no** | Throws `FeatureNotSupported` |

When `DropColumnSupported` is false (SQLite), the controller silently skips column drops.
The column remains in the table but is ignored by entity queries.

## Adding New Columns

When you add a new `[EntityProperty]` to an existing entity, `UpdateTables` with `Update` mode
detects the missing column and adds it via ALTER TABLE:

```csharp
[Entity(Scope = "myapp", Table = "products")]
public class Product
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 256)]
    public string Name { get; set; }

    // New column — will be added automatically on UpdateTables
    [EntityProperty(DbType = DbType.Int32, Nullable = true)]
    public int? Rating { get; set; }
}
```

New columns should generally be `Nullable = true` or have a `DefaultValue`, since existing rows
won't have data for them.

## What UpdateTables Cannot Handle

The controller does **not** detect or handle:
- **Column type changes** — use a patch (drop old column + add new one)
- **Column renames** — use a patch (add new column, copy data, drop old column)
- **Primary key changes** — must recreate the table
- **New composite indexes on existing tables** — use a patch
- **View changes** — must drop and recreate the view

For these cases, use the **Patch mechanism** (see below).

## Lifecycle Callbacks

Four callback attributes fire at specific points during migration:

| Attribute | Targets | When fired |
|-----------|---------|-----------|
| `[OnEntityCreate]` | class | After table is CREATE'd |
| `[OnEntityDrop]` | class | Before table is DROP'd |
| `[OnEntityPropertyCreate]` | property | After column is ADD'd via ALTER TABLE |
| `[OnEntityPropertyDrop]` | property | After column is DROP'd via ALTER TABLE |

All callbacks reference a static method with signature `void Method(SqlDbConnection connection)`:

```csharp
[Entity(Scope = "myapp", Table = "products")]
public class Product
{
    [AutoId]
    public int Id { get; set; }

    // Populate default value after column is added to existing table
    [OnEntityPropertyCreate(typeof(ProductMigration), nameof(ProductMigration.SetDefaultRating))]
    [EntityProperty(DbType = DbType.Int32, Nullable = true)]
    public int? Rating { get; set; }

    // Clean up before column is dropped
    [ObsoleteEntityProperty(Field = "old_notes")]
    [OnEntityPropertyDrop(typeof(ProductMigration), nameof(ProductMigration.BackupNotes))]
    public string OldNotes { get; set; }
}

internal static class ProductMigration
{
    public static void SetDefaultRating(SqlDbConnection connection)
    {
        using var query = connection.GetQuery("UPDATE products SET rating = 0 WHERE rating IS NULL");
        query.ExecuteNoData();
    }

    public static void BackupNotes(SqlDbConnection connection)
    {
        // Archive data before column drop
    }
}
```

### Listening to Controller Events

For logging or progress tracking:
```csharp
controller.OnAction += (sender, args) =>
{
    Console.WriteLine($"{args.Action}: {args.EntityType?.Name ?? args.Table}");
};
```

## Patch Mechanism — Versioned Custom Migrations

For changes that `UpdateTables` cannot handle (type changes, data transformations, index
creation, etc.), use the patch system. Patches are versioned, tracked in a `ef_patch_history`
table, and applied only once.

### Defining a Patch

```csharp
[EfPatch("myapp", 1, 0, 1)]  // scope, major, minor, patch
public class RenameDescriptionColumn : IEfPatch
{
    public void Apply(SqlDbConnection connection)
    {
        // Add new column
        using (var q = connection.GetQuery(
            "ALTER TABLE products ADD COLUMN description VARCHAR(1024)"))
            q.ExecuteNoData();

        // Copy data from old column
        using (var q = connection.GetQuery(
            "UPDATE products SET description = old_description"))
            q.ExecuteNoData();

        // Old column will be dropped by [ObsoleteEntityProperty] on next UpdateTables
    }
}
```

For async patches:
```csharp
[EfPatch("myapp", 1, 0, 2)]
public class CreateFullTextIndex : IEfPatchAsync
{
    public void Apply(SqlDbConnection connection) => ApplyAsync(connection).Wait();

    public async Task ApplyAsync(SqlDbConnection connection)
    {
        // ... async migration logic
    }
}
```

### Applying Patches

```csharp
// Find all patch classes in the assembly, sorted by version
var patches = EfPatchProcessor.FindAllPatches(
    new[] { typeof(MyEntity).Assembly }, "myapp");

// Apply only patches newer than the last applied one
connection.ApplyPatches(patches, "myapp");

// Or async
await connection.ApplyPatchesAsync(patches, "myapp");
```

### How Patch Versioning Works

1. On first run, the processor creates the `ef_patch_history` table and records the latest
   patch version (without executing any patches — assumes fresh DB is up to date)
2. On subsequent runs, it reads the last applied version from `ef_patch_history`
3. Only patches with version **greater than** the last applied version are executed
4. Each applied patch is recorded in `ef_patch_history` with a timestamp
5. Patches are sorted by `major * 10,000,000 + minor * 10,000 + patch`

### DI Support in Patches

Patches can use dependency injection:
```csharp
[EfPatch("myapp", 1, 1, 0)]
public class MigrateData : IEfPatch
{
    private readonly ILogger _logger;

    public MigrateData(ILogger<MigrateData> logger)
    {
        _logger = logger;
    }

    public void Apply(SqlDbConnection connection)
    {
        _logger.LogInformation("Applying data migration...");
        // ...
    }
}

// Pass service provider when applying
connection.ApplyPatches(patches, "myapp", serviceProvider);
```

### Querying Patch History

```csharp
// Get last applied patch
EfPatchHistoryRecord last = connection.GetLastAppliedPatch("myapp");
Console.WriteLine($"Last patch: {last.MajorVersion}.{last.MinorVersion}.{last.PatchVersion}");

// Get all applied patches
var history = connection.GetAllPatches("myapp");
```

## SQLite Migration Workaround

SQLite does not support DROP COLUMN. For schema changes that require removing columns
on SQLite, use a patch that performs the table rebuild sequence:

```csharp
[EfPatch("myapp", 1, 0, 1)]
public class RebuildProductsTable : IEfPatch
{
    public void Apply(SqlDbConnection connection)
    {
        // 1. Rename old table
        using (var q = connection.GetQuery("ALTER TABLE products RENAME TO products_old"))
            q.ExecuteNoData();

        // 2. Create new table (without the dropped column)
        using (var q = connection.GetCreateEntityQuery<Product>())
            q.Execute();

        // 3. Copy data (list only the columns that remain)
        using (var q = connection.GetQuery(
            "INSERT INTO products (id, name, price) SELECT id, name, price FROM products_old"))
            q.ExecuteNoData();

        // 4. Drop old table
        using (var q = connection.GetQuery("DROP TABLE products_old"))
            q.ExecuteNoData();
    }
}
```

## Recommended Migration Workflow

1. **Initial setup:** `controller.CreateTables(connection)` or `UpdateTables` with `Update` mode
2. **Adding entities/columns:** Just add them to the code — `UpdateTables(Update)` handles it
3. **Removing columns:** Add `[ObsoleteEntityProperty]` — `UpdateTables(Update)` handles it
   (except on SQLite — use a patch)
4. **Removing entities:** Add `[ObsoleteEntity]` — `UpdateTables(Update)` drops the table
5. **Complex changes** (type changes, renames, data transforms): Write a patch
6. **Call order in your initialization:**
   ```csharp
   var controller = new CreateEntityController(typeof(MyEntity), "myapp");
   controller.UpdateTables(connection, CreateEntityController.UpdateMode.Update);
   
   var patches = EfPatchProcessor.FindAllPatches(new[] { typeof(MyEntity).Assembly }, "myapp");
   connection.ApplyPatches(patches, "myapp");
   ```

## FK Dependency Order

The controller automatically sorts entities by FK dependencies:
- **Create order:** referenced tables first (Category before Product)
- **Drop order:** dependent tables first (Product before Category)

This is handled by `EntityFinder.ArrangeEntities` which performs a topological sort
on the FK dependency graph.

