using System.Data.Entity;

namespace Demo
{
    public class MyDatabase : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }

    }
}