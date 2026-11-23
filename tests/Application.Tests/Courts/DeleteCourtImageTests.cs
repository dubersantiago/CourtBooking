// tests/Application.Tests/Courts/DeleteCourtImageTests.cs
using Application.Common.Exceptions;
using Application.Courts.UseCases;
using Application.Tests.Fakes;
using Domain.Courts;

namespace Application.Tests.Courts;

public class DeleteCourtImageTests
{
    private readonly InMemoryCourtRepository _courts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly DeleteCourtImage _useCase;

    public DeleteCourtImageTests()
    {
        _useCase = new DeleteCourtImage(_courts, _storage);
    }

    private Court SeedCourt(string name = "Cancha 1")
    {
        var court = new Court(name);
        _courts.Courts.Add(court);
        return court;
    }

    // Argumentos con nombre: el command tiene dos Guid seguidos y un cruce compilaría igual.
    private static DeleteCourtImageCommand Command(Guid courtId, Guid imageId) =>
        new(CourtId: courtId, ImageId: imageId);

    [Fact]
    public async Task Execute_WithExistingImage_RemovesSavesAndDeletesItsFile()
    {
        var court = SeedCourt();
        var keep = court.AddCourtImage("courts/test/keep.jpg");
        var target = court.AddCourtImage("courts/test/delete.jpg");

        await _useCase.ExecuteAsync(Command(court.Id, target.Id));

        var remaining = Assert.Single(court.Images);
        Assert.Equal(keep.Id, remaining.Id);                     // solo se fue la pedida
        Assert.Same(court, Assert.Single(_courts.Updated));       // se guardó en MySQL
        var deletedKey = Assert.Single(_storage.DeletedKeys);
        Assert.Equal("courts/test/delete.jpg", deletedKey);       // se borró el archivo de ESA imagen
    }

    [Fact]
    public async Task Execute_WhenCourtDoesNotExist_ThrowsNotFoundAndDeletesNothing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecuteAsync(Command(Guid.NewGuid(), Guid.NewGuid())));

        Assert.Empty(_courts.Updated);
        Assert.Empty(_storage.DeletedKeys);
    }

    [Fact]
    public async Task Execute_WhenImageDoesNotExist_ThrowsNotFoundAndChangesNothing()
    {
        var court = SeedCourt();
        court.AddCourtImage("courts/test/1.jpg");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecuteAsync(Command(court.Id, Guid.NewGuid())));

        Assert.Single(court.Images);
        Assert.Empty(_courts.Updated);
        Assert.Empty(_storage.DeletedKeys);
    }

    [Fact]
    public async Task Execute_WithImageOfAnotherCourt_ThrowsNotFoundAndKeepsBothCourtsIntact()
    {
        var court = SeedCourt("Cancha 1");
        var other = SeedCourt("Cancha 2");
        var otherImage = other.AddCourtImage("courts/other/1.jpg");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecuteAsync(Command(court.Id, otherImage.Id)));

        Assert.Single(other.Images);
        Assert.Empty(_courts.Updated);
        Assert.Empty(_storage.DeletedKeys);
    }

    [Fact]
    public async Task Execute_AfterDeleting_FreesASlotForANewImage()
    {
        var court = SeedCourt();
        CourtImage last = null!;
        for (var i = 0; i < Court.MaxCourtImages; i++)
            last = court.AddCourtImage($"courts/test/{i}.jpg");

        await _useCase.ExecuteAsync(Command(court.Id, last.Id));

        court.AddCourtImage("courts/test/new.jpg");               // no lanza: volvió a haber cupo
        Assert.Equal(Court.MaxCourtImages, court.Images.Count);
    }
}