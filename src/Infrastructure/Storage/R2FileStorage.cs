using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Courts.Interfaces;

namespace Infrastructure.Storage;
public class R2FileStorage : IFileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    public R2FileStorage(R2Options options)
    {
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);

        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl
        });

        _bucketName = options.BucketName;
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
}