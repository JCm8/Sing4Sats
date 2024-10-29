namespace Api.DTOs;

public class CreateSongRequestResponseDto
{
    public Guid Id { get; set; }
    public string? Invoice { get; set; }
    public int Amount { get; set; }
    public bool IsAlternativeVideo { get; set; }
}