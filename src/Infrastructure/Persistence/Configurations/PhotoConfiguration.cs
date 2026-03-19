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
        }
    }
}
