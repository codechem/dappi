namespace Dappi.HeadlessCms.Interfaces
{
    public interface ICurrentSessionProvider
    {
        public string? GetCurrentUserId();
    }
}