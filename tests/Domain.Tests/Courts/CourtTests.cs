// tests/Domain.Tests/Courts/CourtTests.cs
using Domain.Common;
using Domain.Courts;
using Domain.Reservations;

namespace Domain.Tests.Courts;

public class CourtTests
{
    private static TimeRange Range(int startHour, int endHour) =>
        new TimeRange(new DateTime(2026, 10, 14, startHour, 0, 0),
                      new DateTime(2026, 10, 14, endHour, 0, 0));

    // ---------- Constructor ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() => new Court(name!));
    }

    [Fact]
    public void Constructor_WithValidName_TrimsNameAndStartsActive()
    {
        var court = new Court("  Cancha 1  ");

        Assert.NotEqual(Guid.Empty, court.Id);
        Assert.Equal("Cancha 1", court.Name);
        Assert.True(court.IsActive);
    }

    // ---------- Deactivate ----------

    [Fact]
    public void Deactivate_WhenActive_SetsInactive()
    {
        var court = new Court("Cancha 1");

        court.Deactivate();

        Assert.False(court.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var court = new Court("Cancha 1");
        court.Deactivate();

        Assert.Throws<DomainException>(() => court.Deactivate());
    }

    // ---------- Reserve ----------

    [Fact]
    public void Reserve_WithNoExistingReservations_ReturnsConfirmedReservation()
    {
        var court = new Court("Cancha 1");
        var userId = Guid.NewGuid();
        var range = Range(17, 18);

        var reservation = court.Reserve(userId, range, []);   // [] = colección vacía

        Assert.Equal(court.Id, reservation.CourtId);
        Assert.Equal(userId, reservation.UserId);
        Assert.Equal(range, reservation.TimeRange);
        Assert.Equal(ReservationStatus.Confirmada, reservation.Status);
    }

    [Fact]
    public void Reserve_WhenCourtIsInactive_ThrowsDomainException()
    {
        var court = new Court("Cancha 1");
        court.Deactivate();

        Assert.Throws<DomainException>(() => court.Reserve(Guid.NewGuid(), Range(17, 18), []));
    }

    [Fact]
    public void Reserve_WhenOverlapsConfirmedReservation_ThrowsDomainException()
    {
        var court = new Court("Cancha 1");
        var existing = court.Reserve(Guid.NewGuid(), Range(17, 19), []);

        Assert.Throws<DomainException>(() =>
            court.Reserve(Guid.NewGuid(), Range(18, 20), [existing]));
    }

    [Fact]
    public void Reserve_WhenOverlappingReservationIsCancelled_Succeeds()
    {
        var court = new Court("Cancha 1");
        var existing = court.Reserve(Guid.NewGuid(), Range(17, 19), []);
        existing.Cancelar(new DateTime(2026, 10, 14, 16, 0, 0));

        var reservation = court.Reserve(Guid.NewGuid(), Range(18, 20), [existing]);

        Assert.NotNull(reservation);
    }

    [Fact]
    public void Reserve_WhenOverlappingReservationIsFromAnotherCourt_Succeeds()
    {
        var court = new Court("Cancha 1");
        var otherCourt = new Court("Cancha 2");
        var otherReservation = otherCourt.Reserve(Guid.NewGuid(), Range(17, 19), []);

        var reservation = court.Reserve(Guid.NewGuid(), Range(18, 20), [otherReservation]);

        Assert.NotNull(reservation);
    }

    [Fact]
    public void Reserve_WhenOnlyTouchingExistingReservation_Succeeds()
    {
        var court = new Court("Cancha 1");
        var existing = court.Reserve(Guid.NewGuid(), Range(17, 18), []);

        var reservation = court.Reserve(Guid.NewGuid(), Range(18, 19), [existing]);

        Assert.NotNull(reservation);
    }

    [Fact]
    public void Reserve_WithNullExistingReservations_ThrowsArgumentNullException()
    {
        var court = new Court("Cancha 1");

        Assert.Throws<ArgumentNullException>(() => court.Reserve(Guid.NewGuid(), Range(17, 18), null!));
    }

    [Fact]
    public void Reserve_WithNullRange_ThrowsArgumentNullException()
    {
        var court = new Court("Cancha 1");

        Assert.Throws<ArgumentNullException>(() => court.Reserve(Guid.NewGuid(), null!, []));
    }
        // ---------- Images ----------

    private static Court CourtWithImages(int count)
    {
        var court = new Court("Cancha 1");
        for (var i = 1; i <= count; i++)
            court.AddCourtImage($"courts/test/{i}.jpg");
        return court;
    }

    [Fact]
    public void Constructor_StartsWithoutImages()
    {
        var court = new Court("Cancha 1");

        Assert.Empty(court.Images);
    }

    [Fact]
    public void AddCourtImage_WithValidKey_AddsAndReturnsTheImage()
    {
        var court = new Court("Cancha 1");

        var image = court.AddCourtImage("courts/test/1.jpg");

        Assert.NotEqual(Guid.Empty, image.Id);
        Assert.Equal("courts/test/1.jpg", image.StorageKey);
        Assert.Single(court.Images);
        Assert.Contains(image, court.Images);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddCourtImage_WithInvalidKey_ThrowsAndAddsNothing(string? key)
    {
        var court = new Court("Cancha 1");

        Assert.Throws<DomainException>(() => court.AddCourtImage(key!));
        Assert.Empty(court.Images);
    }

    [Fact]
    public void AddCourtImage_UpToTheMax_Succeeds()
    {
        var court = CourtWithImages(Court.MaxCourtImages);

        Assert.Equal(Court.MaxCourtImages, court.Images.Count);
    }

    [Fact]
    public void AddCourtImage_WhenAtTheMax_ThrowsAndKeepsTheMax()
    {
        var court = CourtWithImages(Court.MaxCourtImages);

        Assert.Throws<DomainException>(() => court.AddCourtImage("courts/test/extra.jpg"));
        Assert.Equal(Court.MaxCourtImages, court.Images.Count);
    }

    [Fact]
    public void AddCourtImage_ToInactiveCourt_Succeeds()
    {
        var court = new Court("Cancha 1");
        court.Deactivate();

        court.AddCourtImage("courts/test/1.jpg");

        Assert.Single(court.Images);
    }

    [Fact]
    public void RemoveCourtImage_WhenExists_RemovesAndReturnsIt()
    {
        var court = new Court("Cancha 1");
        var image = court.AddCourtImage("courts/test/1.jpg");
        court.AddCourtImage("courts/test/2.jpg");

        var removed = court.RemoveCourtImage(image.Id);

        Assert.Same(image, removed);                 // Same = el mismo objeto, no solo "igual"
        Assert.DoesNotContain(image, court.Images);
        Assert.Single(court.Images);
    }

    [Fact]
    public void RemoveCourtImage_WhenImageIsFromAnotherCourt_ThrowsAndRemovesNothing()
    {
        var court = CourtWithImages(2);
        var otherImage = new Court("Cancha 2").AddCourtImage("courts/other/1.jpg");

        Assert.Throws<DomainException>(() => court.RemoveCourtImage(otherImage.Id));
        Assert.Equal(2, court.Images.Count);
    }

    [Fact]
    public void RemoveCourtImage_WhenAtTheMax_AllowsAddingAnotherOne()
    {
        var court = new Court("Cancha 1");
        var first = court.AddCourtImage("courts/test/first.jpg");
        for (var i = 2; i <= Court.MaxCourtImages; i++)
            court.AddCourtImage($"courts/test/{i}.jpg");

        court.RemoveCourtImage(first.Id);
        court.AddCourtImage("courts/test/new.jpg");

        Assert.Equal(Court.MaxCourtImages, court.Images.Count);
    }

    [Fact]
    public void Images_CannotBeModifiedFromOutside()
    {
        var court = new Court("Cancha 1");
        var foreignImage = new Court("Cancha 2").AddCourtImage("courts/other/1.jpg");

        // Intento "tramposo": convertir la colección a ICollection para poder llamar Add
        var asCollection = (ICollection<CourtImage>)court.Images;

        Assert.Throws<NotSupportedException>(() => asCollection.Add(foreignImage));
        Assert.Empty(court.Images);
    }
}