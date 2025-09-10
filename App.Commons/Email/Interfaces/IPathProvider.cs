namespace App.Commons.Email.Interfaces;

public interface IPathProvider
{
    string GetEmailTemplatePath(string templatePath);
    string GetBasePath();
}