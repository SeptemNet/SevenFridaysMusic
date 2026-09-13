using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using Microsoft.Extensions.Logging;

namespace SevenFridaysMusic
{
    
    public class YoutubeParser : IMusicProvider
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly ILogger<YoutubeParser> _logger;
        public YoutubeParser(YoutubeClient youtube, ILogger<YoutubeParser> logger)
        {
            _youtubeClient = youtube;
            _logger = logger;
        }
        public async Task<IReadOnlyList<TrackInfo>> SearchAsync(string searchText, CancellationToken ct = default)
        {

            var tracks = new List<TrackInfo>();
            var videos = await _youtubeClient.Search.GetVideosAsync(searchText, ct).CollectAsync(20);
            
            foreach (var vid in videos)
            {
                if (vid.Duration?.TotalSeconds < 720D && vid.Duration?.TotalSeconds > 60D)
                {

                    var track = new TrackInfo(vid.Id.Value, vid.Title, vid.Author.ChannelTitle, vid.Duration);
                    tracks.Add(track);

                }
            }
            
            return tracks;
        }
        public async Task<string?> DownloadAsync(string videoId, CancellationToken ct = default)
        {
            try
            {
                    var video = await _youtubeClient.Videos.GetAsync(videoId, ct);
                string ffmpegPath = OperatingSystem.IsWindows()
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg", "ffmpeg.exe")
                    : "ffmpeg";

                string uniqueName = $"{video.Title}";

                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        uniqueName = uniqueName.Replace(c.ToString(), "");
                    }

                    uniqueName = Regex.Replace(uniqueName, @"[^\w\s\-\(\)\[\]\.]", "");
                    uniqueName = Regex.Replace(uniqueName, @"\s+", " ").Trim();
                    string fileName = $"{uniqueName}_{Guid.NewGuid().ToString()[..8]}.mp3";
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    _logger.LogInformation("Downloading video {VideoId} to {FullPath}", videoId, fullPath);
                    await _youtubeClient.Videos.DownloadAsync(videoId, fullPath, o => o
                    .SetFFmpegPath(ffmpegPath));
                    _logger.LogInformation("Download completed for video {VideoId}", videoId);
                return fullPath;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error occurred while downloading video {VideoId}", videoId); return null; }

        }
    }
}
