namespace Api.DTOs.External;

public class LnAddressWellKnownResponseDto
{
    public string callback { get; set; }
    public long maxSendable { get; set; }
    public long minSendable { get; set; }
    public string? metadata { get; set; }
    public int commentAllowed { get; set; }
    public string tag { get; set; }
}