using System.Linq.Expressions;
using App.DAL.Queries.Interfaces;

namespace App.DAL.Queries
{
    public class QueryOptions<T> where T : class
    {
        public Expression<Func<T, bool>>? Predicate { get; set; }
        public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; set; }
        public bool Tracked { get; set; } = false;

        public List<Expression<Func<T, object>>> IncludeProperties { get; set; } = 
            new List<Expression<Func<T, object>>>();

        public List<IIncludeSpecification<T>> AdvancedIncludes { get; set; } = new List<IIncludeSpecification<T>>();
    }
}