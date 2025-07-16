using System.Linq.Expressions;
using App.DAL.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Queries.Implementations;

public class SimpleIncludeSpecification<T> : IIncludeSpecification<T> where T : class
{
    private readonly Expression<Func<T, object>> _includeExpression;

    public SimpleIncludeSpecification(Expression<Func<T, object>> includeExpression)
    {
        _includeExpression = includeExpression ?? throw new ArgumentNullException(nameof(includeExpression));
    }

    public IQueryable<T> Include(IQueryable<T> query)
    {
        return query.Include(_includeExpression);
    }
}