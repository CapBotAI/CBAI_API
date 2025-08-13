using System;
using App.Commons.Paging;

namespace App.Entities.DTOs.Phases;

public class GetPhasesQueryDTO : PagingModel
{
    public int? SemesterId { get; set; }
}
