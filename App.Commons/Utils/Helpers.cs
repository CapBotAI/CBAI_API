using System.Text;

namespace App.Commons.Utils;

public static class Helpers
{
    #region STRING

    /// <summary>
    /// Converts to camelcase.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <returns></returns>
    public static string ToCamelCase(this string str)
    {
        string lower = str.ToLower();
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            if (i == 0)
                stringBuilder.Append(lower[i]);
            else
                stringBuilder.Append(str[i]);
        }
        return stringBuilder.ToString();
    }

    #endregion
}