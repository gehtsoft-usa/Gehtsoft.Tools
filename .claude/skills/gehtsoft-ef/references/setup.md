# Setup Reference

## Adding Packages

Before adding packages, look up the latest version on NuGet:
```bash
dotnet package search Gehtsoft.EF.Db.SqlDb --take 1
```

Always needed:
```xml
<PackageReference Include="Gehtsoft.EF.Db.SqlDb" Version="LATEST" />
<PackageReference Include="Gehtsoft.EF.Entities" Version="LATEST" />
```

Per-database driver (pick one):
```xml
<PackageReference Include="Gehtsoft.EF.Db.SqliteDb" Version="LATEST" />
<PackageReference Include="Gehtsoft.EF.Db.MssqlDb" Version="LATEST" />
<PackageReference Include="Gehtsoft.EF.Db.PostgresDb" Version="LATEST" />
<PackageReference Include="Gehtsoft.EF.Db.MysqlDb" Version="LATEST" />
<PackageReference Include="Gehtsoft.EF.Db.OracleDb" Version="LATEST" />
```

Optional:
```xml
<PackageReference Include="Gehtsoft.EF.Utils" Version="LATEST" />
```

Replace `LATEST` with the actual version number from the NuGet search above.
All Gehtsoft.EF packages should use the same version.

## Connection Initialization

### Direct creation
```csharp
using Gehtsoft.EF.Db.SqlDb;

// Driver constants: "mssql", "mysql", "npgsql", "sqlite", "oracle"
using SqlDbConnection connection = UniversalSqlDbFactory.Create("sqlite", "Data Source=mydb.db");
```

### DI-friendly factory
```csharp
var factory = new SqlDbUniversalConnectionFactory("sqlite", "Data Source=mydb.db");
using SqlDbConnection connection = factory.GetConnection();

// Or async
using SqlDbConnection connection = await factory.GetConnectionAsync();
```

### ISqlDbConnectionFactory interface
```csharp
public interface ISqlDbConnectionFactory
{
    bool NeedDispose { get; }
    SqlDbConnection GetConnection();
    Task<SqlDbConnection> GetConnectionAsync(CancellationToken? token = null);
}
```

### Wrapping existing connection
```csharp
var factory = new ExistingConnectionFactory(existingConnection);
// NeedDispose = false -- won't dispose the underlying connection
```

### Driver name constants
```csharp
UniversalSqlDbFactory.MSSQL    // "mssql"
UniversalSqlDbFactory.MYSQL    // "mysql"
UniversalSqlDbFactory.POSTGRES // "npgsql"
UniversalSqlDbFactory.SQLITE   // "sqlite"
UniversalSqlDbFactory.ORACLE   // "oracle"
```

### Thread safety
SqlDbConnection is NOT thread-safe. Use Lock/LockAsync for concurrent access:
```csharp
using (await connection.LockAsync())
{
    // safe to use connection here
}
```
Best practice: create one connection per operation or per request.

### Database-specific notes
SQLite -- set PRAGMA for case-sensitive LIKE if needed:
```csharp
using var query = connection.GetQuery("PRAGMA case_sensitive_like=true;");
query.ExecuteNoData();
```

## Creating and Dropping Tables

### Using CreateEntityController (recommended)

```csharp
// Discover all entities in the assembly containing MyEntity, filtered by scope
var controller = new CreateEntityController(typeof(MyEntity), "myapp");

// Create tables that don't exist yet
controller.CreateTables(connection);

// Drop obsolete tables (marked with [ObsoleteEntity])
controller.DropTables(connection);
```

### Manual table operations
```csharp
// Create single entity table
using (var query = connection.GetCreateEntityQuery<Customer>())
    query.Execute();

// Drop single entity table
using (var query = connection.GetDropEntityQuery<Customer>())
    query.Execute();
```

## Schema Migration

For full migration coverage (UpdateTables, obsolete entities/properties, patches, SQLite workarounds),
read `references/migration.md`.

