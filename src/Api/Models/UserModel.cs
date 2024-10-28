namespace Api.Models;

public class UserModel
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<SongRequestModel> YoutubeRequests { get; set; }
}