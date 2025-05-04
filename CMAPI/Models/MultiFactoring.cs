namespace CMAPI.Models;

public class MultiFactoring
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MultifactorCode { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Validated { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
}