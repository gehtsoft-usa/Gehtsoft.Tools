# Entity CRUD Operations

All entity query extension methods live on `SqlDbConnection` via `EntityConnectionExtension`.
Every query object is `IDisposable` -- always wrap in `using`.

Assumed entity model: `Category` (AutoId, Name) and `Product` (AutoId, Name, FK to Category, Price, Stock).

## Insert

```csharp
var category = new Category { Name = "Electronics" };
using (var query = connection.GetInsertEntityQuery<Category>())
    query.Execute(category);
// category.Id is now populated with the auto-generated value

var product = new Product { Name = "Laptop", Category = category, Price = 999.99, Stock = 10 };
using (var query = connection.GetInsertEntityQuery<Product>())
    query.Execute(product);
```

Non-generic form: `connection.GetInsertEntityQuery(typeof(Category))`

The `ignoreAutoIncrement` parameter -- insert with an explicit ID value:

```csharp
using var query = connection.GetInsertEntityQuery<Category>(ignoreAutoIncrement: true);
```

## Update (Single Entity)

Updates by primary key:

```csharp
product.Price = 899.99;
using (var query = connection.GetUpdateEntityQuery<Product>())
    query.Execute(product);
```

Common save pattern (insert or update based on ID):

```csharp
public void SaveProduct(SqlDbConnection connection, Product product)
{
    using var query = product.Id < 1
        ? connection.GetInsertEntityQuery<Product>()
        : connection.GetUpdateEntityQuery<Product>();
    query.Execute(product);
}
```

Auto-detect insert vs update (requires auto-increment int PK; inserts when PK == 0):

```csharp
using var query = connection.GetModifyEntityQueryFor(product);
query.Execute(product);
```

## Update (Mass/Bulk)

Update multiple rows matching a condition:

```csharp
using var query = connection.GetMultiUpdateEntityQuery<Product>();
query.AddUpdateColumn(nameof(Product.Stock), 0);
query.Where.Property(nameof(Product.Price)).Ls(10.0);
query.Execute();
```

`AddUpdateColumnByExpression` for raw SQL expressions in the SET clause:

```csharp
query.AddUpdateColumnByExpression(nameof(Product.Stock), "stock - 1");
```

## Delete (Single Entity)

Delete by primary key:

```csharp
using (var query = connection.GetDeleteEntityQuery<Product>())
    query.Execute(product);
```

Check if safe to delete (no FK references exist):

```csharp
bool canDelete = connection.CanDelete(category);
if (canDelete)
{
    using var query = connection.GetDeleteEntityQuery<Category>();
    query.Execute(category);
}
```

Async variant: `await connection.CanDeleteAsync(category)`

Exclude specific types from the FK check:

```csharp
bool canDelete = connection.CanDelete(category, except: new[] { typeof(ArchivedProduct) });
```

## Delete (Mass/Bulk)

Delete multiple rows matching a condition:

```csharp
using var query = connection.GetMultiDeleteEntityQuery<Product>();
query.Where.Property(nameof(Product.Stock)).Eq(0);
query.Execute();
```

FK dependency order: delete children before parents. Delete Products referencing a Category before deleting the Category.

## Complete End-to-End Workflow

This shows the full lifecycle: create tables, insert parent, insert child with FK, select
with auto-populated FK, and clean up.

```csharp
// 1. Create tables (order is handled automatically)
var controller = new CreateEntityController(typeof(Product), "myapp");
controller.CreateTables(connection);

// 2. Insert parent
var category = new Category { Name = "Electronics" };
using (var q = connection.GetInsertEntityQuery<Category>())
    q.Execute(category);
// category.Id is now set (e.g., 1)

// 3. Insert child with FK reference
var product = new Product
{
    Name = "Laptop",
    Category = category,   // pass the whole object, not just the ID
    Price = 999.99,
    Stock = 10
};
using (var q = connection.GetInsertEntityQuery<Product>())
    q.Execute(product);

// 4. Select — FK is auto-populated
using (var q = connection.GetSelectOneEntityQuery<Product>(product.Id))
{
    Product loaded = q.ReadOne<Product>();
    // loaded.Category is a fully populated Category object
    // loaded.Category.Id == 1
    // loaded.Category.Name == "Electronics"
}

// 5. Clean up (children before parents)
using (var q = connection.GetDeleteEntityQuery<Product>())
    q.Execute(product);
using (var q = connection.GetDeleteEntityQuery<Category>())
    q.Execute(category);
```

## Basic Select

### Select all entities

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
EntityCollection<Product> products = query.ReadAll<Product>();
// ReadAll calls Execute() automatically
```

### Select one entity by PK

```csharp
using var query = connection.GetSelectOneEntityQuery<Product>(productId);
Product product = query.ReadOne<Product>();
```

### Select with simple where

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
query.Where.Property(nameof(Product.Price)).Gt(100.0);
var expensive = query.ReadAll<Product>();
```

### Count

```csharp
using var query = connection.GetSelectEntitiesCountQuery<Product>();
query.Where.Property(nameof(Product.Stock)).Gt(0);
query.Execute();
int count = query.RowCount;
```

### Row-by-row iteration (lower memory)

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
query.Execute();
while (query.ReadNext())
{
    Product p = query.ReadOne<Product>();
    // process one at a time
}
```

## Async Versions

All operations have async counterparts:

```csharp
using var query = connection.GetInsertEntityQuery<Product>();
await query.ExecuteAsync(product);

using var selectQuery = connection.GetSelectEntitiesQuery<Product>();
var products = await selectQuery.ReadAllAsync<Product>();

using var oneQuery = connection.GetSelectOneEntityQuery<Product>(productId);
Product p = await oneQuery.ReadOneAsync<Product>();
```

## Transactions

```csharp
using (var transaction = connection.BeginTransaction())
{
    try
    {
        using (var q1 = connection.GetInsertEntityQuery<Category>())
            q1.Execute(category);
        using (var q2 = connection.GetInsertEntityQuery<Product>())
            q2.Execute(product);
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

With isolation level:

```csharp
using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
```

## Query Lifecycle Notes

- All query objects are `IDisposable` -- always use `using`. Some databases require disposal before the next query can execute.
- `Execute()` prepares and runs the query.
- For select queries, `ReadAll`/`ReadOne` call `Execute()` automatically.
- `ReadNext()` advances the cursor one row (returns `false` when exhausted).
- Non-generic forms accept `Type` parameter: `GetInsertEntityQuery(typeof(T))`, etc.
