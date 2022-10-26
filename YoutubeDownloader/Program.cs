using System.Reflection;
using Spectre.Console;
using Syroot.Windows.IO;
using YoutubeDownloader.Extensions;
using YoutubeDownloader.Validation;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader;

public static class Program
{
    public static void Main(string[] args)
    {
        PrintHeader();

        UrlInputResult url = AskYoutubeUrl();
        HandleInput(url);
    }

    private static void HandleInput(UrlInputResult result)
    {
        var youtube = new YoutubeClient();
        var playlistTitle = "youtube downloads";
        IReadOnlyList<IVideo>? videos = null;
        if (result.IsVideo)
        {
            var video = youtube.Videos.GetAsync(result.VideoId!.Value).GetAwaiter().GetResult();
            videos = new[] { video };
        }
        else if (result.IsPlaylist)
        {
            var playlist = youtube.Playlists.GetAsync(result.PlaylistId!.Value).GetAwaiter().GetResult();
            videos = youtube.Playlists.GetVideosAsync(result.PlaylistId!.Value).GetAwaiter().GetResult();
            playlistTitle = playlist.Title;
        }

        if (videos == null)
            return;

        PrintResults(videos);

        var filePath = Path.Combine(KnownFolders.Downloads.Path, GetSafeFilename(playlistTitle));
        Directory.CreateDirectory(filePath);

        AnsiConsole.Write(new Rule($"[bold lime]Downloading {videos.Count} videos[/]") { Alignment = Justify.Left, Style = Style.Parse("red") });

        foreach (var video in videos)
        {
            var status = AnsiConsole.Status().Spinner(Spinner.Known.Christmas);
            status.Start($"[dodgerblue2]Downloading {video.Title.RemoveInvalidChars()}[/]", context =>
            {
                var streamManifest = youtube.Videos.Streams.GetManifestAsync(video.Id).GetAwaiter().GetResult();
                youtube.Videos.Streams.DownloadAsync(streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate(),
                        Path.Combine(filePath, GetSafeFilename(video.Title) + ".mp3")).GetAwaiter().GetResult();
            });
            Thread.Sleep(100); // Try to avoid being rate limited by youtube.
        }

        AnsiConsole.MarkupLine("[green3]Videos have been downloaded in your downloads folder.[/]");
        AnsiConsole.MarkupLine("[gold3]Press any key to exit.[/]");

        while (!Console.KeyAvailable)
        {
            Thread.Sleep(10);
        }
    }

    private static string GetSafeFilename(string filename)
    {
        return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
    }

    private static UrlInputResult AskYoutubeUrl()
    {
        var prompt = new TextPrompt<string>("Youtube url: ")
                    .PromptStyle("green")
                    .ValidationErrorMessage("[red]This is not a valid youtube url[/]")
                    .Validate(str => UrlInputResult.FromUrl(str).IsValid
                             ? ValidationResult.Success()
                             : ValidationResult.Error("[red]This is not a valid youtube url[/]"));
        var url = prompt.Show(AnsiConsole.Console);
        return UrlInputResult.FromUrl(url);
    }

    private static void PrintHeader()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version ?? Version.Parse("0.0.0");
        AnsiConsole.Write(new Rule($"[bold lime]Youtube Downloader {version.ToString(3)}[/]") { Alignment = Justify.Left, Style = Style.Parse("red") });
    }

    private static void PrintResults(IReadOnlyList<IVideo> videos)
    {
        if (videos.Count == 0)
        {
            AnsiConsole.Markup("[red]No videos found.[/]");
            return;
        }

        var table = new Table()
                   .Title($"[green1]Found {videos.Count} video(s)[/]")
                   .AddColumn("[darkorange3]Author[/]")
                   .AddColumn("[darkorange3]Title[/]")
                   .AddColumn("[darkorange3]Duration[/]")
                   .Border(TableBorder.Rounded)
                   .BorderStyle(Style.Parse("dim red"))
                   .ShowFooters();
        foreach (var video in videos)
        {
            table.AddRow(video.Author.ChannelTitle.RemoveInvalidChars(),
                    video.Title.RemoveInvalidChars(),
                    (video.Duration ?? TimeSpan.Zero).ToString(@"hh\:mm\:ss"));
        }

        AnsiConsole.Write(table);
    }
}