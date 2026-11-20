namespace Infrastructure.Storage;
public sealed class R2Options
{
    public const string SectionName = "R2";

    public string ServiceUrl { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public string BucketName { get; set; } = "";

}