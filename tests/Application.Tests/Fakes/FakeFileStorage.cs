using Application.Courts.Interfaces;

namespace Application.Tests.Fakes;

public class FakeFileStorage : IFileStorage
{
    public List<string> UploadedKeys { get; } = [];
    public bool ShouldFail { get; set; }   // para simular que R2 falla
    public List<string> DeletedKeys { get; } = new();

    public Task UploadAsync(string key, Stream content, string contentType)
    {
        if (ShouldFail) throw new IOException("Fallo simulado de R2");

        UploadedKeys.Add(key);
        return Task.CompletedTask;
    }

    public string GetReadUrl(string key) => $"https://fake-storage/{key}";


    public Task DeleteAsync(string key)
    {
        DeletedKeys.Add(key);
        return Task.CompletedTask;
    }
}