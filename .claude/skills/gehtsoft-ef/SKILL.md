---
name: gehtsoft-ef
description: |
  Guide for working with the Gehtsoft.EF .NET ORM library — defining entities, querying data, managing schemas, and writing tests. Use this skill whenever the project references Gehtsoft.EF packages, or the user works with classes annotated with [Entity], [AutoId], [EntityProperty], [ForeignKey], or mentions SqlDbConnection, EntityQuery, CreateEntityController, GenericEntityAccessor, SelectEntitiesQuery, or any Gehtsoft.EF API. Also trigger when the user asks about data access code in a project that already uses Gehtsoft.EF, even if they don't name the library explicitly.
---

# Gehtsoft.EF Skill

Gehtsoft.EF is a lightweight .NET ORM that maps C# classes to database tables using attributes. It is NOT Entity Framework / EF Core — it has its own API, conventions, and query model. Do not confuse the two or suggest EF Core patterns.

Key differences from EF Core:
- No DbContext — uses `SqlDbConnection` directly
- No LINQ-to-SQL query pipeline — uses a fluent query builder API
- No change tracking — explicit insert/update/delete calls
- No migrations framework — uses `CreateEntityController.UpdateTables()` for schema evolution
- Supports: SQLite, SQL Server, PostgreSQL, MySQL, Oracle

## Work Modes

### Creating EF code
When adding Gehtsoft.EF to a project or writing new data access code:
1. Read `references/setup.md` for packages and connection initialization
2. Read `references/entity-model.md` for entity definitions
3. Read `references/entity-queries.md` for CRUD operations
4. Read `references/patterns.md` for DAO layer structure and DI integration

### Modifying EF code
When changing existing entities, queries, or schema:
1. Read the existing entity definitions to understand the current model
2. Read `references/entity-model.md` for attribute reference (especially schema evolution with `[ObsoleteEntityProperty]`)
3. Read the relevant query reference for the operation being modified

### Testing EF code
When writing tests for EF-backed code:
1. Read `references/patterns.md` Section 6 for the SQLite in-memory testing pattern
2. Use `CreateEntityController.CreateTables()` in test fixtures
3. The test suite in the Gehtsoft.EF repo itself is a good reference — see file pointers in each reference doc

### Understanding/describing EF code
When explaining what existing code does:
1. Use `references/entity-model.md` to decode entity attributes
2. Use `references/entity-queries.md` and `references/select-advanced.md` to explain query logic
3. Pay attention to FK relationships — they drive automatic joins

## Quick Reference

### Entity definition
```csharp
[Entity(Scope = "myapp", Table = "products")]
public class Product
{
    [AutoId]
    public int Id { get; set; }

    [EntityProperty(DbType = DbType.String, Size = 256)]
    public string Name { get; set; }

    [ForeignKey]
    public Category Category { get; set; }

    [EntityProperty(DbType = DbType.Double)]
    public double Price { get; set; }
}
```

### Connection
```csharp
using SqlDbConnection connection = UniversalSqlDbFactory.Create("sqlite", connectionString);
```

### Table creation / migration
```csharp
var controller = new CreateEntityController(typeof(Product), "myapp");
controller.UpdateTables(connection, CreateEntityController.UpdateMode.Update);
```

### CRUD
```csharp
// Insert
using (var q = connection.GetInsertEntityQuery<Product>())
    q.Execute(product);

// Update
using (var q = connection.GetUpdateEntityQuery<Product>())
    q.Execute(product);

// Delete
using (var q = connection.GetDeleteEntityQuery<Product>())
    q.Execute(product);

// Select
using var q = connection.GetSelectEntitiesQuery<Product>();
q.Where.Property(nameof(Product.Price)).Gt(100.0);
var results = q.ReadAll<Product>();

// Count
using var cq = connection.GetSelectEntitiesCountQuery<Product>();
cq.Execute();
int count = cq.RowCount;
```

### Aggregation
```csharp
using var q = connection.GetGenericSelectEntityQuery<Product>();
q.AddToResultset(AggFn.Avg, nameof(Product.Price), "avg");
q.AddGroupBy(nameof(Product.Category));
var rows = q.ReadAllDynamic();
```

### Subquery
```csharp
using var sub = connection.GetGenericSelectEntityQuery<OrderItem>();
sub.AddToResultset(nameof(OrderItem.Product));
sub.Where.Property(nameof(OrderItem.Quantity)).Gt(10);

using var q = connection.GetSelectEntitiesQuery<Product>();
q.Where.Property(nameof(Product.Id)).In().Query(sub);
```

### Raw SQL
```csharp
using var q = connection.GetQuery("SELECT COUNT(*) FROM products WHERE price > @p");
q.BindParam("p", 100.0);
q.ExecuteReader();
q.ReadNext();
int count = q.GetValue<int>(0);
```

## Reference Files

Read these as needed — they contain detailed API documentation with examples.

| File | When to read |
|------|-------------|
| `references/setup.md` | Adding packages, creating connections, initial table creation |
| `references/migration.md` | Schema migration: UpdateTables, obsolete entities/columns, patches, SQLite workarounds |
| `references/entity-model.md` | Defining entities, attributes, relationships, schema evolution |
| `references/entity-queries.md` | Insert, update, delete, basic select, transactions |
| `references/select-advanced.md` | Joins, aggregation, WHERE/HAVING, ordering, subqueries, hierarchical queries |
| `references/generic-accessor.md` | GenericEntityAccessor for simplified CRUD, filter pattern |
| `references/raw-sql.md` | Raw SQL via SqlQuery, low-level QueryBuilder |
| `references/patterns.md` | DAO layer structure, DI integration, testing patterns, common mistakes |

## Key Namespaces

```
Gehtsoft.EF.Entities              — [Entity], [EntityProperty], [AutoId], [ForeignKey], enums
Gehtsoft.EF.Db.SqlDb              — SqlDbConnection, UniversalSqlDbFactory, SqlDbQuery
Gehtsoft.EF.Db.SqlDb.EntityQueries — EntityQuery, SelectEntitiesQuery, ModifyEntityQuery, CreateEntityController
Gehtsoft.EF.Db.SqlDb.QueryBuilder  — SelectQueryBuilder, TableDescriptor, ConditionBuilder
Gehtsoft.EF.Db.SqlDb.EntityGenericAccessor — GenericEntityAccessor, FilterPropertyAttribute
```

## Key Enums

```
AggFn:    None, Count, Sum, Avg, Min, Max
CmpOp:    Eq, Neq, Ls, Le, Gt, Ge, Like, In, NotIn, IsNull, NotNull, Exists, NotExists
LogOp:    None, Not, And, Or
SortDir:  Asc, Desc
TableJoinType: None, Inner, Left, Right, Outer
```
