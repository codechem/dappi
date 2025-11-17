namespace Dappi.Core.Attributes;

public enum AuthorizeMethods
{
    Get = 1,
    Post = 2,
    Put = 3,
    Delete = 4
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DappiAuthorizeAttribute : Attribute
{
    public string[]? Roles { get; set; } = [];
    
    public AuthorizeMethods[] Methods { get; set; } = [];
}
