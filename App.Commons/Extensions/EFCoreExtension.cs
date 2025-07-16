using System.Reflection;

namespace App.Commons.Extensions;

public static class EfCoreExtension
{
    public static IQueryable<T> ToPagedList<T>(this IQueryable<T> list, int pageNumber, int pageSize)
    {
        return list.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    public static IEnumerable<T> ToPagedList<T>(this IEnumerable<T> list, int pageNumber, int pageSize)
    {
        return list.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}