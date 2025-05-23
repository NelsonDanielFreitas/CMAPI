using CMAPI.Models;

namespace CMAPI.Services.Chat;

public interface IChatService
{
    Task<ChatMessage> SaveMessageAsync(ChatMessage message);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid avariaId);
    Task AddReadReceiptAsync(Guid messageId, Guid userId);
}