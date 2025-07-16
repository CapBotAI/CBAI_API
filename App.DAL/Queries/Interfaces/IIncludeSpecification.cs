namespace App.DAL.Queries.Interfaces;

public interface IIncludeSpecification<T> where T : class
{
    IQueryable<T> Include(IQueryable<T> query);
}