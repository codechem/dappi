namespace Dappi.HeadlessCms.Interfaces
{
    public interface ICurrentDappiSessionProvider
    {
        public Guid? GetCurrentUserId();
    }
}