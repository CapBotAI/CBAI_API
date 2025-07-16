using System.Linq.Expressions;

namespace App.DAL.Queries.Implementations;

public class AdvancedQueryBuilder<T> : QueryBuilder<T> where T : class
{
    private string _currentIncludePath;
    private Type _currentPropertyType;

    public AdvancedQueryBuilder() : base() { }
    
    public new AdvancedQueryBuilder<T> WithPredicate(Expression<Func<T, bool>> predicate)
    {
        base.WithPredicate(predicate);
        return this;
    }

    public new AdvancedQueryBuilder<T> WithTracking(bool tracked)
    {
        base.WithTracking(tracked);
        return this;
    }

    public new AdvancedQueryBuilder<T> WithOrderBy(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
    {
        base.WithOrderBy(orderBy);
        return this;
    }

    public AdvancedQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> includeExpression)
    {
        if (includeExpression == null)
            throw new ArgumentNullException(nameof(includeExpression));

        // Lấy path từ expression
        string path = GetPropertyPath(includeExpression);
        
        // Thêm Complex Include specification với path
        var specification = new ComplexIncludeSpecification<T>(path);
        base._options.AdvancedIncludes.Add(specification);
        
        // Lưu thông tin cho ThenInclude
        _currentIncludePath = path;
        _currentPropertyType = typeof(TProperty);
        
        return this;
    }

    public AdvancedQueryBuilder<T> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));

        if (string.IsNullOrEmpty(_currentIncludePath))
            throw new InvalidOperationException("ThenInclude must be called after Include");

        string propertyPath = GetPropertyPath(propertyExpression);
        string fullPath = $"{_currentIncludePath}.{propertyPath}";

        // Thêm Complex Include specification
        var specification = new ComplexIncludeSpecification<T>(fullPath);
        base._options.AdvancedIncludes.Add(specification);

        // Cập nhật current path cho ThenInclude tiếp theo nếu cần
        _currentIncludePath = fullPath;
        _currentPropertyType = typeof(TProperty);

        return this;
    }

    private string GetPropertyPath<TSource, TProperty>(Expression<Func<TSource, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        throw new ArgumentException("Invalid expression", nameof(expression));
    }
}