using System.Linq.Expressions;

namespace App.DAL.Queries.Interfaces;

public interface IQueryBuilder<T> where T : class
{
    IQueryBuilder<T> WithPredicate(Expression<Func<T, bool>> predicate);
    IQueryBuilder<T> WithTracking(bool tracked);
    IQueryBuilder<T> WithOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy);
    IQueryBuilder<T> WithInclude(params Expression<Func<T, object>>[] includeProperty);
    QueryOptions<T> Build();
}