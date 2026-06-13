# GenericEntityAccessor and Filters

## GenericEntityAccessor<T, TKey>

A higher-level CRUD wrapper that simplifies common operations. No need to create queries manually.

```csharp
var accessor = new GenericEntityAccessor<Product, int>(connection);

// Save (auto-detects insert vs update)
var product = new Product { Name = "Widget", Price = 9.99, Stock = 100 };
accessor.Save(product);      // inserts, populates product.Id
product.Price = 8.99;
accessor.Save(product);      // updates (Id is set)

// Get by primary key
Product p = accessor.Get(42);

// Delete
accessor.Delete(product);

// Check if deletable (no FK references)
bool safe = accessor.CanDelete(product);

// Async versions
await accessor.SaveAsync(product);
await accessor.DeleteAsync(product);
Product p2 = await accessor.GetAsync(42);
```

Key types: `int`, `string`, `Guid`

For Guid primary keys:
```csharp
var accessor = new GenericEntityAccessor<MyEntity, Guid>(connection);
var entity = new MyEntity();
accessor.NewGuidKey(entity);  // generates a unique GUID PK
accessor.Save(entity);
```

## Filters with FilterProperty

Define a filter class by deriving from `GenericEntityAccessorFilterT<T>`:

```csharp
public class ProductFilter : GenericEntityAccessorFilterT<Product>
{
    [FilterProperty(Operation = CmpOp.Eq)]
    public int? Id { get; set; }

    [FilterProperty(Operation = CmpOp.Like, PropertyName = nameof(Product.Name))]
    public string NamePattern { get; set; }

    [FilterProperty(Operation = CmpOp.Ge, PropertyName = nameof(Product.Price))]
    public double? MinPrice { get; set; }

    [FilterProperty(Operation = CmpOp.Le, PropertyName = nameof(Product.Price))]
    public double? MaxPrice { get; set; }

    [FilterProperty(Operation = CmpOp.IsNull, PropertyName = nameof(Product.Category))]
    public bool? CategoryIsNull { get; set; }
}
```

Rules:
- Property types must be nullable versions of entity property types (int?, double?, etc.)
- `null` value means the filter is inactive
- `PropertyName` defaults to the filter property name if omitted
- For `IsNull`/`NotNull`, use `bool?`: `true` = IsNull, `false` = IsNotNull, `null` = inactive
- For `In`/`NotIn`, use `ICollection` or `Array`
- All active filters are joined by AND

### Using filters with the accessor

```csharp
var accessor = new GenericEntityAccessor<Product, int>(connection);
var filter = new ProductFilter { MinPrice = 10.0, NamePattern = "Wid%" };

// Count
int count = accessor.Count(filter);

// Read with sort, skip, limit
var sortOrder = new[]
{
    new GenericEntitySortOrder(nameof(Product.Price), SortDir.Asc),
    new GenericEntitySortOrder(nameof(Product.Name), SortDir.Desc),
};
var products = accessor.Read<EntityCollection<Product>>(filter, sortOrder, skip: 0, limit: 20);

// Navigate to next/previous entity in sort order
Product next = accessor.NextEntity(currentProduct, sortOrder, filter);
Product prev = accessor.NextEntity(currentProduct, sortOrder, filter, reverseDirection: true);

// Get next entity's key without loading the full entity
int nextId = accessor.NextKey(currentProduct, sortOrder, filter);

// Delete matching filter
accessor.DeleteMultiple(filter);

// Update matching filter
accessor.UpdateMultiple(filter, nameof(Product.Stock), 0);
```

### Using filters with entity queries directly

Filters can also be bound to any ConditionEntityQueryBase:
```csharp
var filter = new ProductFilter { MinPrice = 50.0 };
using var query = connection.GetSelectEntitiesQuery<Product>();
filter.BindToQuery(query);
var products = query.ReadAll<Product>();
```

## GenericEntityAccessorWithAggregates<T, TKey>

Extends the base accessor for parent-child relationships where the child (aggregate) entities are managed through the parent.

```csharp
// Product is the parent, OrderItem is the aggregate (child)
var accessor = new GenericEntityAccessorWithAggregates<Product, int>(connection, typeof(OrderItem));

// Get aggregates for a parent entity
var items = accessor.GetAggregates<EntityCollection<OrderItem>, OrderItem>(
    product, filter: null, sortOrder: null, skip: null, limit: null);

// Count aggregates
int itemCount = accessor.GetAggregatesCount<OrderItem>(product, filter: null);

// Save aggregates (handles insert/update/delete diff)
accessor.SaveAggregates(product, originalItems, newItems,
    areDataEqual: (a, b) => a.Quantity == b.Quantity && a.UnitPrice == b.UnitPrice,
    areIDEqual: (a, b) => a.Id == b.Id,
    isDefined: a => a.Id > 0,
    isNew: a => a.Id < 1);
```
