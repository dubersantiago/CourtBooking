// tests/Application.Tests/Courts/GetCourtsTests.cs
using Application.Courts.UseCases;
using Application.Tests.Fakes;
using Domain.Courts;

namespace Application.Tests.Courts;

public class GetCourtsTests
{
    private readonly InMemoryCourtRepository _courts = new();
    private readonly GetCourts _useCase;

    public GetCourtsTests()
    {
        _useCase = new GetCourts(_courts);
    }

    private Court SeedCourt(string name)
    {
        var court = new Court(name);
        _courts.Courts.Add(court);
        return court;
    }

    [Fact]
    public async Task Execute_WithoutCourts_ReturnsEmptyList()
    {
        var courts = await _useCase.ExecuteAsync();

        Assert.Empty(courts);
    }

    [Fact]
    public async Task Execute_ReturnsEachCourtWithItsIdAndName()
    {
        var first = SeedCourt("Cancha 1");
        var second = SeedCourt("Cancha 2");

        var courts = await _useCase.ExecuteAsync();

        Assert.Equal(2, courts.Count);
        Assert.Contains(courts, v => v.Id == first.Id && v.Name == "Cancha 1");
        Assert.Contains(courts, v => v.Id == second.Id && v.Name == "Cancha 2");
    }

    [Fact]
    public async Task Execute_PreservesRepositoryOrder()
    {
        var first = SeedCourt("Cancha A");
        var second = SeedCourt("Cancha B");
        var third = SeedCourt("Cancha C");

        var courts = await _useCase.ExecuteAsync();

        Assert.Equal([first.Id, second.Id, third.Id], courts.Select(v => v.Id));
    }
}
