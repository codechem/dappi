namespace Dappi.SourceGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DappiAuthorizeAttribute : Attribute
{
    public DappiAuthorizeAttribute(string[] roles, string[] methods, bool authenticated = true)
    {
        Roles = roles;
        Methods = methods;
        Authenticated = authenticated;
    }

    public DappiAuthorizeAttribute()
    {
        Authenticated = true;

    }
    
    public string[]? Roles { get; }
    public string[]? Methods { get; }
    public bool? Authenticated { get; }
}