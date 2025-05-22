namespace CCApi.Extensions.DependencyInjection.Interfaces
{
    public interface ICurrentSessionProvider
    {
        public Guid? GetCurrentUserId();
    }
}