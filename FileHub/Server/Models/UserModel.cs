namespace Server.Models;

public class UserModel
{
    public int Id { get; set; }
    public string? ComputerId { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public DateTime? LastPing { get; set; }
}
