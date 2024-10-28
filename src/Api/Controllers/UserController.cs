using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("check-username")]
    public async Task<ActionResult<bool>> CheckUsername([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username cannot be empty");
        }

        var exists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());

        return Ok(!exists); // Returns true if username is available
    }

    [HttpPost("register")]
    public async Task<ActionResult<Guid>> RegisterUser([FromBody] RegisterUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest("Username cannot be empty");
        }

        // Check if username already exists
        var exists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());

        if (exists)
        {
            return Conflict("Username already taken");
        }

        var user = new UserModel
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            CreatedAt = DateTime.UtcNow,
            YoutubeRequests = new List<SongRequestModel>()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(user.Id);
    }
}