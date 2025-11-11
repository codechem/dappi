using Dappi.HeadlessCms.Controllers;

namespace Dappi.HeadlessCms.Interfaces;

public interface IEnumService
{
    Task<Dictionary<string, Dictionary<string, int>>> GetAllEnumsAsync();
    Task<Dictionary<string, int>?> GetEnumAsync(string enumName);
    Task<ServiceResult<Dictionary<string, int>>> CreateEnumAsync(string enumName, List<EnumValueRequest> values);
    Task<ServiceResult<Dictionary<string, int>>> UpdateEnumAsync(string enumName, List<EnumValueRequest> values);
    Task<ServiceResult<bool>> DeleteEnumAsync(string enumName);
    Task RegenerateAllEnumFilesAsync();
}

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static ServiceResult<T> SuccessResult(T data)
    {
        return new ServiceResult<T> { Success = true, Data = data };
    }

    public static ServiceResult<T> ErrorResult(string errorMessage)
    {
        return new ServiceResult<T> { Success = false, ErrorMessage = errorMessage };
    }
}