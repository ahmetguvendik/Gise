using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Validators;

namespace Gise.Catalog.UnitTests.Validators;

public sealed class CreateEventCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    private readonly CreateEventCommandValidator _validator = new(new FixedTimeProvider(Now));

    private static CreateEventCommand ValidCommand() => new()
    {
        Title = "Duman Konseri",
        Venue = "Harbiye Açıkhava",
        StartsAt = Now.AddDays(30)
    };

    [Fact]
    public void Gecerli_komut_hatasiz_gecer()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Bos_alanlar_alan_bazli_turkce_hata_uretir()
    {
        var command = ValidCommand();
        command.Title = "";
        command.Venue = "";

        var errors = _validator.Validate(command).Errors;

        Assert.Contains(errors, e => e.PropertyName == nameof(CreateEventCommand.Title) && e.ErrorMessage == "Etkinlik adı gereklidir.");
        Assert.Contains(errors, e => e.PropertyName == nameof(CreateEventCommand.Venue) && e.ErrorMessage == "Mekân gereklidir.");
    }

    [Fact]
    public void Cok_uzun_ad_reddedilir()
    {
        var command = ValidCommand();
        command.Title = new string('a', 201);

        Assert.Contains(_validator.Validate(command).Errors, e => e.PropertyName == nameof(CreateEventCommand.Title));
    }

    [Fact]
    public void Gecmis_tarih_reddedilir()
    {
        var command = ValidCommand();
        command.StartsAt = Now.AddHours(-1);

        Assert.Contains(_validator.Validate(command).Errors, e => e.ErrorMessage == "Etkinlik tarihi gelecekte olmalıdır.");
    }
}
