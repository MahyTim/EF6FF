using System;
using Library;

namespace Demo
{
    public class Order : IEntity
    {
        public Guid Id { get; set; }

        public virtual Customer Customer { get; set; }
    }
}