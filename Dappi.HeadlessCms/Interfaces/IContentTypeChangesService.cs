using Dappi.HeadlessCms.Enums;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IContentTypeChangesService
    {
        Task AddContentTypeChangeAsync(string modelName, Dictionary<string, string> fields, ContentTypeState state);
        Task UpdateContentTypeChangeFieldsAsync(string modelName, Dictionary<string, string> newFields);
    }
}