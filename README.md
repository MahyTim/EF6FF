# Entity Framework 6 - a faster 'Find' implementation

## What
The fastest implementation for Entity Framework 6 to load an entity by it's unique key.

## Why
The IDbSet implementation from Entity Framework also provides a 'Find' method to search an entity based on it's unique id. So why not use that one? Well put simply: **it's slow, very slow**.

Let's assume we have an entity model

 - Customer has Orders
 - An Order references to a Customer
 - Both have some properties

If we insert 1000 customers each having 3 orders, let's assume we would 'Find' these 4000 objects using the following method we get timings in this magnitude:

|method| 0% loaded (*) | 50% loaded | 100% loaded |
|------|-----------|------------|-------------|
|Entity Framework Find | 20 seconds | 36 seconds | 2,5 minutes |
|our 'new' Find implementation | 5 seconds | 4,4 seconds | 4,7 seconds |

(*) the amount of % loaded means the number of objects already loaded from the database and available in the change tracker of Entity Framework.

**How can the default Find from Entity Framework become slower when more data is already loaded?**

The implementation learns us that Find (and also .Local) always performs a 'DetectChanges' operation which includes: validate all entities & check all entities for state changes.........hence the penalty increase when loading more objects.

**Why is this implementation faster? Does everything of EF still work?**

We implemented a Dictionary lookup and skipped the 'DetectChanges'. That also means that not every feature of Entity Framework will work, otherwise they wouldn't call DetectChanges everytime :)
The following feature does NOT work in this implementation:

    using (var db = new MyDatabase())
    {
            var finder = new FasterFind(db);
            var customer = db.Customers.Add(new Customer()
            {
                   Id = customerIds.Next(),
                   Name = "Some name",
                   Orders = new List<Order>()
            });
            customer.Orders.Add(new Order()
            {
                  Customer = customer,
                  Id = orderIds.Next(),
            });
        // This is the case where EF can find the object
        // and our implementation cannot find it 
        // because the Order is not yet known by EF
        Debug.Assert(finder.Find<Order>(orderIds.All().First()) == null);
        Debug.Assert(db.Orders.Find(orderIds.All().First()) != null);

        db.SaveChanges();
        Debug.Assert(finder.Find<Order>(orderIds.All().First()) != null);
    }

 how to solve this?  Always tell EF to track the entity directly like
 

     var customer = db.Customers.Add(new Customer()
     {
            Id = customerIds.Next(),
            Name = "Some name",
            Orders = new List<Order>()
            {
               new Order()
               {
                   Id =  orderIds.Next(),
               }
           }
     });


## How can I use this?
There is **no NuGet package** available! 

**Why** ? you may want to change the IEntity interface approach for your needs and it's feature complete (and very easy) so why not just include the code.

**So how to use this?** take a look at this code and include a copy in yours.


