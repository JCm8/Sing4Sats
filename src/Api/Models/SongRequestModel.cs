namespace Api.Models;

public class SongRequestModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserModel User { get; set; }
    public string YoutubeLink { get; set; }
    public string Invoice { get; set; }
    public string PaymentHash { get; set; }
    public bool IsAlternativeVideo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PlayedAt { get; set; }
}