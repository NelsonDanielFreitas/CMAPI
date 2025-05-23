using CMAPI.Data;
using CMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services.Chat;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db) => _db = db;

    public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        message.Id = Guid.NewGuid();
        message.SentAt = DateTime.UtcNow;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid avariaId)
    {
        return _db.ChatMessages
            .Where(m => m.AvariaId == avariaId)
            .OrderBy(m => m.SentAt)
            .ToListAsync()
            .ContinueWith(t => (IEnumerable<ChatMessage>)t.Result);
    }

    public async Task AddReadReceiptAsync(Guid messageId, Guid userId)
    {
        var receipt = new MessageReadReceipt
        {
            Id = Guid.NewGuid(),
            ChatMessageId = messageId,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        };
        _db.MessageReadReceipts.Add(receipt);
        await _db.SaveChangesAsync();
    }
}