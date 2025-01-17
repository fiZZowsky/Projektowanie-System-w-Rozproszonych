namespace Common.Models;

public class ActiveUserModel
{
    public string UserId { get; set; }
    public string ComputerId { get; set; }
    public int ClientPort { get; set; }
    public DateTime LastPing { get; set; }
}
