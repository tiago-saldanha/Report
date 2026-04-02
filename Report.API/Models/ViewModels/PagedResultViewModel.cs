namespace Report.API.Models.ViewModels;
public class PagedResultViewModel<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Count { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
