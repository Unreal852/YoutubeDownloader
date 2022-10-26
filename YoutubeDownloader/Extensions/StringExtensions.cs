namespace YoutubeDownloader.Extensions;

public static class StringExtensions
{
    public static string RemoveInvalidChars(this string str)
    {
        return str.Replace("[", "")
                  .Replace("]", "")
                  .Replace("/", "");
    }
}