using App.BLL.Interfaces;
using App.Commons.BaseAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CapBot.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : BaseAPIController
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;

        public FileController(IFileService fileService,
            ILogger<FileController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment)
        {
            this._fileService = fileService;
            this._logger = logger;
            this._environment = environment;
            this._configuration = configuration;
            this._hostEnvironment = hostEnvironment;
        }
    }
}
