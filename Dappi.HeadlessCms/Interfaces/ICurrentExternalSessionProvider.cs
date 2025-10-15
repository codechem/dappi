namespace Dappi.HeadlessCms.Interfaces
{
    public interface ICurrentExternalSessionProvider
    {
        public string? GetCurrentUserId();
    }
}