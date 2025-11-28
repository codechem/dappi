namespace Dappi.HeadlessCms.Exceptions
{
    public class PropertyNotFoundException : Exception
    {
        public Type? Resource { get; set; }
        public string? PropertyName { get; set; }

        public PropertyNotFoundException(string message) : base(message)
        {
        }

        public PropertyNotFoundException(string message, Type resource, string propertyName) : base(message)
        {
            Resource = resource;
            PropertyName = propertyName;
        }
    }
}