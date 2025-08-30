using System.Text;
using Microsoft.AspNetCore.Http;

namespace App.Commons.Utils;

public static class Helpers
{
    private const long OneKB = 1024;
    private const long OneMB = OneKB * 1024;
    private const long OneGB = OneMB * 1024;

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

    #region File

    /// <summary>
    /// Convert MB sang bytes
    /// </summary>
    public static long FromMB(int megabytes) => megabytes * OneMB;

    /// <summary>
    /// Convert GB sang bytes
    /// </summary>
    public static long FromGB(int gigabytes) => gigabytes * OneGB;

    /// <summary>
    /// Convert bytes sang MB
    /// </summary>
    public static double ToMB(long bytes) => (double)bytes / OneMB;

    /// <summary>
    /// Convert bytes sang GB
    /// </summary>
    public static double ToGB(long bytes) => (double)bytes / OneGB;

    /// <summary>
    /// Format kích thước bytes thành chuỗi (KB, MB, GB) dễ đọc
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        if (bytes < OneKB)
            return $"{bytes} B";
        if (bytes < OneMB)
            return $"{(double)bytes / OneKB:N1} KB";
        if (bytes < OneGB)
            return $"{(double)bytes / OneMB:N1} MB";

        return $"{(double)bytes / OneGB:N1} GB";
    }

    public static string PathCombine(params string[] paths)
    {
        return Path.Combine(paths.Where(x => !string.IsNullOrEmpty(x)).ToArray());
    }

    public static string UrlCombine(params string[] paths)
    {
        var trimmedPaths = paths.Where(x => !string.IsNullOrEmpty(x))
                            .Select(x => x.Trim('/'))
                            .ToArray();
        return "/" + string.Join("/", trimmedPaths);
    }

    #endregion
}
