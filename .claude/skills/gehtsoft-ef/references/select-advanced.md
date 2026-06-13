# Advanced SELECT Features

Assumes the entity definitions from entity-queries.md (Category, Product, OrderItem).

## Section 1: Where Conditions

### Fluent condition API

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();

// Single condition
query.Where.Property(nameof(Product.Price)).Gt(100.0);

// Multiple conditions (AND is default)
query.Where.Property(nameof(Product.Price)).Gt(100.0);
query.Where.And().Property(nameof(Product.Stock)).Gt(0);

// OR
query.Where.Property(nameof(Product.Price)).Ls(10.0);
query.Where.Or().Property(nameof(Product.Price)).Gt(1000.0);
```

### Comparison operators (all as extension methods on SingleEntityQueryConditionBuilder)

- `Eq(value)` / `Neq(value)` — equal / not equal
- `Gt(value)` / `Ge(value)` — greater than / greater or equal
- `Ls(value)` / `Le(value)` — less than / less or equal
- `Like(pattern)` — SQL LIKE, use % and _ wildcards
- `IsNull()` / `NotNull()` — NULL checks
- `In()` / `NotIn()` — followed by `.Values(...)` or `.Query(...)`

### Operator-only form (for parameter binding later)

```csharp
query.Where.Property(nameof(Product.Price)).Gt().Parameter("minPrice");
query.BindParam("minPrice", 100.0);
```

### Grouping conditions with brackets

```csharp
query.Where.And(group =>
{
    group.Property(nameof(Product.Price)).Ls(10.0);
    group.Or().Property(nameof(Product.Price)).Gt(1000.0);
});
// Result: ... AND (price < 10 OR price > 1000)
```

### SQL functions in conditions

```csharp
// Case-insensitive comparison
query.Where.Property(nameof(Product.Name)).ToUpper().Like("LAPTOP%").ToUpper();

// Date parts
query.Where.Property(nameof(Order.OrderDate)).Year().Eq(2024);

// Aggregate in HAVING (see Section 6)
query.Having.Add().Property(nameof(Product.Id)).Count().Gt(5);
```

Full list of function extensions: `ToUpper()`, `ToLower()`, `Trim()`, `Abs()`, `Year()`, `Month()`, `Day()`, `Hour()`, `Minute()`, `Second()`, `Round(digits)`, `Left(chars)`, `Length()`, `ToString()`, `ToInteger()`, `ToDouble()`, `ToDate()`, `ToTimestamp()`, `Sum()`, `Min()`, `Max()`, `Avg()`, `Count()`.

## Section 2: Auto-Join (via Foreign Keys)

When an entity has FK properties, SELECT automatically joins the referenced tables.

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
var products = query.ReadAll<Product>();
// Each product.Category is fully populated
```

Filter by FK entity's properties using `PropertyOf<T>`:

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
query.Where.PropertyOf<Category>(nameof(Category.Name)).Eq("Electronics");
var products = query.ReadAll<Product>();
```

Order by FK entity's properties:

```csharp
query.AddOrderBy<Category>(c => c.Name);
// or
query.AddOrderBy(typeof(Category), nameof(Category.Name));
```

### PropertyOf with occurrence (same type joined multiple times)

When an entity has two FK properties pointing to the same type, use the `occurrence` parameter
to distinguish which join to filter on:

```csharp
[Entity(Table = "transfers")]
public class Transfer
{
    [AutoId] public int Id { get; set; }
    [ForeignKey] public Account FromAccount { get; set; }   // occurrence 0
    [ForeignKey] public Account ToAccount { get; set; }     // occurrence 1
    [EntityProperty(DbType = DbType.Double)] public double Amount { get; set; }
}

using var query = connection.GetSelectEntitiesQuery<Transfer>();
// Filter on the first Account join (FromAccount)
query.Where.PropertyOf(nameof(Account.Name), typeof(Account), occurrence: 0).Eq("Checking");
// Filter on the second Account join (ToAccount)
query.Where.And().PropertyOf(nameof(Account.Name), typeof(Account), occurrence: 1).Eq("Savings");
```

### Deeply nested bracket conditions

```csharp
query.Where.And(outer =>
{
    outer.Property(nameof(Product.Stock)).Gt(0);
    outer.Or(inner =>
    {
        inner.Property(nameof(Product.Price)).Ls(10.0);
        inner.And().Property(nameof(Product.Name)).Like("Sale%");
    });
});
// Result: ... AND (stock > 0 OR (price < 10 AND name LIKE 'Sale%'))
```

## Section 3: Manual Join

For explicit join control:

```csharp
using var query = connection.GetSelectEntitiesQueryBase<OrderItem>();
query.AddEntity<Product>(connectToProperty: nameof(OrderItem.Product));
query.AddEntity<Category>(); // auto-connects via Product.Category FK chain
```

With explicit join type:

```csharp
query.AddEntity(typeof(Category), TableJoinType.Left);
```

Join types: `TableJoinType.Inner`, `TableJoinType.Left`, `TableJoinType.Right`, `TableJoinType.Outer`

With explicit join condition:

```csharp
query.AddEntity(typeof(Category), TableJoinType.Left,
    typeof(Product), nameof(Product.Category), CmpOp.Eq,
    typeof(Category), nameof(Category.Id));
```

## Section 4: Result Set Customization

### Choosing specific fields (use GetSelectEntitiesQueryBase)

```csharp
using var query = connection.GetSelectEntitiesQueryBase<Product>();
query.AddToResultset(nameof(Product.Id));
query.AddToResultset(nameof(Product.Name));
query.AddToResultset(nameof(Product.Price));
query.Execute();
while (query.ReadNext())
{
    int id = query.GetValue<int>(0);
    string name = query.GetValue<string>(1);
    double price = query.GetValue<double>(2);
}
```

### Generic select with dynamic results

```csharp
using var query = connection.GetGenericSelectEntityQuery<Product>();
query.AddToResultset(nameof(Product.Name));
query.AddToResultset(AggFn.Sum, nameof(Product.Stock), "totalStock");
query.AddGroupBy(nameof(Product.Name));
dynamic result = query.ReadOneDynamic();
string name = result.Name;
int total = result.totalStock;
```

## Section 5: Aggregation

### Aggregate functions: `AggFn.Count`, `AggFn.Sum`, `AggFn.Avg`, `AggFn.Min`, `AggFn.Max`

```csharp
using var query = connection.GetGenericSelectEntityQuery<Product>();
query.AddToResultset(AggFn.Count, nameof(Product.Id), "count");
query.AddToResultset(AggFn.Avg, nameof(Product.Price), "avgPrice");
query.AddToResultset(AggFn.Max, nameof(Product.Price), "maxPrice");
dynamic result = query.ReadOneDynamic();
```

### With GROUP BY

```csharp
using var query = connection.GetGenericSelectEntityQuery<Product>();
query.AddEntity<Category>(connectToProperty: nameof(Product.Category));
query.AddToResultset(typeof(Category), nameof(Category.Name));
query.AddToResultset(AggFn.Count, nameof(Product.Id), "productCount");
query.AddToResultset(AggFn.Avg, nameof(Product.Price), "avgPrice");
query.AddGroupBy(typeof(Category), nameof(Category.Name));
query.AddOrderBy(typeof(Category), nameof(Category.Name));

var results = query.ReadAllDynamic();
foreach (dynamic row in results)
    Console.WriteLine($"{row.Name}: {row.productCount} products, avg ${row.avgPrice}");
```

### Lambda form with SqlFunction

```csharp
using var query = connection.GetGenericSelectEntityQuery<Product>();
query.AddToResultset<Product, DateTime>(p => SqlFunction.Max(p.Price), "maxPrice");
dynamic result = query.ReadOneDynamic();
```

## Section 6: HAVING

Filter on aggregate results:

```csharp
using var query = connection.GetGenericSelectEntityQuery<Product>();
query.AddToResultset(typeof(Category), nameof(Category.Name));
query.AddToResultset(AggFn.Count, nameof(Product.Id), "count");
query.AddGroupBy(typeof(Category), nameof(Category.Name));
query.Having.Add().Property(nameof(Product.Id)).Count().Gt(5);
```

## Section 7: Ordering and Pagination

### ORDER BY

```csharp
query.AddOrderBy(nameof(Product.Price), SortDir.Desc);
query.AddOrderBy(nameof(Product.Name)); // SortDir.Asc is default
```

### Pagination (LIMIT/OFFSET)

```csharp
query.Skip = 20;   // skip first 20 rows
query.Limit = 10;  // return 10 rows
```

### DISTINCT

```csharp
query.Distinct = true;
```

## Section 8: Subqueries in WHERE

### IN subquery

```csharp
// Find products in categories that have "Electronics" in the name
using var query = connection.GetSelectEntitiesQuery<Product>();
using var subquery = connection.GetGenericSelectEntityQuery<Category>();
subquery.AddToResultset(nameof(Category.Id));
subquery.Where.Property(nameof(Category.Name)).Like("%Electronics%");

query.Where.Property(nameof(Product.Category)).In().Query(subquery);
var products = query.ReadAll<Product>();
```

### NOT IN subquery

```csharp
query.Where.Property(nameof(Product.Category)).NotIn().Query(subquery);
```

### EXISTS / NOT EXISTS

```csharp
query.Where.Exists(subquery);
// or
query.Where.NotExists(subquery);
```

### Parameter sharing between query and subquery

```csharp
using var query = connection.GetSelectEntitiesQuery<Product>();
using var sub = connection.GetGenericSelectEntityQuery<OrderItem>();
sub.AddToResultset(nameof(OrderItem.Product));
sub.Where.Property(nameof(OrderItem.Quantity)).Gt().Parameter("minQty");
query.Where.Property(nameof(Product.Id)).In().Query(sub);
query.BindParam("minQty", 10);
```

## Section 9: Hierarchical Queries (CTE)

For tree-structured data with self-referencing FK:

```csharp
// Category has Parent FK to itself
using var query = connection.GetSelectEntitiesTreeQuery<Category>();
query.Root = parentCategoryId;  // starting node (null for all roots)
var tree = query.ReadAll<Category>();
```

The query uses recursive CTE internally. The entity must have a nullable self-referencing FK property.

