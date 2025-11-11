using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Services;
public class EnumService : IEnumService
{
    private readonly string _enumsFilePath;
    private readonly string _enumsPath;
    private readonly ILogger<EnumService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly DomainModelEditor _domainModelEditor;
    public EnumService(ILogger<EnumService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        DomainModelEditor domainModelEditor)
    {
        _logger = logger;
        _environment = environment;
        _domainModelEditor = domainModelEditor;
        var dataPath = configuration.GetValue<string>("DataPath") ?? "Data";
        Directory.CreateDirectory(dataPath);
        _enumsFilePath = Path.Combine(dataPath, "enums.json");
        
        _enumsPath = "Enums";
        Directory.CreateDirectory(_enumsPath);
        
        _logger.LogInformation("EnumService initialized with enums path: {ModelsPath}", Path.GetFullPath(_enumsPath));
    }

    public async Task<Dictionary<string, Dictionary<string, int>>> GetAllEnumsAsync()
    {
        try
        {
            if (!File.Exists(_enumsFilePath))
            {
                return new Dictionary<string, Dictionary<string, int>>();
            }

            var json = await File.ReadAllTextAsync(_enumsFilePath);
            var enums = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);
            return enums ?? new Dictionary<string, Dictionary<string, int>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read enums from file");
            return new Dictionary<string, Dictionary<string, int>>();
        }
    }

    public async Task<Dictionary<string, int>?> GetEnumAsync(string enumName)
    {
        var allEnums = await GetAllEnumsAsync();
        return allEnums.TryGetValue(enumName, out var enumData) ? enumData : null;
    }

    public async Task<ServiceResult<Dictionary<string, int>>> CreateEnumAsync(string enumName, List<EnumValueRequest> values)
    {
        try
        {
            var allEnums = await GetAllEnumsAsync();

            if (allEnums.ContainsKey(enumName))
            {
                return ServiceResult<Dictionary<string, int>>.ErrorResult($"Enum '{enumName}' already exists");
            }

            var enumValues = new Dictionary<string, int>();
            var usedValues = new HashSet<int>();

            foreach (var val in values)
            {
                if (enumValues.ContainsKey(val.Name))
                {
                    return ServiceResult<Dictionary<string, int>>.ErrorResult($"Duplicate enum value name '{val.Name}'");
                }

                if (usedValues.Contains(val.Value))
                {
                    return ServiceResult<Dictionary<string, int>>.ErrorResult($"Duplicate enum value '{val.Value}'");
                }

                enumValues[val.Name] = val.Value;
                usedValues.Add(val.Value);
            }

            allEnums[enumName] = enumValues;
            await SaveEnumsAsync(allEnums);
            await GenerateEnumFileAsync(enumName, enumValues);

            _logger.LogInformation("Created enum '{EnumName}' with {ValueCount} values", enumName, values.Count);
            return ServiceResult<Dictionary<string, int>>.SuccessResult(enumValues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create enum '{EnumName}'", enumName);
            return ServiceResult<Dictionary<string, int>>.ErrorResult("Failed to create enum");
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> UpdateEnumAsync(string enumName, List<EnumValueRequest> values)
    {
        try
        {
            var allEnums = await GetAllEnumsAsync();

            if (!allEnums.ContainsKey(enumName))
            {
                return ServiceResult<Dictionary<string, int>>.ErrorResult($"Enum '{enumName}' not found");
            }

            var enumValues = new Dictionary<string, int>();
            var usedValues = new HashSet<int>();

            foreach (var val in values)
            {
                if (enumValues.ContainsKey(val.Name))
                {
                    return ServiceResult<Dictionary<string, int>>.ErrorResult($"Duplicate enum value name '{val.Name}'");
                }

                if (usedValues.Contains(val.Value))
                {
                    return ServiceResult<Dictionary<string, int>>.ErrorResult($"Duplicate enum value '{val.Value}'");
                }

                enumValues[val.Name] = val.Value;
                usedValues.Add(val.Value);
            }

            allEnums[enumName] = enumValues;
            await SaveEnumsAsync(allEnums);
            await GenerateEnumFileAsync(enumName, enumValues);

            _logger.LogInformation("Updated enum '{EnumName}' with {ValueCount} values", enumName, values.Count);
            return ServiceResult<Dictionary<string, int>>.SuccessResult(enumValues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update enum '{EnumName}'", enumName);
            return ServiceResult<Dictionary<string, int>>.ErrorResult("Failed to update enum");
        }
    }

    public async Task<ServiceResult<bool>> DeleteEnumAsync(string enumName)
    {
        try
        {
            var allEnums = await GetAllEnumsAsync();

            if (!allEnums.ContainsKey(enumName))
            {
                return ServiceResult<bool>.ErrorResult($"Enum '{enumName}' not found");
            }

            allEnums.Remove(enumName);
            await SaveEnumsAsync(allEnums);
            await DeleteEnumFileAsync(enumName);

            _logger.LogInformation("Deleted enum '{EnumName}'", enumName);
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete enum '{EnumName}'", enumName);
            return ServiceResult<bool>.ErrorResult("Failed to delete enum");
        }
    }

    private async Task SaveEnumsAsync(Dictionary<string, Dictionary<string, int>> enums)
    {
        var json = JsonSerializer.Serialize(enums, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_enumsFilePath, json);
    }

    private async Task GenerateEnumFileAsync(string enumName, Dictionary<string, int> enumValues)
    {
        try
        {
            var enumFilePath = Path.Combine(_enumsPath, $"{enumName}.cs");
            var enumContent = _domainModelEditor.GenerateEnumCode(enumName, enumValues);
            await File.WriteAllTextAsync(enumFilePath, enumContent);
            _logger.LogInformation("Generated enum file for '{EnumName}' at '{FilePath}'", enumName, enumFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enum file for '{EnumName}'", enumName);
        }
    }

    private async Task DeleteEnumFileAsync(string enumName)
    {
        try
        {
            var enumFilePath = Path.Combine(_enumsPath, $"{enumName}.cs");
            if (File.Exists(enumFilePath))
            {
                File.Delete(enumFilePath);
                _logger.LogInformation("Deleted enum file for '{EnumName}' at '{FilePath}'", enumName, enumFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete enum file for '{EnumName}'", enumName);
        }
    }

    public async Task RegenerateAllEnumFilesAsync()
    {
        try
        {
            var allEnums = await GetAllEnumsAsync();
            foreach (var enumPair in allEnums)
            {
                await GenerateEnumFileAsync(enumPair.Key, enumPair.Value);
            }
            _logger.LogInformation("Regenerated {Count} enum files", allEnums.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate enum files");
        }
    }
}