namespace Api.DTOs;

public class NextSongResponseDto
{
    public Guid SongId { get; set; }
    public string YoutubeLink { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; }
}