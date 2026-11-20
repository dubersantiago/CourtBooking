// tests/Application.Tests/Courts/GetCourtImagesTests.cs
using Application.Common.Exceptions;
using Application.Courts.UseCases;
using Application.Tests.Fakes;
using Domain.Courts;

namespace Application.Tests.Courts;

public class GetCourtImagesTests
{
    private readonly InMemoryCourtRepository _courts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly GetImages _useCase;

    public GetCourtImagesTests()
    {
        _useCase = new GetImages(_courts, _storage);
    }

    private Court SeedCourt()
    {
        var court = new Court("Cancha 1");
        _courts.Courts.Add(court);
        return court;
    }

    [Fact]
    public async Task Execute_WhenCourtDoesNotExist_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecuteAsync(new GetCourtImagesQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task Execute_WithoutImages_ReturnsEmptyList()
    {
        var court = SeedCourt();

        var images = await _useCase.ExecuteAsync(new GetCourtImagesQuery(court.Id));

        Assert.Empty(images);
    }

    [Fact]
    public async Task Execute_ReturnsEachImageWithTheUrlOfItsOwnKey()
    {
        var court = SeedCourt();
        var first = court.AddCourtImage("courts/test/1.jpg");
        var second = court.AddCourtImage("courts/test/2.png");

        var images = await _useCase.ExecuteAsync(new GetCourtImagesQuery(court.Id));

        Assert.Equal(2, images.Count);
        Assert.Contains(images, v => v.Id == first.Id && v.Url == "https://fake-storage/courts/test/1.jpg");
        Assert.Contains(images, v => v.Id == second.Id && v.Url == "https://fake-storage/courts/test/2.png");
    }
}