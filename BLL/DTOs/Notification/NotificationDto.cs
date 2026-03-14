namespace BLL.DTOs.Notification;

public class NotificationDto
{
    public long Id { get; set; }
    //public string? UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? SourceType { get; set; }

}
