namespace Api.DTOs;

public class CreateSongRequestRequestDto
{
    public string YoutubeLink { get; set; }
    public Guid UserId { get; set; }
}