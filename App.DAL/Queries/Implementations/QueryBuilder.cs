using System.Linq.Expressions;
using App.DAL.Queries.Interfaces;

namespace App.DAL.Queries.Implementations
{
	public class QueryBuilder<T> : IQueryBuilder<T> where T : class
	{
		protected readonly QueryOptions<T> _options = new QueryOptions<T>();

		public IQueryBuilder<T> WithPredicate(Expression<Func<T, bool>> predicate)
		{
			_options.Predicate = predicate;
			return this;
		}

		public IQueryBuilder<T> WithTracking(bool tracked)
		{
			_options.Tracked = tracked;
			return this;
		}

		public IQueryBuilder<T> WithOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
		{
			_options.OrderBy = orderBy;
			return this;
		}

		public IQueryBuilder<T> WithInclude(params Expression<Func<T, object>>[] includeProperty)
		{
			_options.IncludeProperties.AddRange(includeProperty);
			return this;
		}

		public QueryOptions<T> Build()
		{
			return _options;
		}
	}
}
