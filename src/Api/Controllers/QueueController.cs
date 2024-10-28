using Api.Data;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly HashService _hashService;

    public QueueController(ApplicationDbContext context, HashService hashService)
    {
        _context = context;
        _hashService = hashService;
    }

    [HttpGet("next")]
    public async Task<ActionResult<NextSongResponseDto>> GetNextSong([FromHeader(Name = "X-Auth-PreImage")] string preImage)
    {
        if (!_hashService.VerifyHash(preImage))
        {
            return Unauthorized("Invalid authentication");
        }

        var nextSong = await _context.SongRequests
            .Include(y => y.User)
            .Where(y => y.PaidAt.HasValue && !y.PlayedAt.HasValue)
            .OrderBy(y => y.PaidAt)
            .FirstOrDefaultAsync();

        if (nextSong == null)
        {
            return NotFound("No songs in queue");
        }

        return Ok(new NextSongResponseDto()
        {
            SongId = nextSong.Id,
            YoutubeLink = nextSong.YoutubeLink,
            UserId = nextSong.UserId,
            Username = nextSong.User.Username
        });
    }

    [HttpPost("{id}/start-playing")]
    public async Task<IActionResult> StartPlaying(Guid id, [FromHeader(Name = "X-Auth-PreImage")] string preImage)
    {
        if (!_hashService.VerifyHash(preImage))
        {
            return Unauthorized("Invalid authentication");
        }

        var song = await _context.SongRequests
            .FindAsync(id);

        if (song == null)
        {
            return NotFound("Song request not found");
        }

        if (!song.PaidAt.HasValue)
        {
            return BadRequest("Song has not been paid for");
        }

        if (song.PlayedAt.HasValue)
        {
            return BadRequest("Song has already been played");
        }

        song.PlayedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok();
    }
}