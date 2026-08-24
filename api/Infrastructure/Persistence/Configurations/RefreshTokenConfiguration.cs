using Domain.Identity;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(
            "RefreshTokens",
            table => table.HasComment(
                "Lịch sử refresh token (chỉ lưu hash). Mỗi login/refresh tạo bản ghi mới; " +
                "token cũ bị revoke nhưng giữ lại để phát hiện tái sử dụng (replay)."));
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired()
            .HasComment("Hash SHA-256 của refresh token; không lưu plaintext.");

        builder.Property(token => token.FamilyId)
            .HasComment("Nhóm token cùng phiên đăng nhập; revoke cả family khi phát hiện replay.");

        builder.Property(token => token.ExpiresAt)
            .HasComment("Thời điểm hết hạn; dùng cho cleanup retention.");

        builder.Property(token => token.RevokedAt)
            .HasComment("Thời điểm revoke; null = còn hiệu lực (nếu chưa hết hạn).");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.FamilyId);
        builder.HasIndex(token => token.ExpiresAt);
        builder.HasIndex(token => token.RevokedAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
