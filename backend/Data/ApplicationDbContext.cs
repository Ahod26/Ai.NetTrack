using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using backend.Models.Domain;

namespace backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApiUser>
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

  public DbSet<Chat> Chats { get; set; }
  public DbSet<ChatMessage> ChatMessages { get; set; }
  public DbSet<NewsItem> NewsItems { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Chat>()
      .HasIndex(c => c.UserId);  // GetChatsByUserIdAsync, GetUserChatCountAsync

    modelBuilder.Entity<Chat>()
      .HasIndex(c => new { c.UserId, c.LastMessageAt });  // GetChatsByUserIdAsync with ordering

    modelBuilder.Entity<ChatMessage>()
      .HasIndex(m => m.ChatId);  // GetMessagesAsync

    modelBuilder.Entity<ChatMessage>()
      .HasIndex(m => new { m.ChatId, m.CreatedAt });  // GetMessagesAsync with ordering

    // NewsItem indexes
    modelBuilder.Entity<NewsItem>()
      .HasIndex(n => n.SourceType);

    modelBuilder.Entity<NewsItem>()
      .HasIndex(n => n.PublishedDate);

    modelBuilder.Entity<NewsItem>()
      .HasIndex(n => new { n.SourceType, n.PublishedDate });

    modelBuilder.Entity<NewsItem>()
      .HasIndex(n => n.Url);
  }
}