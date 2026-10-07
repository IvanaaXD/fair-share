using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.Property(n => n.Type)
                    .IsRequired();

            builder.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(1000);

            builder.Property(n => n.CreatedAt)
                    .IsRequired();

            // The unread counter (badge in the UI) is queried on almost every page load.
            builder.HasIndex(n => new { n.UserId, n.IsRead });

            builder.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
