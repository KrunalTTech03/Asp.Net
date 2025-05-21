using System.Linq.Expressions;
using StudentCoreWebApi.DTOs;

namespace StudentCoreWebApi.Helpers
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<GenericFilter> filters)
        {
            foreach (var filter in filters)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, filter.Column);

                Expression propertyAsString = property.Type != typeof(string)
                    ? Expression.Call(property, "ToString", null)
                    : (Expression)property;

                var constant = Expression.Constant(filter.Value);
                Expression body;

                switch (filter.Condition.ToLower())
                {
                    case "contains":
                        body = Expression.Call(propertyAsString, "Contains", null, constant);
                        break;
                    case "notcontains":
                        body = Expression.Not(Expression.Call(propertyAsString, "Contains", null, constant));
                        break;
                    case "equals":
                        body = Expression.Equal(propertyAsString, constant);
                        break;
                    case "notequals":
                        body = Expression.NotEqual(propertyAsString, constant);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported condition: {filter.Condition}");
                }

                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
    }
}
