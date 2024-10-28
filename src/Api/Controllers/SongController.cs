using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SongController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly LnbitsService _lnbitsService;
    private readonly YoutubeService _youtubeService;
    private readonly IConfiguration _configuration;

    public SongController(ApplicationDbContext context, LnbitsService lnbitsService, YoutubeService youtubeService, IConfiguration configuration)
    {
        _context = context;
        _lnbitsService = lnbitsService;
        _youtubeService = youtubeService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateRequest([FromBody] CreateSongRequestRequestDto request)
    {
        // Validate YouTube video
        var (isValid, videoId, error) = await _youtubeService.ValidateVideo(request.YoutubeLink);
        
        if (!isValid)
        {
            return BadRequest(new { error });
        }
        
        // If videoId is different from the original, it means we found an alternative
        bool isAlternative = !request.YoutubeLink.Contains(videoId);
        string finalUrl = isAlternative ? 
            $"https://www.youtube.com/watch?v={videoId}" : 
            request.YoutubeLink;
        
        // Amount in sats for the song request
        var amount = int.Parse(_configuration["SongRequest:Amount"]);
        var description = $"Song request: {finalUrl}";

        // Generate LNbits invoice
        var (invoice, paymentHash) = await _lnbitsService.CreateInvoice(amount, description);
        
        var songRequest = new SongRequestModel
        {
            YoutubeLink = finalUrl,
            UserId = request.UserId,
            Invoice = invoice,
            PaymentHash = paymentHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.SongRequests.Add(songRequest);
        await _context.SaveChangesAsync();
        
        return Ok(new CreateSongRequestResponseDto
        {
            Id = songRequest.Id,
            Invoice = invoice,
            Amount = amount,
            IsAlternativeVideo = isAlternative
        });
    }
}