using App.DAL.Context;
using App.DAL.Interfaces;
using App.DAL.Queries;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Implementations
{
	public class RepoBase<T> : IRepoBase<T> where T : class
	{
		private readonly MyDbContext _context;
		protected readonly DbSet<T> _dbSet;
		public RepoBase(MyDbContext context)
		{
			_context = context;
			_dbSet = _context.Set<T>();
		}


		public async Task<T> CreateAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
			return entity;
		}

		public async Task CreateAllAsync(List<T> entities)
		{
			await _dbSet.AddRangeAsync(entities);
		}

		public Task DeleteAsync(T entity)
		{
			if (_context.Entry<T>(entity).State == EntityState.Detached)
			{
				_dbSet.Attach(entity);
			}
			_dbSet.Remove(entity);

			return Task.CompletedTask;
		}

		public Task DeleteAllAsync(List<T> entities)
		{
			_dbSet.RemoveRange(entities);
			return Task.CompletedTask;
		}

		public IQueryable<T> Get(QueryOptions<T> options)
		{
			IQueryable<T> query = _dbSet;

			if (options.Tracked == false)
			{
				query = query.AsNoTracking();
			}

			// Xử lý Include thông thường
			if (options.IncludeProperties?.Any() ?? false)
			{
				foreach (var includeProperty in options.IncludeProperties)
				{
					query = query.Include(includeProperty);
				}
			}

			// Xử lý Advanced Include
			if (options.AdvancedIncludes?.Any() ?? false)
			{
				foreach (var includeSpec in options.AdvancedIncludes)
				{
					query = includeSpec.Include(query);
				}
			}

			if (options.Predicate != null)
			{
				query = query.Where(options.Predicate);
			}

			if (options.OrderBy != null)
			{
				query = options.OrderBy(query);
			}

			return query;
		}

		//public Task UpdateAsync(T entity)
		//{

		//	if (_context.Entry<T>(entity).State == EntityState.Detached)
		//	{
		//		_dbSet.Attach(entity);
		//	}
		//	_dbSet.Update(entity);

		//	return Task.CompletedTask;
		//}
        public Task UpdateAsync(T entity)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                var key = _context.Model.FindEntityType(typeof(T)).FindPrimaryKey()
                    .Properties.Select(p => p.PropertyInfo.GetValue(entity)).ToArray();
                var trackedEntity = _dbSet.Find(key);
                if (trackedEntity != null)
                {
                    _context.Entry(trackedEntity).CurrentValues.SetValues(entity);
                    return Task.CompletedTask;
                }
                _dbSet.Attach(entity);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Update(entity);
            }
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<T>> GetAllAsync(QueryOptions<T> options)
		{
			return await Get(options).ToListAsync();
		}

		public async Task<T> GetSingleAsync(QueryOptions<T> options)
		{
			return await Get(options).FirstOrDefaultAsync();
		}

		public async Task<bool> AnyAsync(QueryOptions<T> options)
		{
			if (options.Predicate != null)
			{
				var result = await _dbSet.AnyAsync(options.Predicate);
				return result;
			}
			return false;
		}
	}
}
