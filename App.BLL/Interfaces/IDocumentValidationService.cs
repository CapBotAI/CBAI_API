using App.Entities.DTOs.DocumentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.BLL.Interfaces
{
    public interface IDocumentValidationService
    {
        Task<DocumentValidationResponse> ValidateAsync([FromForm] DocumentValidationRequest req);
    }
}
