using gis_photo_sharing_app.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gis_photo_sharing_app.Infrastructure.Persistence.Configurations
{
    public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
            builder.Property(p => p.FilePath).HasMaxLength(500).IsRequired();
            builder.Property(p => p.ThumbnailPath).HasMaxLength(500);
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.LocationName).HasMaxLength(200);
            builder.Property(p => p.Category).HasMaxLength(50);
            builder.Property(p => p.TakenAt).HasMaxLength(40);
            builder.Property(p => p.CreatedByUserId).HasMaxLength(450);

            builder.HasIndex(p => p.CreatedByUserId);
            builder.HasIndex(p => p.Created);
            builder.HasIndex(p => p.Category);
            builder.HasIndex(p => p.IsPublic);
            builder.HasIndex(p => new { p.Latitude, p.Longitude });
        }
    }
}
