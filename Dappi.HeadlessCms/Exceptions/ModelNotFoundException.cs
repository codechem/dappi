namespace Dappi.HeadlessCms.Exceptions;

public class ModelNotFoundException : Exception
{
    public string? ModelName { get; set; }

    public ModelNotFoundException(string message) : base(message)
    {
    }

    public ModelNotFoundException(string message, string modelName) : base(message)
    {
        ModelName = modelName;
    }
}
