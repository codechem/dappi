namespace CCApi.Extensions.DependencyInjection.Models
{
    public class PagedResponseDto<T>
    {
        public int Total { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public List<T> Data { get; set; }
    }
}