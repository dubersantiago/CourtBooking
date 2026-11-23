namespace Application.Courts.Interfaces;
public interface IFileStorage
{
    Task UploadAsync(string key, Stream content, string contentType);
    string GetReadUrl(string key);
    Task DeleteAsync(string key);
}