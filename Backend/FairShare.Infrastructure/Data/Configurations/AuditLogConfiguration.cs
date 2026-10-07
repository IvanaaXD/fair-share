using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            // "Controller.Action", e.g. "AdminUser.Block".
            builder.Property(a => a.Action)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(a => a.EntityType)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(a => a.Timestamp)
                    .IsRequired();

            // The admin view always sorts by time, newest first.
            builder.HasIndex(a => a.Timestamp);

            // Restrict: audit history must survive - a user with audit entries cannot be hard-deleted.
            builder.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
