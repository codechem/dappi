using System.Text.Json;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Services
{
    public class ContentTypeChangesService : IContentTypeChangesService
    {
        private readonly ICurrentDappiSessionProvider _currentSessionProvider;
        private readonly DappiDbContext _dbContext;
        
        public ContentTypeChangesService(ICurrentDappiSessionProvider currentSessionProvider,  IDbContextAccessor dappiDbContextAccessor)
        {
            _currentSessionProvider = currentSessionProvider;
            _dbContext = dappiDbContextAccessor.DbContext;
        }

        public async Task AddContentTypeChangeAsync(string modelName, Dictionary<string, string> fields, ContentTypeState state)
        {
            var contentTypeChange = new ContentTypeChange
            {
                ModelName = modelName,
                Fields = JsonSerializer.Serialize(fields),
                ModifiedBy = _currentSessionProvider.GetCurrentUserId() ?? Guid.Empty,
                State = state
            };

            _dbContext.ContentTypeChanges.Add(contentTypeChange);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateContentTypeChangeFieldsAsync(string modelName, Dictionary<string, string> newFields)
        {
            try
            {
                var contentTypeChangeForModel = await _dbContext.ContentTypeChanges
                    .Where(ctc => ctc.ModelName == modelName && ctc.State == ContentTypeState.PendingPublish)
                    .OrderByDescending(ctc => ctc.ModifiedAt)
                    .FirstOrDefaultAsync();

                if (contentTypeChangeForModel is not null)
                {
                    var oldFields =
                        JsonSerializer.Deserialize<Dictionary<string, string>>(contentTypeChangeForModel.Fields);
                    foreach (var kvp in newFields)
                    {
                        oldFields?.Add(kvp.Key, kvp.Value);
                    }

                    contentTypeChangeForModel.ModifiedAt = DateTimeOffset.UtcNow;
                    contentTypeChangeForModel.Fields = JsonSerializer.Serialize(oldFields);
                }
                else
                {
                    contentTypeChangeForModel = new ContentTypeChange
                    {
                        ModelName = modelName,
                        Fields = JsonSerializer.Serialize(newFields),
                        State = ContentTypeState.PendingPublish
                    };
                    _dbContext.ContentTypeChanges.Add(contentTypeChangeForModel);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating ContentTypeChanges: {ex.Message}");
                throw;
            }
        }
    }
}