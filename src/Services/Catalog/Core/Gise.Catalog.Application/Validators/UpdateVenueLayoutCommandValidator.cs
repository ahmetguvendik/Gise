using FluentValidation;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Microsoft.Extensions.Configuration;

namespace Gise.Catalog.Application.Validators;

public sealed class UpdateVenueLayoutCommandValidator : AbstractValidator<UpdateVenueLayoutCommand>
{
    public UpdateVenueLayoutCommandValidator(IConfiguration configuration)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Sections).ValidVenueLayout(configuration);

        // Koleksiyonda Stop "ilk hatalı elemanda dur" demek; her bölümün hatası görünsün.
        RuleForEach(x => x.Sections).Cascade(CascadeMode.Continue).SetValidator(new VenueSectionInputValidator(configuration));
    }
}
