using System.Dynamic;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IDataShaperService
    {
        ExpandoObject ShapeObject<TSource>(TSource source, string? fields);
    }
}