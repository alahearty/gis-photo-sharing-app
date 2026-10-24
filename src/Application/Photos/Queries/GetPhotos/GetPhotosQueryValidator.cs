using System.Linq;
using FluentValidation;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

public class GetPhotosQueryValidator : AbstractValidator<GetPhotosQuery>
{
    public GetPhotosQueryValidator()
    {
        RuleFor(x => x.Filter).NotNull();
        RuleFor(x => x.Filter.Search).MaximumLength(200);
        RuleFor(x => x.Filter.Category).MaximumLength(50);
        RuleFor(x => x.Filter.Sort)
            .Must(sort => new[] { "newest", "oldest", "title", "title_desc" }
                .Contains((sort ?? "newest").ToLowerInvariant()))
            .WithMessage("Sort must be one of: newest, oldest, title, title_desc.");

        RuleFor(x => x.Filter.MinLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Filter.MaxLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Filter.MinLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Filter.MaxLongitude).InclusiveBetween(-180, 180);

        RuleFor(x => x.Filter)
            .Must(filter => !filter.MinLatitude.HasValue || !filter.MaxLatitude.HasValue ||
                            filter.MinLatitude <= filter.MaxLatitude)
            .WithMessage("Minimum latitude must not be greater than maximum latitude.");

        RuleFor(x => x.Filter)
            .Must(filter => !filter.MinLongitude.HasValue || !filter.MaxLongitude.HasValue ||
                            filter.MinLongitude <= filter.MaxLongitude)
            .WithMessage("Minimum longitude must not be greater than maximum longitude.");

        RuleFor(x => x.Filter)
            .Must(filter => !filter.From.HasValue || !filter.To.HasValue || filter.From <= filter.To)
            .WithMessage("The start date must not be later than the end date.");
    }
}
