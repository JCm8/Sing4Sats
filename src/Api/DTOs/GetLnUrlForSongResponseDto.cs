namespace Api.DTOs;

public class GetLnUrlForSongResponseDto(string lnUrl)
{
    public string LnUrl { get; set; } = lnUrl;
}