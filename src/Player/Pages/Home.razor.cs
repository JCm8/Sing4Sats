using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Player.Pages;

public partial class Home
{
    private DotNetObjectReference<Home>? _objRef;
    private readonly HttpClient _httpClient;
    private bool _videoStarted;
    private bool _showNextSingerInfo;
    private Singer? _currentSinger;
    private Singer? _nextSinger;
    private CancellationTokenSource _pollCancellation;
    private const string API_BASE_URL = "http://localhost:5255/api"; // Configure this
    private const string AUTH_PREIMAGE = "admin"; // Configure this - matches the hash in QueueController
    private const int POLLING_INTERVAL_MS = 5000; // Poll every 5 seconds

    public Home()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Auth-PreImage", AUTH_PREIMAGE);
        _pollCancellation = new CancellationTokenSource();
    }

    protected override async Task OnInitializedAsync()
    {
        _objRef = DotNetObjectReference.Create(this);
        await LoadNextSong(false);
        await JS.InvokeVoidAsync("ScrollingText.init");
    }

    private async Task<SongRequestDto?> GetNextSong()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SongRequestDto>($"{API_BASE_URL}/queue/next");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting next song: {ex.Message}");
            return null;
        }
    }

    private async Task PollForSongs()
    {
        while (!_pollCancellation.Token.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Trying to get next song in queue...");
                var nextSong = await GetNextSong();
                if (nextSong != null && nextSong.SongId != _currentSinger!.SongId)
                {
                    // Stop polling once we find a song
                    await _pollCancellation.CancelAsync();
                    await LoadNextSong();
                    return;
                }

                await Task.Delay(POLLING_INTERVAL_MS, _pollCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, exit the loop
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during song polling: {ex.Message}");
                // Continue polling despite errors
            }
        }
    }

    private async Task NotifyPlayStarted(Guid songId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{API_BASE_URL}/queue/{songId}/start-playing", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying song start: {ex.Message}");
        }
    }

    private async Task StartVideo()
    {
        if (!_videoStarted)
        {
            _videoStarted = true;
            StateHasChanged();

            // Enter fullscreen mode
            await JS.InvokeVoidAsync("Fullscreen.enterFullscreen");

            // Initialize YouTube player
            await JS.InvokeVoidAsync("YouTubePlayer.initialize", _currentSinger!.YoutubeSongId, _objRef);

            // Generate RoboHashes
            await GenerateRoboHashes();

            // Initialize the scroller
            await JS.InvokeVoidAsync("ScrollingText.init");

            // Tell the song has started
            await NotifyPlayStarted(_currentSinger.SongId);
        }
    }

    [JSInvokable]
    public async Task PlayerStateChanged(int state)
    {
        // YouTube Player States:
        // -1: unstarted, 0: ended, 1: playing, 2: paused, 3: buffering, 5: video cued
        if (state == 1) // Playing
        {
            var duration = await JS.InvokeAsync<double>("YouTubePlayer.getDuration");
            var currentTime = await JS.InvokeAsync<double>("YouTubePlayer.getCurrentTime");
            var timeRemaining = duration - currentTime;

            // Schedule next singer info 30 seconds before song ends
            if (timeRemaining > 30)
            {
                await Task.Delay((int)((timeRemaining - 30) * 1000));
                _showNextSingerInfo = true;
                StateHasChanged();

                // Generate identicon for next singer
                await JS.InvokeVoidAsync(
                    "IdenticonGenerator.generateIdenticon",
                    "next-singer-identicon",
                    _nextSinger.Id.ToString(),
                    100);
            }
        }
        else if (state == 0) // Ended
        {
            // Song ended, load next song
            await LoadNextSong();
        }
    }

    private string ExtractVideoId(string youtubeUrl)
    {
        try
        {
            var uri = new Uri(youtubeUrl);

            // Handle youtube.com/watch?v= format
            if (uri.Host.Contains("youtube.com"))
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query["v"] ?? throw new Exception("There's no video ID on the link");
            }
            // Handle youtu.be/ format
            else if (uri.Host.Contains("youtu.be"))
            {
                return uri.AbsolutePath.TrimStart('/');
            }

            return youtubeUrl; // Return as-is if no parsing needed
        }
        catch
        {
            return youtubeUrl; // Return as-is if parsing fails
        }
    }

    private async Task LoadNextSong(bool autoPlay = true)
    {
        _showNextSingerInfo = false;
        StateHasChanged();

        if (_currentSinger != null)
        {
            if (_nextSinger != null)
            {
                _currentSinger = _nextSinger;
            }
        }
        else
        {
            var nextSong = await GetNextSong();
            if (nextSong == null)
            {
                await PollForSongs();
                return;
            }

            _currentSinger = new Singer
            {
                Id = nextSong.UserId,
                Name = nextSong.Username,
                SongTitle = nextSong.YoutubeLink,
                YoutubeSongId = ExtractVideoId(nextSong.YoutubeLink),
                SongId = nextSong.SongId
            };
        }

        // If there's another song in queue, try to get it for the "next up" display
        var upcomingSong = await GetNextSong();
        if (upcomingSong == null || upcomingSong.SongId == _currentSinger!.SongId)
        {
            _ = PollForSongs();
        }
        else
        {
            _nextSinger = new Singer
            {
                Id = upcomingSong.UserId,
                Name = upcomingSong.Username,
                SongTitle = upcomingSong.YoutubeLink,
                YoutubeSongId = ExtractVideoId(upcomingSong.YoutubeLink),
                SongId = upcomingSong.SongId
            };
        }

        StateHasChanged();

        if (autoPlay)
        {
            await JS.InvokeVoidAsync("YouTubePlayer.loadVideoById", _currentSinger.YoutubeSongId);
            await NotifyPlayStarted(_currentSinger.SongId);
        }

        await JS.InvokeVoidAsync("ScrollingText.init");
    }

    private async Task LoadNextSongOld()
    {
        // Reset overlays
        _showNextSingerInfo = false;
        StateHasChanged();

        // Update current and next singer (mock)
        _currentSinger = _nextSinger;
        _nextSinger = new Singer
        {
            Id = Guid.Parse("43a128eb-0ac9-47dd-bf96-ff60d32bd979"),
            Name = "Alice Johnson",
            SongTitle = "Let It Be"
        };
        StateHasChanged();

        // Load the next video
        await JS.InvokeVoidAsync("YouTubePlayer.loadVideoById", "ve0lHrKIlF4");

        // Initialize the scroller
        await JS.InvokeVoidAsync("ScrollingText.init");
    }

    private async Task GenerateRoboHashes()
    {
        // Generate identicon for current singer
        await JS.InvokeVoidAsync("RoboHashGenerator.generateRoboHash", "current-singer-identicon",
            _currentSinger.Id.ToString(), 100);

        // If next singer info is being displayed, generate identicon
        if (_showNextSingerInfo)
        {
            await JS.InvokeVoidAsync("RoboHashGenerator.generateRoboHash", "next-singer-identicon",
                _nextSinger.Id.ToString(), 100);
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

    public class Singer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SongTitle { get; set; }
        public Guid SongId { get; set; }
        public string YoutubeSongId { get; set; }
        public string QRCodeUrl { get; set; }
    }

    public class SongRequestDto
    {
        public Guid SongId { get; set; }
        public string YoutubeLink { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}