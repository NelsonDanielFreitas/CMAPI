namespace CMAPI.Models;

public class MessageReadReceipt
{
    public Guid Id { get; set; }
    public Guid ChatMessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; }

    public ChatMessage ChatMessage { get; set; }
    public User User { get; set; }
}