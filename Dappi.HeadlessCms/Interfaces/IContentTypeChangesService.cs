using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IContentTypeChangesService
    {
        Task AddContentTypeChangeAsync(string modelName, Dictionary<string, string> fields, ContentTypeState state);
        Task UpdateContentTypeChangeFieldsAsync(string modelName, Dictionary<string, string> newFields);
        IQueryable<ContentTypeChange> GetDraftsAsync();
    }
}