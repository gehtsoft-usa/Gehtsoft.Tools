# Entity Model Definition

Gehtsoft.EF maps .NET classes to database tables using attributes.

## Entity Class Definition

Apply `[Entity]` (from `Gehtsoft.EF.Entities`) to a class to mark it as a database entity.

| Property | Type | Description |
|----------|------|-------------|
| `Table` | string | Database table name. If omitted, derived from class name per NamingPolicy (pluralized by default). |
| `Scope` | string | Groups entities for `CreateEntityController` (e.g., `"myapp"`). |
| `NamingPolicy` | `EntityNamingPolicy` | Name generation for table/columns. Default = pluralize class name. |
| `View` | bool | If `true`, maps to a database view instead of a table. |
| `Metadata` | Type | Type implementing `ICompositeIndexMetadata` or `IViewCreationMetadata`. |

`EntityNamingPolicy` values: `Default`, `BackwardCompatibility`, `AsIs`, `LowerCase`, `UpperCase`, `LowerFirstCharacter`, `UpperFirstCharacter`, `LowerCaseWithUnderscores`, `UpperCaseWithUnderscopes`.

```csharp
using Gehtsoft.EF.Entities;
using System.Data;

[Entity(Scope = "myapp", Table = "customers")]
public class Customer
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(Field = "name", DbType = DbType.String, Size = 128, Sorted = true)]
    public string Name { get; set; }
}
```

## Property Attributes

### `[EntityProperty]`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Field` | string | (derived) | Column name. If omitted, derived from property name per NamingPolicy. |
| `DbType` | `System.Data.DbType` | `DbType.Object` | Column type. If `Object`, inferred from .NET type. |
| `Size` | int | 0 | Column size. Required for `String`. |
| `Precision` | int | 0 | Decimal places for numeric types. |
| `PrimaryKey` | bool | false | Marks column as primary key. |
| `Autoincrement` | bool | false | Auto-increment column. |
| `AutoId` | bool | false | Shorthand for PrimaryKey + Autoincrement. |
| `ForeignKey` | bool | false | Marks column as a foreign key reference. |
| `Sorted` | bool | false | Creates an index on the column. |
| `Unique` | bool | false | Adds a unique constraint. |
| `Nullable` | bool | false | Allows NULL. Also set automatically for nullable .NET types. |
| `DefaultValue` | object | null | Default value (primitive types only). |
| `IgnoreRead` | bool | false | Excludes property from "read all" queries. |

### Shorthand Attributes

- **`[AutoId]`** -- equivalent to `[EntityProperty(AutoId = true, DbType = DbType.Int32)]`. Sets PrimaryKey and Autoincrement.
- **`[ForeignKey]`** -- equivalent to `[EntityProperty(ForeignKey = true)]`.

### DbType to .NET Type Mapping

| DbType | .NET Type | Notes |
|--------|-----------|-------|
| `Int32` | `int` | |
| `Int64` | `long` | |
| `Double` | `double` | |
| `Decimal` | `decimal` | Use `Size` + `Precision` |
| `String` | `string` | Requires `Size` |
| `DateTime` | `DateTime` | Date + time |
| `Date` | `DateTime` | Date only |
| `Boolean` | `bool` | |
| `Guid` | `Guid` | |
| `Binary` | `byte[]` | |

When `DbType` is omitted (`DbType.Object`), the type is inferred from the .NET property type.

### Primary Key Variants

```csharp
// Auto-increment int (most common)
[AutoId]
public int Id { get; set; }

// Guid primary key (assigned manually or via GenericEntityAccessor.NewGuidKey)
[EntityProperty(PrimaryKey = true)]
public Guid Id { get; set; }

// String primary key
[EntityProperty(PrimaryKey = true, DbType = DbType.String, Size = 64)]
public string Code { get; set; }
```

### Non-Mapped Properties

Properties without `[EntityProperty]`, `[AutoId]`, or `[ForeignKey]` are not mapped to the
database. Use them for computed values:

```csharp
[Entity(Table = "deliveries")]
public class Delivery
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.Double)]
    public double GrossWeight { get; set; }

    [EntityProperty(DbType = DbType.Double)]
    public double TareWeight { get; set; }

    // Not mapped — computed from mapped properties
    public double NetWeight => GrossWeight - TareWeight;
}
```

## Relationships (Foreign Keys)

A foreign key is a property whose .NET type is another entity class. The framework stores the referenced entity's primary key in the column.

```csharp
[Entity(Scope = "myapp", Table = "orders")]
public class Order
{
    [AutoId]
    public int Id { get; set; }

    [ForeignKey]
    public Customer Customer { get; set; }

    [EntityProperty(DbType = DbType.DateTime)]
    public DateTime OrderDate { get; set; }
}
```

### Self-Referencing Foreign Keys

For trees, an entity can reference itself. Mark as `Nullable` since root nodes have no parent.

```csharp
[Entity(Table = "categories")]
public class Category
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 128)]
    public string Name { get; set; }

    [EntityProperty(ForeignKey = true, Nullable = true)]
    public Category Parent { get; set; }
}
```

**Creation order:** Referenced entities must exist before referencing ones.
`CreateEntityController` resolves this automatically.

## Schema Evolution

`[ObsoleteEntityProperty]` marks a column for drop. Props: `Field` (column name), `ForeignKey` (bool), `Sorted` (bool).

```csharp
[ObsoleteEntityProperty(Field = "old_column")]
public string OldColumn { get; set; }
```

`[ObsoleteEntity]` marks an entire entity for table drop. Same properties as `[Entity]`.

```csharp
[ObsoleteEntity(Table = "old_table", Scope = "myapp")]
public class OldEntity { }
```

## Runtime Metadata

### AllEntities Registry

`AllEntities` is a global registry of all entity types discovered at runtime. Use it to get metadata about entities programmatically:

```csharp
using Gehtsoft.EF.Entities;

// Get the descriptor for an entity type
EntityDescriptor descriptor = AllEntities.Get<Product>();

// Check if a type is a registered entity
bool isEntity = AllEntities.Contains(typeof(Product));
```

### EntityDescriptor

`EntityDescriptor` provides runtime access to an entity's table name, columns, and relationships:

```csharp
EntityDescriptor descriptor = AllEntities.Get<Product>();

string tableName = descriptor.TableDescriptor.Name;

// Iterate columns
foreach (var column in descriptor.TableDescriptor)
{
    string columnName = column.Name;
    DbType dbType = column.DbType;
    bool isPK = column.PrimaryKey;
    bool isFK = column.ForeignKey;
    bool isNullable = column.Nullable;
}

// Access a specific column by entity property name
var priceColumn = descriptor[nameof(Product.Price)];
```

### TableDescriptor

`TableDescriptor` is the lower-level table schema used by `QueryBuilder`. Entity queries use `EntityDescriptor` (which wraps a `TableDescriptor`). See `references/raw-sql.md` Section 2 for `TableDescriptor` usage with `QueryBuilder`.

### EntityCollection<T>

`EntityCollection<T>` is the typed list returned by `ReadAll<T>()`. It extends `List<T>` with no additional members — use standard LINQ or list operations on it.

## Composite Indexes

Implement `ICompositeIndexMetadata` and reference via `Metadata` on `[Entity]`.
`CompositeIndex` supports plain columns, functions (`SqlFunctionId.Upper`), and sort direction.
Set `FailIfUnsupported = true` to throw if the database lacks function-index support.

```csharp
[Entity(Scope = "myapp", Table = "clients", Metadata = typeof(ClientMetadata))]
public class Client
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 256, Unique = true)]
    public string Name { get; set; }
}

public class ClientMetadata : ICompositeIndexMetadata
{
    public IEnumerable<CompositeIndex> Indexes
    {
        get
        {
            var index = new CompositeIndex("name_no_case");
            index.FailIfUnsupported = true;
            index.Add(SqlFunctionId.Upper, nameof(Client.Name));
            yield return index;
        }
    }
}
```

## Entity Lifecycle Callbacks

### `[OnEntityCreate]`

Called after table creation by `CreateEntityController`. Use for seed data.
Callback must match `delegate void EntityActionDelegate(SqlDbConnection connection)`.

```csharp
[OnEntityCreate(typeof(SeedData), nameof(SeedData.InsertDefaults))]
[Entity(Scope = "myapp", Table = "roles")]
public class Role
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 64)]
    public string Name { get; set; }
}

internal static class SeedData
{
    public static void InsertDefaults(SqlDbConnection connection)
    {
        using var query = connection.GetInsertEntityQuery<Role>();
        query.Execute(new Role { Name = "Admin" });
        query.Execute(new Role { Name = "User" });
    }
}
```

### `[OnEntityDrop]`

Called before table drop by `CreateEntityController`. Same delegate signature.

## Complete Entity Model Example

```csharp
using Gehtsoft.EF.Entities;
using System.Data;

[Entity(Scope = "myapp", Table = "statuses")]
public class Status
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 32, Unique = true)]
    public string Name { get; set; }
}

[Entity(Scope = "myapp", Table = "categories")]
public class Category
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 128)]
    public string Name { get; set; }

    [EntityProperty(ForeignKey = true, Nullable = true)]
    public Category Parent { get; set; }  // self-referencing tree
}

[Entity(Scope = "myapp", Table = "tickets")]
public class Ticket
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 256)]
    public string Title { get; set; }

    [ForeignKey]
    public Status Status { get; set; }

    [EntityProperty(ForeignKey = true, Nullable = true)]
    public Category Category { get; set; }

    [EntityProperty(DbType = DbType.DateTime)]
    public DateTime CreatedAt { get; set; }
}
```

