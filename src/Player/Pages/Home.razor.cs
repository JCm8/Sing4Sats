using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Player.Pages;

public partial class Home
{
    private DotNetObjectReference<Home> objRef;
    private readonly HttpClient _httpClient;
    private bool videoStarted;
    private bool showNextSingerInfo;
    private Singer? currentSinger;
    private Singer? nextSinger;
    private const string API_BASE_URL = "http://localhost:5255/api";  // Configure this
    private const string AUTH_PREIMAGE = "admin";  // Configure this - matches the hash in QueueController
    
    public Home()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Auth-PreImage", AUTH_PREIMAGE);
    }
    
    protected override async Task OnInitializedAsync()
    {
        objRef = DotNetObjectReference.Create(this);
        
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
        if (!videoStarted)
        {
            videoStarted = true;
            StateHasChanged();
            
            // Enter fullscreen mode
            await JS.InvokeVoidAsync("Fullscreen.enterFullscreen");

            // Initialize YouTube player
            await JS.InvokeVoidAsync("YouTubePlayer.initialize", currentSinger.SongId, objRef);

            // Generate RoboHashes
            await GenerateRoboHashes();
            
            // Initialize the scroller
            await JS.InvokeVoidAsync("ScrollingText.init");
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
                showNextSingerInfo = true;
                StateHasChanged();
                
                // Generate identicon for next singer
                await JS.InvokeVoidAsync("IdenticonGenerator.generateIdenticon", "next-singer-identicon", nextSinger.Id.ToString(), 100);
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
                return query["v"];
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
        showNextSingerInfo = false;
        StateHasChanged();

        var nextSong = await GetNextSong();
        if (nextSong != null)
        {
            currentSinger = new Singer
            {
                Id = nextSong.UserId,
                Name = nextSong.Username,
                SongTitle = nextSong.YoutubeLink,
                SongId = ExtractVideoId(nextSong.YoutubeLink)
            };

            // If there's another song in queue, try to get it for the "next up" display
            var upcomingSong = await GetNextSong();
            nextSinger = upcomingSong != null && upcomingSong.SongId != nextSong.SongId ? new Singer
            {
                Id = upcomingSong.UserId,
                Name = upcomingSong.Username,
                SongTitle = upcomingSong.YoutubeLink,
                SongId = ExtractVideoId(upcomingSong.YoutubeLink)
            } : null;

            StateHasChanged();

            if (autoPlay)
            {
                await JS.InvokeVoidAsync("YouTubePlayer.loadVideoById", currentSinger.SongId);
                await NotifyPlayStarted(nextSong.SongId);
            }

            await JS.InvokeVoidAsync("ScrollingText.init");
        }
    }
    
    private async Task LoadNextSongOld()
    {
        // Reset overlays
        showNextSingerInfo = false;
        StateHasChanged();

        // Update current and next singer (mock)
        currentSinger = nextSinger;
        nextSinger = new Singer
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
        await JS.InvokeVoidAsync("RoboHashGenerator.generateRoboHash", "current-singer-identicon", currentSinger.Id.ToString(), 100);

        // If next singer info is being displayed, generate identicon
        if (showNextSingerInfo)
        {
            await JS.InvokeVoidAsync("RoboHashGenerator.generateRoboHash", "next-singer-identicon", nextSinger.Id.ToString(), 100);
        }
    }

    public void Dispose()
    {
        objRef?.Dispose();
    }

    public class Singer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SongTitle { get; set; }
        public string SongId { get; set; }
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