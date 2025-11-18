using System.Linq.Dynamic.Core;
using System.Text;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Extensions
{
    public static class QueryExtensions
    {
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, List<Filter>? filters)
        {
            if (filters == null)
            {
                return query;
            }

            var sb = new StringBuilder();


            foreach (var filter in filters)
            {
                var t = typeof(T);
                var startQuery = string.Empty;
                var endQuery = string.Empty;
                Type? prev = null;
                if (filter.Fields == null)
                {
                    throw new ArgumentException("Fields cannot be null");
                }

                foreach (var field in filter.Fields)
                {
                    try
                    {
                        var prop = prev != null ? prev.GetProperty(field) : t.GetProperty(field);
                        var isCollection = prop != null && prop.PropertyType.IsGenericType && typeof(ICollection<>)
                            .IsAssignableFrom(prop.PropertyType.GetGenericTypeDefinition());

                        if (isCollection)
                        {
                            startQuery += $"{field}.Any(";
                            endQuery += ")";
                            prev = prop?.PropertyType.GetGenericArguments()[0];
                        }
                        else
                        {
                            if (prev is not null && prev.IsGenericType)
                            {
                                startQuery += field;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(startQuery) && startQuery.LastOrDefault() == '(')
                                {
                                    startQuery += field;
                                }
                                else
                                {
                                    startQuery += string.IsNullOrEmpty(startQuery) ? field : $".{field}";
                                }
                            }
                            prev = prop?.PropertyType;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                var queryString = BuildFilter(filter.Operation, filter.Value.ConvertValue(), startQuery, endQuery);
                if (filters.Count == 1)
                {
                    sb.Append(queryString);
                }
                else
                {
                    if (filters.Count > 1 && filters.IndexOf(filter) == filters.Count - 1)
                    {
                        sb.Append($" {queryString} ");
                    }
                    else
                    {
                        sb.Append($" {queryString} {ConvertOperator(filter.Operator)} ");
                    }
                }
            }

            var fullQuery = sb.ToString();
            try
            { 
                return query.Where(fullQuery);
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while filtering the collection.");
            }
        }

        private static string ConvertOperator(Operator @operator)
        {
            return @operator == Operator.Or ? "||" : "&&";
        }
        
        private static string BuildFilter(Operation operation, object value, string startQuery, string endQuery = "")
        {
            switch (operation)
            {
                //Equals
                case Operation.Eq:
                    return $"{startQuery} == {value.SurroundWithQuotes()}{endQuery}";
                //Equals Ignore Case
                case Operation.Eqic:
                    return $"{startQuery}.ToLower() == {value.SurroundWithQuotes(true)}{endQuery}";
                //Not Equals
                case Operation.Ne:
                    return $"{startQuery} != {value.SurroundWithQuotes()}{endQuery}";
                //Less Than
                case Operation.Lt:
                    return $"{startQuery} < {value.SurroundWithQuotes()}{endQuery}";
                //Less Than Or Equal To
                case Operation.Lte:
                    return $"{startQuery} <= {value.SurroundWithQuotes()}{endQuery}";
                //Greater Than
                case Operation.Gt:
                    return $"{startQuery} > {value.SurroundWithQuotes()}{endQuery}";
                //Greater Than Or Equal To
                case Operation.Gte:
                    return $"{startQuery} >= {value.SurroundWithQuotes()}{endQuery}";
                //Contains
                case Operation.C:
                    return $"{startQuery}.Contains({value.SurroundWithQuotes()}){endQuery}";
                //Contains ignore case
                case Operation.Cic:
                    return $"{startQuery}.ToLower().Contains({value.SurroundWithQuotes(true)}){endQuery}";
                //Not Contains
                case Operation.Nc:
                    return $"NOT {startQuery}.Contains({value.SurroundWithQuotes()}){endQuery}";
                //Not Contains Ignore Case
                case Operation.Ncic:
                    return $"NOT {startQuery}.ToLower().Contains({value.SurroundWithQuotes(true)}){endQuery}";
                //Included in List
                case Operation.In:
                    if (value is IEnumerable<object> values)
                        return $"{startQuery} IN @{values.SurroundWithQuotes()}{endQuery}";
                    throw new ArgumentException($"Invalid value for operation{nameof(Operation.In)}: {value}");
                //Not included in the List
                case Operation.Notin:
                    if (value is IEnumerable<object> notInValues)
                        return $"{startQuery} NOT IN {notInValues}{endQuery}";
                    throw new ArgumentException($"Invalid value for operation{nameof(Operation.In)}: {value}");
                //Starts With
                case Operation.Sw:
                    return $"{startQuery}.StartsWith({value.SurroundWithQuotes()}){endQuery}";
                //Ends With
                case Operation.Ew:
                    return $"{startQuery}.EndsWith({value.SurroundWithQuotes()}){endQuery}";
                case Operation.Null:
                    return $"{startQuery} == null{endQuery}";
                case Operation.Notnull:
                    return $"{startQuery} != null{endQuery}";
                default:
                    throw new ArgumentException($"Unknown operator: {operation.ToString()}");
            }
        }

        private static object SurroundWithQuotes(this object value, bool ignoreCase = false)
        {
            if (value is string)
            {
                if (ignoreCase)
                {
                    return $"\"{value.ToString()?.ToLower()}\"";
                }
                return $"\"{value}\"";
            }
            return value;
        }
    }
}