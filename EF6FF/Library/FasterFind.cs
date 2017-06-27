using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Library
{
    public class FasterFind : IFasterFind
    {
        private readonly DbContext _dbContext;
        public FasterFind(DbContext dbContext)
        {
            _dbContext = dbContext;
            var objectContext = ((IObjectContextAdapter)this._dbContext).ObjectContext;
            objectContext.ObjectStateManager.ObjectStateManagerChanged += AddOrRemoveLoadedEntities;
        }
        private readonly Dictionary<Guid, IEntity> _loadedEntities = new Dictionary<Guid, IEntity>();
        private readonly HashSet<Guid> _deletedEntities = new HashSet<Guid>();

        private void AddOrRemoveLoadedEntities(object sender, System.ComponentModel.CollectionChangeEventArgs e)
        {
            var entity = e.Element as IEntity;
            if (entity != null)
            {
                if (e.Action == CollectionChangeAction.Add)
                {
                    _loadedEntities[entity.Id] = entity;
                }
                else if (e.Action == CollectionChangeAction.Remove)
                {
                    if (_loadedEntities.ContainsKey(entity.Id))
                    {
                        _loadedEntities.Remove(entity.Id);
                    }
                    _deletedEntities.Add(entity.Id);
                }
                else if (e.Action == CollectionChangeAction.Refresh)
                {
                    _loadedEntities.Clear();
                    _deletedEntities.Clear();
                }
            }
        }
        public T FindOrDefault<T>(Expression<Func<T, bool>> expression) where T : class, IEntity
        {
            var func = expression.Compile();
            T result = this.GetLocalEntities<T>().FirstOrDefault(func);
            if (result == null)
            {
                result = this._dbContext.Set<T>().AsQueryable().Where(expression).FirstOrDefault();
            }
            return result;
        }
        public IEnumerable<TEntity> GetLocalEntities<TEntity>() where TEntity : class
        {
            const EntityState statesToInclude = EntityState.Added | EntityState.Modified | EntityState.Unchanged;
            var objectContext = ((IObjectContextAdapter)this._dbContext).ObjectContext;
            return
                objectContext
                    .ObjectStateManager
                    .GetObjectStateEntries(statesToInclude)
                    .Where(e => e.Entity is TEntity)
                    .Select(e => (TEntity)e.Entity);
        }
        public TEntity Find<TEntity>(Guid id) where TEntity : class, IEntity
        {
            IEntity result = null;
            _loadedEntities.TryGetValue(id, out result);
            var find = result as TEntity;
            if (find != null)
            {
                return find;
            }
            if (_deletedEntities.Contains(id))
                return null;
            return FetchById<TEntity>(id);
        }
        private TEntity FetchById<TEntity>(Guid id) where TEntity : class, IEntity
        {
            return this._dbContext.Set<TEntity>().FirstOrDefault(z => z.Id == id);
        }
    }
}