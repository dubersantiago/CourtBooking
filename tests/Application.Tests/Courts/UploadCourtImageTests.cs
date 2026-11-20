// tests/Application.Tests/Courts/UploadCourtImageTests.cs
using Application.Common.Exceptions;
using Application.Courts.UseCases;
using Application.Tests.Fakes;
using Domain.Common;
using Domain.Courts;

namespace Application.Tests.Courts;

public class UploadCourtImageTests
{
    private const long OneMb = 1024 * 1024;

    private readonly InMemoryCourtRepository _courts = new();
    private readonly FakeFileStorage _storage = new();
    private readonly UploadCourtImage _useCase;

    public UploadCourtImageTests()
    {
        _useCase = new UploadCourtImage(_courts, _storage);
    }

    private Court SeedCourt()
    {
        var court = new Court("Cancha 1");
        _courts.Courts.Add(court);
        return court;
    }

    // Los bytes no importan: el caso de uso no lee el contenido, solo lo pasa.
    private static UploadCourtImageCommand Command(Guid courtId, string contentType = "image/jpeg", long size = OneMb) =>
        new(courtId, new MemoryStream(new byte[] { 1, 2, 3 }), contentType, size);

    [Fact]
    public async Task Execute_WithValidImage_UploadsSavesAndReturnsImageId()
    {
        var court = SeedCourt();

        var id = await _useCase.ExecuteAsync(Command(court.Id));

        var image = Assert.Single(court.Images);
        Assert.Equal(id, image.Id);
        var uploadedKey = Assert.Single(_storage.UploadedKeys);
        Assert.Equal(image.StorageKey, uploadedKey);       // R2 y MySQL apuntan al mismo archivo
        Assert.Same(court, Assert.Single(_courts.Updated));
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public async Task Execute_BuildsKeyWithCourtFolderAndExtension(string contentType, string extension)
    {
        var court = SeedCourt();

        await _useCase.ExecuteAsync(Command(court.Id, contentType));

        var key = Assert.Single(_storage.UploadedKeys);
        Assert.StartsWith($"courts/{court.Id}/", key);
        Assert.EndsWith(extension, key);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public async Task Execute_WithInvalidType_ThrowsValidationAndDoesNothing(string contentType)
    {
        var court = SeedCourt();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _useCase.ExecuteAsync(Command(court.Id, contentType)));

        Assert.Empty(_storage.UploadedKeys);
        Assert.Empty(_courts.Updated);
        Assert.Empty(court.Images);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5 * OneMb + 1)]
    public async Task Execute_WithInvalidSize_ThrowsValidationAndDoesNothing(long size)
    {
        var court = SeedCourt();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _useCase.ExecuteAsync(Command(court.Id, size: size)));

        Assert.Empty(_storage.UploadedKeys);
        Assert.Empty(_courts.Updated);
    }

    [Fact]
    public async Task Execute_WithExactlyMaxSize_Succeeds()
    {
        var court = SeedCourt();

        await _useCase.ExecuteAsync(Command(court.Id, size: 5 * OneMb));

        Assert.Single(court.Images);
    }

    [Fact]
    public async Task Execute_WhenCourtDoesNotExist_ThrowsNotFoundAndUploadsNothing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecuteAsync(Command(Guid.NewGuid())));

        Assert.Empty(_storage.UploadedKeys);
    }

    [Fact]
    public async Task Execute_WhenCourtHasMaxImages_ThrowsDomainExceptionAndUploadsNothing()
    {
        var court = SeedCourt();
        for (var i = 1; i <= Court.MaxCourtImages; i++)
            court.AddCourtImage($"courts/test/{i}.jpg");

        await Assert.ThrowsAsync<DomainException>(() =>
            _useCase.ExecuteAsync(Command(court.Id)));

        Assert.Empty(_storage.UploadedKeys);   // la regla se revisa ANTES de subir
        Assert.Empty(_courts.Updated);
    }

    [Fact]
    public async Task Execute_WhenUploadFails_DoesNotSaveCourt()
    {
        var court = SeedCourt();
        _storage.ShouldFail = true;

        await Assert.ThrowsAsync<IOException>(() =>
            _useCase.ExecuteAsync(Command(court.Id)));

        Assert.Empty(_courts.Updated);          // O1: si R2 falla, MySQL no se toca
    }
}