using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Library;

namespace Demo
{
    class Program
    {


        static void Main(string[] args)
        {
            FunctionalTest();
            PerformanceTest();
        }

        private static void FunctionalTest()
        {
            Console.WriteLine("Functional test");

            {
                // Object can be retreived after adding it to the DbContext
                var customerIds = new IdGenerator();
                using (var db = new MyDatabase())
                {
                    var finder = new FasterFind(db);
                    db.Customers.Add(new Customer()
                    {
                        Id = customerIds.Next(),
                        Name = "Some name",
                        Orders = new List<Order>()
                    });
                    Debug.Assert(finder.Find<Customer>(customerIds.All().First()) != null);
                    Debug.Assert(finder.Find<Order>(customerIds.All().First()) == null);
                }
            }

            {
                // Object cannot be retreived after deleting it from the DbContext
                var customerIds = new IdGenerator();
                using (var db = new MyDatabase())
                {
                    var finder = new FasterFind(db);
                    var customer = db.Customers.Add(new Customer()
                    {
                        Id = customerIds.Next(),
                        Name = "Some name",
                        Orders = new List<Order>()
                    });
                    db.SaveChanges();
                    db.Customers.Remove(customer);
                    Debug.Assert(finder.Find<Customer>(customerIds.All().First()) == null);
                    db.SaveChanges();
                    Debug.Assert(finder.Find<Customer>(customerIds.All().First()) == null);
                }
            }

            {
                // Nested Object can be retrieved
                var customerIds = new IdGenerator();
                var orderIds = new IdGenerator();
                using (var db = new MyDatabase())
                {
                    var finder = new FasterFind(db);
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
                    Debug.Assert(finder.Find<Order>(orderIds.All().First()) != null);
                    db.SaveChanges();
                    Debug.Assert(finder.Find<Order>(orderIds.All().First()) != null);
                }
            }

            {
                // Nested Object can be retrieved - late bound
                // WARNING THIS IS THE ONLY USE-CASE THAT THIS
                // IMPLEMENTATION DOES NOT SUPPORT - UNLESS SAVED WITH SAVECHANGES
                var customerIds = new IdGenerator();
                var orderIds = new IdGenerator();
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
                    // but EF is slow because of this 'only' case.
                    Debug.Assert(finder.Find<Order>(orderIds.All().First()) == null);
                    Debug.Assert(db.Orders.Find(orderIds.All().First()) != null);

                    db.SaveChanges();
                    Debug.Assert(finder.Find<Order>(orderIds.All().First()) != null);
                }
            }

        }

        private static void PerformanceTest()
        {
            Console.WriteLine("Performance test");
            var customerIds = new IdGenerator();
            var orderIds = new IdGenerator();
            using (var db = new MyDatabase())
            {
                for (int i = 0; i < 500; i++)
                {
                    var customer = db.Customers.Add(new Customer()
                    {
                        Id = customerIds.Next(),
                        Name = "Some name",
                        Orders = new List<Order>()
                    });
                    for (int j = 0; j < 3; j++)
                    {
                        customer.Orders.Add(new Order()
                        {
                            Id = orderIds.Next(),
                            Customer = customer
                        });
                    }
                }
                db.SaveChanges();
            }

            ExecutePerformanceTest(customerIds, orderIds, "0% loaded", false);
            ExecutePerformanceTest(customerIds, orderIds, "0% loaded", true);
            ExecutePerformanceTest(customerIds, orderIds, "50% loaded", false, (db) =>
            {
                db.Customers.Take(customerIds.All().Count() / 2).ToArray();
                db.Orders.Take(orderIds.All().Count() / 2).ToArray();
            });
            ExecutePerformanceTest(customerIds, orderIds, "50% loaded", true, (db) =>
            {
                db.Customers.Take(customerIds.All().Count() / 2).ToArray();
                db.Orders.Take(orderIds.All().Count() / 2).ToArray();
            });
            ExecutePerformanceTest(customerIds, orderIds, "100% loaded", false, (db) => { db.Customers.Include(z => z.Orders).ToArray(); });
            ExecutePerformanceTest(customerIds, orderIds, "100% loaded", true, (db) => { db.Customers.Include(z => z.Orders).ToArray(); });
        }

        static void ExecuteOnNewDbContext(Action<MyDatabase> action)
        {
            using (var database = new MyDatabase())
            {
                action(database);
            }
        }

        static void ExecutePerformanceTest(IdGenerator customers,
            IdGenerator orders,
            string title,
            bool withFinder,
            Action<MyDatabase> setup = null)
        {
            ExecuteOnNewDbContext(db =>
            {
                setup?.Invoke(db);

                FasterFind finder = withFinder ? new FasterFind(db) : null;
                title = withFinder ? $"Finder.Find: {title}" : $"DbContext.Find: {title}";

                using (new PerformanceScope($"Customers - {title}"))
                {
                    foreach (var id in customers.All())
                    {
                        if (finder != null)
                            finder.Find<Customer>(id);
                        else
                            db.Customers.Find(id);
                    }
                }
                using (new PerformanceScope($"Orders - {title}"))
                {
                    foreach (var id in orders.All())
                    {
                        if (finder != null)
                            finder.Find<Order>(id);
                        else
                            db.Orders.Find(id);
                    }
                }
            });
        }
    }
}
