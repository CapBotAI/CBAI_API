using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.BLL.Interfaces
{
    public interface IAIService
    {
        Task<List<string>> GenerateKeywordsAsync(string title, string? description, int maxKeywords = 20, CancellationToken ct = default);
    }
}
