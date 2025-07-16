namespace App.Commons.Paging;

public class PagingModel
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Keyword { get; set; }
    public int TotalRecord { get; set; }

    public static PagingModel Default = new PagingModel()
    {
        PageNumber = 1,
        PageSize = 10
    };

    public void SetDefaultValueToPage()
    {
        if (PageNumber <= 0) PageNumber = 1;
        if (PageSize <= 0) PageSize = 10;
    }
}