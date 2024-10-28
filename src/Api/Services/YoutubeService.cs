using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Api.Services;

public class YoutubeService
{
    private readonly YouTubeService _youtubeService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubeService> _logger;
    private readonly IConfiguration _configuration;

    public YoutubeService(IConfiguration configuration, HttpClient httpClient, ILogger<YouTubeService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = configuration["YouTube:ApiKey"]
        });
    }

    public async Task<(bool isValid, string videoId, string error)> ValidateVideo(string videoUrl, int retries = 0)
    {
        try
        {
            string videoId = ExtractVideoId(videoUrl);
            if (string.IsNullOrEmpty(videoId))
            {
                return (false, null, "Invalid YouTube URL");
            }

            var videoRequest = _youtubeService.Videos.List("status,snippet,contentDetails");
            videoRequest.Id = videoId;
            var videoResponse = await videoRequest.ExecuteAsync();

            if (videoResponse.Items.Count == 0)
            {
                return (false, null, "Video not found");
            }

            var video = videoResponse.Items[0];
            
            // Multiple checks for playability
            if (!IsVideoPlayable(video))
            {
                // Try to find an alternative
                var alternativeVideo = await FindAlternativeVideo(video.Snippet.Title, retries);
                if (alternativeVideo != null)
                {
                    // Verify the alternative is actually playable
                    videoRequest.Id = alternativeVideo.Id;
                    var alternativeResponse = await videoRequest.ExecuteAsync();
                    if (alternativeResponse.Items.Count > 0 && 
                        IsVideoPlayable(alternativeResponse.Items[0]))
                    {
                        return (true, alternativeVideo.Id, null);
                    }
                }
                return (false, null, "Video is not playable in embedded players");
            }

            // Additional embed test for the video
            if (!await TestEmbed(videoId))
            {
                var alternativeVideo = await FindAlternativeVideo(video.Snippet.Title, retries);
                if (alternativeVideo != null && await TestEmbed(alternativeVideo.Id))
                {
                    return (true, alternativeVideo.Id, null);
                }
                return (false, null, "Video embedding test failed");
            }

            return (true, videoId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating YouTube video");
            return (false, null, "Error validating video");
        }
    }
    
    private bool IsVideoPlayable(Google.Apis.YouTube.v3.Data.Video video)
    {
        // Check multiple conditions
        return video.Status.Embeddable == true && 
               video.Status.PrivacyStatus == "public" &&
               // !video.ContentDetails.ContentRating.YtRating.HasValue && // Not age restricted
               video.Status.UploadStatus == "processed" &&
               !string.IsNullOrEmpty(video.Snippet.LiveBroadcastContent) && 
               video.Snippet.LiveBroadcastContent == "none"; // Not a live stream
    }
    
    private async Task<bool> TestEmbed(string videoId)
    {
        try
        {
            // Also check the oEmbed endpoint
            var oembedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
            var oembedResponse = await _httpClient.GetAsync(oembedUrl);

            if (!oembedResponse.IsSuccessStatusCode)
                return false;

            // Additional check using the IFrame API
            var playerUrl = $"https://www.youtube.com/embed/{videoId}?origin=https://www.youtube.com";
            var playerResponse = await _httpClient.GetAsync(playerUrl);
            
            // Check if we get redirected to the "Video Unavailable" page
            if (playerResponse.RequestMessage.RequestUri.ToString().Contains("watch?v="))
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private async Task<VideoData> FindAlternativeVideo(string originalTitle, int retries = 0)
    {
        if (retries++ > 10)
            return null;
        
        try
        {
            // Create a list of search queries to try
            var searchQueries = new[]
            {
                $"{originalTitle} karaoke",
                $"{originalTitle} karaoke version",
                $"{originalTitle} instrumental",
                $"{originalTitle} sing along",
                $"{originalTitle} cover karaoke"
            };

            foreach (var query in searchQueries)
            {
                var searchRequest = _youtubeService.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = 10;
                searchRequest.Type = "video";
                searchRequest.VideoCategoryId = "10"; // Music category
                searchRequest.VideoEmbeddable = SearchResource.ListRequest.VideoEmbeddableEnum.True__;
                searchRequest.VideoSyndicated = SearchResource.ListRequest.VideoSyndicatedEnum.True__; // Additional filter for external playback
                
                var searchResponse = await searchRequest.ExecuteAsync();

                foreach (var searchResult in searchResponse.Items)
                {
                    // Verify the video is actually playable
                    var (isValid, videoId, _) = await ValidateVideo($"https://youtube.com/watch?v={searchResult.Id.VideoId}", retries);
                    if (isValid)
                    {
                        return new VideoData
                        {
                            Id = videoId,
                            Title = searchResult.Snippet.Title,
                            OriginalTitle = originalTitle
                        };
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding alternative video");
            return null;
        }
    }

    private string ExtractVideoId(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                if (uri.Host.Contains("youtube.com"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    return query["v"];
                }
                else if (uri.Host.Contains("youtu.be"))
                {
                    return uri.AbsolutePath.TrimStart('/');
                }
            }
            return url; // Assume it's already a video ID
        }
        catch
        {
            return null;
        }
    }
}

public class VideoData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string OriginalTitle { get; set; }
}