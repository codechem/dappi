// public class BaseFilter
// {
//     public interface IPagingFilter
//     {
//         int Limit { get; set; }
//         int Offset { get; set; }
//     }

//     public interface ISortingFilter
//     {
//         string SortBy { get; set; }
//         SortDirection SortDirection { get; set; }
//     }

//     public interface IGenericFilter : ISortingFilter, IPagingFilter
//     {
//     }

//     public enum SortDirection
//     {
//         Ascending = 1,
//         Descending = -1
//     }

//     public class PagingFilter : IGenericFilter
//     {
//         public int Limit { get; set; } = 10;
//         public int Offset { get; set; }

//         public string SortBy { get; set; } = "Id";
//         public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
//     }
// }