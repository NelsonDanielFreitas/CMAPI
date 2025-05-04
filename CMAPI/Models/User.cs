namespace CMAPI.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    
    public Guid IdRole { get; set; }
    public string? refreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndTime { get; set; }
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    public string? EmailVerificationCode { get; set; }
    public bool EmailVerified { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }

    public Role Role { get; set; }
    public ICollection<MultiFactoring> MultiFactorings { get; set; }
    public ICollection<Avaria> AvariaReported { get; set; }
    public ICollection<Avaria> AvariaAssigned { get; set; }
    public ICollection<AvariaComentario> AvariaComentarios { get; set; }
    public ICollection<AvariaAtribuicao> AvariaAtribuicoesBy { get; set; }
    public ICollection<AvariaAtribuicao> AvariaAtribuicoesAssigned { get; set; }
    public ICollection<Notification> Notifications { get; set; }
    public ICollection<ChatMessage> ChatMessagesSent { get; set; }
    public ICollection<MessageReadReceipt> MessagesRead { get; set; }
}
