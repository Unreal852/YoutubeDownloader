using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubeDownloader.Validation;

public class UrlInputResult
{
    public static readonly UrlInputResult Empty = new();

    public static UrlInputResult FromUrl(string url)
    {
        var videoId = YoutubeExplode.Videos.VideoId.TryParse(url);
        if (videoId != null)
            return new UrlInputResult { VideoId = videoId };
        var playlistId = YoutubeExplode.Playlists.PlaylistId.TryParse(url);
        if (playlistId != null)
            return new UrlInputResult { PlaylistId = playlistId };
        return Empty;
    }

    public VideoId?    VideoId    { get; init; }
    public PlaylistId? PlaylistId { get; init; }
    public bool        IsVideo    => VideoId    != null;
    public bool        IsPlaylist => PlaylistId != null;
    public bool        IsValid    => IsVideo || IsPlaylist;
}