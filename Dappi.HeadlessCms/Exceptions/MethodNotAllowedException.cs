namespace Dappi.HeadlessCms.Exceptions
{
    public class MethodNotAllowedException : Exception
    {
        string Method { get; set; }
        
        public MethodNotAllowedException(string message) : base(message)
        {
            
        }

        public MethodNotAllowedException(string message, string method) : base(message)
        {
            Method = method;   
        }
    }
}