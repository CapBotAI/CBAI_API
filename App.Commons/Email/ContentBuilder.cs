using System.Text;
using Skincare_Product_Sale_System.Commons.Email;

namespace App.Commons.Email;

public class ContentBuilder
{
    StringBuilder _stringBuilder;

    public ContentBuilder(string content)
    {
        _stringBuilder = new StringBuilder(content);
    }

    public ContentBuilder BuildCallback(List<ObjectReplace> replaces)
    {
        foreach (var item in replaces)
        {
            _stringBuilder.Replace(item.Name, item.Value);
        }

        return this;
    }

    public string GetContent()
    {
        return _stringBuilder.ToString();
    }
}