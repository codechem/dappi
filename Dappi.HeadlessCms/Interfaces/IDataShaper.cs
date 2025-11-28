using System.Dynamic;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IDataShaper
    {
        ExpandoObject ShapeObject<TSource>(TSource source, string? fields);
    }
}