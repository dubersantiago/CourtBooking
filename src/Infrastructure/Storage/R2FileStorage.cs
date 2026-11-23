using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Courts.Interfaces;

namespace Infrastructure.Storage;
public class R2FileStorage : IFileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private static readonly TimeSpan UrlLifetime = TimeSpan.FromHours(1);
    public R2FileStorage(R2Options options)
    {
        AWSConfigsS3.UseSignatureVersion4 = true;

        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);

        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl
        });

        _bucketName = options.BucketName;
    }

    public string GetReadUrl(string key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(UrlLifetime)
        };

        return _client.GetPreSignedURL(request); 
    }

    public async Task UploadAsync(string key, Stream content, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _client.PutObjectAsync(request);
    }

    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };
        await _client.DeleteObjectAsync(request);
    }
}