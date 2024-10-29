namespace Api.DTOs.External;

public class LnHookWrapResponseDto
{
    public string invoice { get; set; }
    public string id { get; set; }
    public int finalAmount { get; set; }
}