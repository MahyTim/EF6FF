using System;
using System.Collections.Generic;

namespace Demo
{
    public class IdGenerator
    {
        private HashSet<Guid> _ids = new HashSet<Guid>();

        public Guid Next()
        {
            var id = Guid.NewGuid();
            _ids.Add(id);
            return id;
        }

        public IEnumerable<Guid> All() => _ids;
    }
}