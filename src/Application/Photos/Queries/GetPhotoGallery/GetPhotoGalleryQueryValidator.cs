using System.Linq;
using FluentValidation;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGallery;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGallery;

public class GetPhotoGalleryQueryValidator : AbstractValidator<GetPhotoGalleryQuery>
{
    public GetPhotoGalleryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 96);
        RuleFor(x => x.Filter).NotNull();
        RuleFor(x => x.Filter.Search).MaximumLength(200);
        RuleFor(x => x.Filter.Category).MaximumLength(50);
        RuleFor(x => x.Filter.Sort)
            .Must(sort => new[] { "newest", "oldest", "title", "title_desc" }
                .Contains((sort ?? "newest").ToLowerInvariant()))
            .WithMessage("Sort must be one of: newest, oldest, title, title_desc.");
        RuleFor(x => x.Filter)
            .Must(filter => !filter.From.HasValue || !filter.To.HasValue || filter.From <= filter.To)
            .WithMessage("The start date must not be later than the end date.");
    }
}
