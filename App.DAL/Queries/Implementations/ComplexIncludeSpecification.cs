using App.DAL.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Queries.Implementations;

public class ComplexIncludeSpecification<T> : IIncludeSpecification<T> where T : class
{
    private readonly string _includePath;

    public ComplexIncludeSpecification(string includePath)
    {
        if (string.IsNullOrWhiteSpace(includePath))
            throw new ArgumentNullException(nameof(includePath));
        
        _includePath = includePath;
    }

    public IQueryable<T> Include(IQueryable<T> query)
    {
        return query.Include(_includePath);
    }
}