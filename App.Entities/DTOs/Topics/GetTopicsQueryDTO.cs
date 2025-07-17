using App.Commons.Paging;

namespace App.Entities.DTOs.Topics;

public class GetTopicsQueryDTO : PagingModel
{
    public int? SemesterId { get; set; }
    public int? CategoryId { get; set; }
}