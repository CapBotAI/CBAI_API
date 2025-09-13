using App.Commons.Email.Interfaces;
using App.Commons.Email.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace App.Commons.Email.Implementations;

public class PathProvider : IPathProvider
{
    private readonly EmailTemplateOptions _options;
    private readonly string _basePath;
    
    public PathProvider(IOptions<EmailTemplateOptions> options, IHostingEnvironment env)
    {
        _options = options.Value;
        _basePath = string.IsNullOrEmpty(_options.BasePath) ? env.ContentRootPath : _options.BasePath;
    }
    
    public string GetEmailTemplatePath(string templatePath)
    {
        return Path.Combine(GetBasePath(), _options.RootPath, templatePath);
    }

    public string GetBasePath()
    {
        return _basePath;
    }
}