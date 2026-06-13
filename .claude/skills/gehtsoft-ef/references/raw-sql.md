# Raw SQL, QueryBuilder, and EntityQueryBuilder

## Section 1: SqlQuery (Raw SQL)

For when entity queries aren't enough — custom reports, DDL, database-specific features.

### Execute non-query (DDL/DML)
```csharp
using var query = connection.GetQuery("CREATE INDEX idx_name ON products (name)");
query.ExecuteNoData();
```

### Execute with parameters
```csharp
using var query = connection.GetQuery(
    "UPDATE products SET price = price * @factor WHERE category_id = @catId");
query.BindParam("factor", 1.1);
query.BindParam("catId", 5);
int rowsAffected = query.ExecuteNoData();
```

### Select with reader
```csharp
using var query = connection.GetQuery(
    "SELECT name, SUM(price * stock) as total_value FROM products GROUP BY name");
query.ExecuteReader();
while (query.ReadNext())
{
    string name = query.GetValue<string>(0);
    double total = query.GetValue<double>(1);
}
```

### Null parameters
```csharp
query.BindNull("paramName", DbType.String);
```

### Output parameters
```csharp
query.BindOutputParam("result", DbType.Int32);
query.ExecuteNoData();
int result = query.GetParamValue<int>("result");
```

### SQL injection protection
Enabled by default. To suppress (for dynamic SQL):
```csharp
using var query = connection.GetQuery(dynamicSql, suppressScalarProtection: true);
```

### Async
```csharp
await query.ExecuteNoDataAsync();
await query.ExecuteReaderAsync();
bool hasRow = await query.ReadNextAsync();
```

### Getting a query from a builder
```csharp
var builder = connection.GetSelectQueryBuilder(tableDescriptor);
// ... configure builder ...
using var query = connection.GetQuery(builder);
query.ExecuteReader();
```

## Section 2: QueryBuilder (Low-Level SQL Construction)

QueryBuilder works with `TableDescriptor` instead of entity types. It gives full control over SQL generation while still being database-agnostic.

### TableDescriptor
```csharp
var table = new TableDescriptor { Name = "my_table" };
table.Add(new TableDescriptor.ColumnInfo
{
    Name = "id", DbType = DbType.Int32, PrimaryKey = true, Autoincrement = true
});
table.Add(new TableDescriptor.ColumnInfo
{
    Name = "name", DbType = DbType.String, Size = 128
});
table.Add(new TableDescriptor.ColumnInfo
{
    Name = "value", DbType = DbType.Double, Nullable = true
});
```

### Create / Drop table
```csharp
using var query = connection.GetQuery(connection.GetCreateTableBuilder(table));
query.ExecuteNoData();

using var dropQuery = connection.GetQuery(connection.GetDropTableBuilder(table));
dropQuery.ExecuteNoData();
```

### SELECT with QueryBuilder
```csharp
var builder = connection.GetSelectQueryBuilder(table);
builder.AddToResultset(table["name"]);
builder.AddToResultset(AggFn.Sum, table["value"]);
builder.Where.Property(table["name"]).Is(CmpOp.Like).Parameter("pattern");
builder.AddGroupBy(table["name"]);
builder.OrderBy.Add(table["name"]);

using var query = connection.GetQuery(builder);
query.BindParam("pattern", "A%");
query.ExecuteReader();
```

### INSERT
```csharp
var builder = connection.GetInsertQueryBuilder(table);
using var query = connection.GetQuery(builder);
query.BindParam(table["name"].Name, "test");
query.BindParam(table["value"].Name, 42.5);
query.ExecuteNoData();
```

### UPDATE
```csharp
var builder = connection.GetUpdateQueryBuilder(table);
builder.AddUpdateColumn(table["value"]);
builder.Where.Property(table["name"]).Is(CmpOp.Eq).Parameter("name");
using var query = connection.GetQuery(builder);
query.BindParam(table["value"].Name, 99.9);
query.BindParam("name", "test");
query.ExecuteNoData();
```

### DELETE
```csharp
var builder = connection.GetDeleteQueryBuilder(table);
builder.Where.Property(table["name"]).Is(CmpOp.Eq).Parameter("name");
using var query = connection.GetQuery(builder);
query.BindParam("name", "test");
query.ExecuteNoData();
```

### JOINs with QueryBuilder
```csharp
var builder = connection.GetSelectQueryBuilder(ordersTable);
var join = builder.AddTable(productsTable, TableJoinType.Inner);
join.On.Property(ordersTable["product_id"]).Is(CmpOp.Eq).Property(productsTable["id"]);

builder.AddToResultset(ordersTable["id"]);
builder.AddToResultset(productsTable["name"]);
```

### ALTER TABLE
```csharp
var alter = connection.GetAlterTableQueryBuilder();
alter.AddColumn(table, new TableDescriptor.ColumnInfo
{
    Name = "new_column", DbType = DbType.String, Size = 64, Nullable = true
});
using var query = connection.GetQuery(alter);
query.ExecuteNoData();
```

### Hierarchical (CTE) queries
```csharp
var builder = connection.GetHierarchicalSelectQueryBuilder(
    table, table["parent_id"], rootParameter: "rootId");
builder.AddToResultset(table["id"]);
builder.AddToResultset(table["name"]);
using var query = connection.GetQuery(builder);
query.BindParam("rootId", 1);
query.ExecuteReader();
```

### Views

```csharp
// Create a view from a select builder
var selectBuilder = connection.GetSelectQueryBuilder(productsTable);
selectBuilder.AddToResultset(productsTable["name"]);
selectBuilder.AddToResultset(AggFn.Sum, productsTable["stock"]);
selectBuilder.AddGroupBy(productsTable["name"]);

var viewBuilder = connection.GetCreateViewBuilder("product_stock_summary", selectBuilder);
using (var query = connection.GetQuery(viewBuilder))
    query.ExecuteNoData();

// Drop a view
var dropViewBuilder = connection.GetDropViewBuilder("product_stock_summary");
using (var query = connection.GetQuery(dropViewBuilder))
    query.ExecuteNoData();
```

### Index creation and drop

```csharp
// Create an index via QueryBuilder
var indexBuilder = connection.GetCreateIndexBuilder(table, new CompositeIndex("idx_name_price")
{
    // Add fields to the index
});
using (var query = connection.GetQuery(indexBuilder))
    query.ExecuteNoData();

// Drop an index
var dropIndexBuilder = connection.GetDropIndexBuilder(table, "idx_name_price");
using (var query = connection.GetQuery(dropIndexBuilder))
    query.ExecuteNoData();

// Or use raw SQL for database-specific indexes
using (var query = connection.GetQuery("CREATE INDEX idx_name ON products (name)"))
    query.ExecuteNoData();
```

## Section 3: EntityQueryBuilder

EntityQueryBuilder wraps QueryBuilder with entity metadata — it maps entity property names to table column names automatically.

When you use `connection.GetSelectEntitiesQuery<T>()` and similar methods, they create EntityQueryBuilder internally. You rarely need to use EntityQueryBuilder directly, but it's available when you need the bridge between entity-level and SQL-level:

```csharp
// Access the underlying query builder from an entity query
using var entityQuery = connection.GetSelectEntitiesQuery<Product>();
var selectBuilder = entityQuery.Builder; // AQueryBuilder
```

The key difference:
- **QueryBuilder** — works with `TableDescriptor`, column names, raw SQL concepts
- **EntityQuery** — works with entity types, property names, automatic FK resolution
- **EntityQueryBuilder** — maps between the two

In practice, prefer EntityQuery for entity operations and raw SqlQuery for custom SQL. QueryBuilder is useful when you need database-agnostic SQL generation without entity mapping.

