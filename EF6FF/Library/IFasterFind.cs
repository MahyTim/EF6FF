using System;
using System.Linq.Expressions;

namespace Library
{
    public interface IFasterFind
    {
        T FindOrDefault<T>(Expression<Func<T, bool>> expression) where T : class, IEntity;
        TEntity Find<TEntity>(Guid id) where TEntity : class, IEntity;
    }
}