using CMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Data;

public class AppDbContext : DbContext
{
    
    
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<MultiFactoring> MultiFactorings { get; set; }
    public DbSet<TipoStatusAvaria> TipoStatusAvaria { get; set; }
    public DbSet<TipoUrgencia> TipoUrgencia { get; set; }
    public DbSet<Avaria> Avaria { get; set; }
    public DbSet<AvariaComentario> AvariaComentarios { get; set; }
    public DbSet<AvariaAtribuicao> AvariaAtribuicoes { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<AssetType> AssetTypes { get; set; }
    public DbSet<AssetStatus> AssetStatuses { get; set; }  
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Status)
            .WithMany(s => s.Assets)
            .HasForeignKey(a => a.AssetStatusId);
        
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.IdRole);

        modelBuilder.Entity<MultiFactoring>()
            .HasOne(mf => mf.User)
            .WithMany(u => u.MultiFactorings)
            .HasForeignKey(mf => mf.UserId);

        modelBuilder.Entity<Avaria>()
            .HasOne(a => a.User)
            .WithMany(u => u.AvariaReported)
            .HasForeignKey(a => a.UserId);

        modelBuilder.Entity<Avaria>()
            .HasOne(a => a.Technician)
            .WithMany(u => u.AvariaAssigned)
            .HasForeignKey(a => a.TechnicianId);

        modelBuilder.Entity<Avaria>()
            .HasOne(a => a.Urgencia)
            .WithMany(tu => tu.Avaria)
            .HasForeignKey(a => a.IdUrgencia);

        modelBuilder.Entity<Avaria>()
            .HasOne(a => a.Status)
            .WithMany(ts => ts.Avaria)
            .HasForeignKey(a => a.IdStatus);

        modelBuilder.Entity<Avaria>()
            .HasOne(a => a.Asset)
            .WithMany(at => at.Avaria)
            .HasForeignKey(a => a.AssetId);

        modelBuilder.Entity<AvariaComentario>()
            .HasOne(ac => ac.Avaria)
            .WithMany(a => a.Comentarios)
            .HasForeignKey(ac => ac.AvariaId);

        modelBuilder.Entity<AvariaComentario>()
            .HasOne(ac => ac.User)
            .WithMany(u => u.AvariaComentarios)
            .HasForeignKey(ac => ac.UserId);

        modelBuilder.Entity<AvariaAtribuicao>()
            .HasOne(aa => aa.Avaria)
            .WithMany(a => a.Atribuicoes)
            .HasForeignKey(aa => aa.AvariaId);

        modelBuilder.Entity<AvariaAtribuicao>()
            .HasOne(aa => aa.AssignedBy)
            .WithMany(u => u.AvariaAtribuicoesBy)
            .HasForeignKey(aa => aa.AtribuidoPor);

        modelBuilder.Entity<AvariaAtribuicao>()
            .HasOne(aa => aa.Technician)
            .WithMany(u => u.AvariaAtribuicoesAssigned)
            .HasForeignKey(aa => aa.TechnicianId);

        modelBuilder.Entity<Asset>()
            .HasOne(a => a.AssetType)
            .WithMany(at => at.Assets)
            .HasForeignKey(a => a.AssetTypeId);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Avaria)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AvariaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.AvariaAtribuicao)
            .WithMany(a => a.Notifications)
            .HasForeignKey(n => n.AvariaAtribuicaoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enum conversions for Notification
        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.ResponseStatus)
            .HasConversion<string>();

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Avaria)
            .WithMany(a => a.ChatMessages)
            .HasForeignKey(cm => cm.AvariaId);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Sender)
            .WithMany(u => u.ChatMessagesSent)
            .HasForeignKey(cm => cm.SenderId);

        modelBuilder.Entity<MessageReadReceipt>()
            .HasOne(mrr => mrr.ChatMessage)
            .WithMany(cm => cm.ReadReceipts)
            .HasForeignKey(mrr => mrr.ChatMessageId);

        modelBuilder.Entity<MessageReadReceipt>()
            .HasOne(mrr => mrr.User)
            .WithMany(u => u.MessagesRead)
            .HasForeignKey(mrr => mrr.UserId);
    }
}