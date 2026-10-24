using FluentValidation;

namespace gis_photo_sharing_app.Application.Photos.Commands.UpdatePhoto;

public class UpdatePhotoCommandValidator : AbstractValidator<UpdatePhotoCommand>
{
    public UpdatePhotoCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.LocationName).MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(50);
        RuleFor(x => x.TakenAt).MaximumLength(40);
    }
}
