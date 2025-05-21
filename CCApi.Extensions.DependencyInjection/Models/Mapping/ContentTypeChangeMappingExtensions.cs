using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CCApi.Extensions.DependencyInjection.Models.Mapping
{
    public static class ContentTypeChangeMappingExtensions
    {
        public static IQueryable<ContentTypeChangeDto> ToDtos(this IQueryable<ContentTypeChange> contentTypeChanges) =>
            contentTypeChanges.Select(contentTypeChange => contentTypeChange.ToDto());

        public static ContentTypeChangeDto ToDto(this ContentTypeChange contentTypeChange) =>
            new()
            {
                Fields = JsonSerializer.Deserialize<Dictionary<string, string>>(contentTypeChange.Fields)!,
                ModelName = contentTypeChange.ModelName,
                IsPublished = contentTypeChange.IsPublished,
                ModifiedAt = contentTypeChange.ModifiedAt,
                ModifiedBy = contentTypeChange.ModifiedBy,
                Id = contentTypeChange.Id,
            };
    }
}