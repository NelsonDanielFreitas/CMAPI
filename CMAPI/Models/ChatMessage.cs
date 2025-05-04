namespace CMAPI.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid AvariaId { get; set; }
    public Guid SenderId { get; set; }
    public string Message { get; set; }
    public DateTime SentAt { get; set; }

    public Avaria Avaria { get; set; }
    public User Sender { get; set; }
    public ICollection<MessageReadReceipt> ReadReceipts { get; set; }
}