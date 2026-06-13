# Gehtsoft.EF Real-World Patterns

## Section 1: DAO Layer Structure

Best practice: separate data access into its own layer with an interface.

```csharp
// Interface defines data operations
public interface IDao : IDisposable
{
    void SaveProduct(Product product);
    Product GetProduct(int id);
    EntityCollection<Product> GetProducts(ProductFilter filter, int? skip, int? limit);
    int GetProductCount(ProductFilter filter);
    void DeleteProduct(Product product);
}

// Implementation wraps SqlDbConnection
public class SqlDao : IDao
{
    private readonly SqlDbConnection mConnection;

    public SqlDao(SqlDbConnection connection)
    {
        mConnection = connection;
    }

    public void Dispose() => mConnection.Dispose();
    // ... implement methods
}
```

For larger projects, use partial classes to organize by entity type:
```
DaoConnection.cs              -- constructor, initialization
DaoConnection.Products.cs     -- Product CRUD
DaoConnection.Categories.cs   -- Category CRUD
DaoConnection.Orders.cs       -- Order CRUD
```

## Section 2: DI Integration

```csharp
// Service wrapping connection factory
public class DaoService : IDaoService
{
    private readonly SqlDbUniversalConnectionFactory mFactory;

    public DaoService(string driver, string connectionString)
    {
        mFactory = new SqlDbUniversalConnectionFactory(driver, connectionString);
    }

    public IDao CreateConnection() => new SqlDao(mFactory.GetConnection());
}

// Extension method for DI registration
public static class ServiceExtensions
{
    public static IServiceCollection AddDao(this IServiceCollection services,
        string driver, string connectionString)
    {
        services.AddSingleton<IDaoService>(new DaoService(driver, connectionString));
        return services;
    }
}
```

## Section 3: Common CRUD Patterns

### Save pattern (insert or update)
```csharp
public void SaveProduct(Product product)
{
    using var query = product.Id < 1
        ? mConnection.GetInsertEntityQuery<Product>()
        : mConnection.GetUpdateEntityQuery<Product>();
    query.Execute(product);
}
```

### Read with optional filter, pagination, and sorting
```csharp
public EntityCollection<Product> ReadProducts(
    ProductFilter filter = null, int? skip = null, int? take = null)
{
    using var query = mConnection.GetSelectEntitiesQuery<Product>();

    if (filter != null)
        ConfigureProductQuery(query, filter);

    query.AddOrderBy(nameof(Product.Name));

    if (skip.HasValue) query.Skip = skip.Value;
    if (take.HasValue) query.Limit = take.Value;

    return query.ReadAll<Product>();
}

public int GetProductCount(ProductFilter filter = null)
{
    using var query = mConnection.GetSelectEntitiesCountQuery<Product>();
    if (filter != null)
        ConfigureProductQuery(query, filter);
    query.Execute();
    return query.RowCount;
}

private static void ConfigureProductQuery(ConditionEntityQueryBase query, ProductFilter filter)
{
    if (!string.IsNullOrEmpty(filter.NameStartsWith))
        query.Where.Property(nameof(Product.Name)).Like(filter.NameStartsWith + "%");
    if (filter.MinPrice.HasValue)
        query.Where.And().Property(nameof(Product.Price)).Ge(filter.MinPrice.Value);
    if (filter.CategoryId.HasValue)
        query.Where.And().Property(nameof(Product.Category)).Eq(filter.CategoryId.Value);
}
```

Notice how `ConfigureProductQuery` accepts `ConditionEntityQueryBase` -- this lets the same filter logic work for both select and count queries.

### Safe delete with dependency check
```csharp
public bool DeleteCategory(Category category)
{
    if (!mConnection.CanDelete(category))
        return false;
    using var query = mConnection.GetDeleteEntityQuery<Category>();
    query.Execute(category);
    return true;
}
```

### Cascading delete (children first)
```csharp
public void DeleteCategoryWithProducts(Category category)
{
    using var transaction = mConnection.BeginTransaction();
    // Delete children first
    using (var q = mConnection.GetMultiDeleteEntityQuery<Product>())
    {
        q.Where.Property(nameof(Product.Category)).Eq(category.Id);
        q.Execute();
    }
    // Then parent
    using (var q = mConnection.GetDeleteEntityQuery<Category>())
        q.Execute(category);
    transaction.Commit();
}
```

## Section 4: Database Initialization Pattern

```csharp
public class SqlDao : IDao
{
    private readonly SqlDbConnection mConnection;

    public SqlDao(SqlDbConnection connection)
    {
        mConnection = connection;
    }

    public void InitializeDatabase(bool forceRecreate = false)
    {
        var controller = new CreateEntityController(GetType(), "myapp");

        controller.UpdateTables(mConnection,
            forceRecreate
                ? CreateEntityController.UpdateMode.Recreate
                : CreateEntityController.UpdateMode.Update);

        // Database-specific post-init
        ApplyDatabaseSpecificSetup();
    }

    private void ApplyDatabaseSpecificSetup()
    {
        if (mConnection.ConnectionType == UniversalSqlDbFactory.SQLITE)
        {
            using var q = mConnection.GetQuery("PRAGMA case_sensitive_like=true;");
            q.ExecuteNoData();
        }
    }
}
```

## Section 5: Multi-Database Support

The connection type is available via `connection.ConnectionType`:
```csharp
if (connection.ConnectionType == UniversalSqlDbFactory.POSTGRES)
{
    // PostgreSQL-specific index
    using var q = connection.GetQuery(
        "CREATE INDEX IF NOT EXISTS idx_name ON products USING gin(name)");
    q.ExecuteNoData();
}
```

## Section 6: Testing with Gehtsoft.EF

Use SQLite for fast in-memory tests:
```csharp
public class ProductDaoTests : IDisposable
{
    private readonly SqlDbConnection mConnection;

    public ProductDaoTests()
    {
        mConnection = UniversalSqlDbFactory.Create("sqlite", "Data Source=:memory:");
        var controller = new CreateEntityController(typeof(Product), "myapp");
        controller.CreateTables(mConnection);
    }

    public void Dispose() => mConnection.Dispose();

    [Fact]
    public void InsertAndRead()
    {
        var product = new Product { Name = "Test", Price = 9.99, Stock = 5 };
        using (var q = mConnection.GetInsertEntityQuery<Product>())
            q.Execute(product);

        using var select = mConnection.GetSelectOneEntityQuery<Product>(product.Id);
        var loaded = select.ReadOne<Product>();
        loaded.Name.Should().Be("Test");
    }
}
```

### Test fixture pattern for shared setup:
```csharp
public class DatabaseFixture : IDisposable
{
    public SqlDbConnection Connection { get; }

    public DatabaseFixture()
    {
        Connection = UniversalSqlDbFactory.Create("sqlite", "Data Source=:memory:");
        var controller = new CreateEntityController(typeof(Product), "myapp");
        controller.CreateTables(Connection);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Insert reference data used across tests
    }

    public void Dispose() => Connection.Dispose();
}

public class ProductTests : IClassFixture<DatabaseFixture>
{
    private readonly SqlDbConnection mConnection;

    public ProductTests(DatabaseFixture fixture)
    {
        mConnection = fixture.Connection;
    }
}
```

### Async test pattern

```csharp
public class ProductAsyncTests : IClassFixture<DatabaseFixture>
{
    private readonly SqlDbConnection mConnection;

    public ProductAsyncTests(DatabaseFixture fixture)
    {
        mConnection = fixture.Connection;
    }

    [Fact]
    public async Task InsertAndReadAsync()
    {
        var product = new Product { Name = "Widget", Price = 5.99, Stock = 20 };
        using (var q = mConnection.GetInsertEntityQuery<Product>())
            await q.ExecuteAsync(product);

        using var select = mConnection.GetSelectOneEntityQuery<Product>(product.Id);
        var loaded = await select.ReadOneAsync<Product>();
        loaded.Name.Should().Be("Widget");
    }
}
```

### Multi-database test parameterization

To run the same tests against multiple database drivers, use `[Theory]` with connection names:

```csharp
public class MultiDbTests
{
    public static TheoryData<string> Drivers => new TheoryData<string>
    {
        { "sqlite" },
        // Add other drivers as available in the test environment:
        // { "mssql" },
        // { "npgsql" },
    };

    [Theory]
    [MemberData(nameof(Drivers))]
    public void CrudWorksOnAllDrivers(string driver)
    {
        string connStr = driver switch
        {
            "sqlite" => "Data Source=:memory:",
            "mssql" => "Server=...;Database=...;...",
            _ => throw new NotSupportedException(driver)
        };

        using var connection = UniversalSqlDbFactory.Create(driver, connStr);
        var controller = new CreateEntityController(typeof(Product), "myapp");
        controller.CreateTables(connection);

        // Run test logic...
    }
}
```

## Section 7: Common Mistakes to Avoid

1. **Forgetting `using` on queries** -- queries hold database resources and must be disposed
2. **Wrong FK delete order** -- always delete children before parents
3. **Missing `Size` on string properties** -- will cause runtime errors
4. **Using entity queries without scope** -- entities without matching scope won't be found by CreateEntityController
5. **Thread-unsafe connection sharing** -- create one connection per thread/request, or use `Lock()`/`LockAsync()`

