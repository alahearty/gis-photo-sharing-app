using System;
using gis_photo_sharing_app.Domain.Common;

namespace gis_photo_sharing_app.Domain.Entities
{
    public class Photo : AuditableEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? LocationName { get; set; }
        public string? Category { get; set; }
        public bool IsPublic { get; set; } = true;
        public string? TakenAt { get; set; }
        public string? CreatedByUserId { get; set; }
    }
}
