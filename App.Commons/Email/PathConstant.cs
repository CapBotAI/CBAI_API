using System.Text;

namespace Skincare_Product_Sale_System.Commons.Email;

public class PathConstant
{
    public static readonly string ConfirmEmail = Path.Combine("Email", "ConfirmEmail", "index.html");

    /// <summary>
    /// deceprated
    /// </summary>
    /// <param name="relative"></param>
    /// <returns></returns>
    public static string GetFilePath(string relative)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relative);
    }
}