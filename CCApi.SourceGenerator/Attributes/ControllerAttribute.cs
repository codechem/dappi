namespace CCApi.SourceGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CCControllerAttribute : Attribute
{
    public string ExcludeOperation { get; set; }
    public bool CreateDto { get; set; }
}

// idea for maybe [MeasureTimeDebugOnly] to give execution time for sth