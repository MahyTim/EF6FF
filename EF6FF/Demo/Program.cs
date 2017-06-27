using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects.DataClasses;
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

            ExecuteTest(customerIds, orderIds,"0% loaded",false);
            ExecuteTest(customerIds, orderIds, "0% loaded", true);
            ExecuteTest(customerIds, orderIds, "50% loaded", false, (db) =>
            {
                db.Customers.Take(customerIds.All().Count() / 2).ToArray();
                db.Orders.Take(orderIds.All().Count() / 2).ToArray();

            });
            ExecuteTest(customerIds, orderIds, "50% loaded", true, (db) =>
            {
                db.Customers.Take(customerIds.All().Count() / 2).ToArray();
                db.Orders.Take(orderIds.All().Count() / 2).ToArray();

            });
            ExecuteTest(customerIds, orderIds, "100% loaded", false, (db) =>
            {
                db.Customers.Include(z => z.Orders).ToArray();
            });
            ExecuteTest(customerIds, orderIds, "100% loaded", true, (db) =>
            {
                db.Customers.Include(z => z.Orders).ToArray();
            });
        }

        static void ExecuteOnNewDbContext(Action<MyDatabase> action)
        {
            using (var database = new MyDatabase())
            {
                action(database);
            }
        }

        static void ExecuteTest(IdGenerator customers,
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
