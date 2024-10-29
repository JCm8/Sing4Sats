using Api.Data;
using Api.DTOs;
using Api.DTOs.External;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LnUrlController: ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly HashService _hashService;
    private readonly LnUrlService _lnUrlService;
    private readonly LnAddressService _lnAddressService;
    private readonly string _baseUrl;
    
    public LnUrlController(IConfiguration configuration, ApplicationDbContext context, HashService hashService, LnUrlService lnUrlService, LnAddressService lnAddressService)
    {
        _context = context;
        _hashService = hashService;
        _lnUrlService = lnUrlService;
        _lnAddressService = lnAddressService;
        _baseUrl = configuration["LnUrl:BaseUrl"];
    }
    
    [HttpGet("{songId}/url")]
    public async Task<ActionResult<GetLnUrlForSongResponseDto>> GetLnUrlForSong(Guid songId, [FromHeader(Name = "X-Auth-PreImage")] string preImage)
    {
        if (!_hashService.VerifyHash(preImage))
        {
            return Unauthorized("Invalid authentication");
        }

        var song = await _context.SongRequests
            .Where(x => x.Id.Equals(songId))
            .FirstOrDefaultAsync();

        if (song == null)
        {
            return NotFound("Song not found");
        }

        return Ok(new GetLnUrlForSongResponseDto(_lnUrlService.Encode(song.Id)));
    }

    [HttpGet("data/{songId}")]
    public async Task<ActionResult<LnAddressWellKnownResponseDto>> GetData(Guid songId)
    {
        var song = await _context.SongRequests
            .Include(x=>x.User)
            .Where(x => x.Id.Equals(songId))
            .FirstOrDefaultAsync();
        
        if (song == null)
        {
            return NotFound("Song not found");
        }

        var payInfo = await _lnAddressService.GetLnurlPayEndpointInfo(song.User.Username);

        payInfo.metadata = null;
        payInfo.callback = $"{_baseUrl}/api/LnUrl/callback/{songId}";
        
        return payInfo;
    }
}