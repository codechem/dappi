namespace Dappi.HeadlessCms.Interfaces
{
    public interface ICurrentSessionProvider
    {
        public Guid? GetCurrentUserId();
    }
}