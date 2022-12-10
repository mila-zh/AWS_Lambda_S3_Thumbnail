using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Net.Sockets;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Thumbnail;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }
    
    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(EventBridgeEvent input, ILambdaContext context)
    {
        var s3EventBucketName = input.detail.bucket.name;
        var s3EventObjectKey = input.detail.@object.key;

        try
        {
            var rs = await this.S3Client.GetObjectMetadataAsync(s3EventBucketName, s3EventObjectKey);

            //check if the file is image and in the upload folder
            if (rs.Headers.ContentType.StartsWith("image/") && s3EventObjectKey.StartsWith("uploads/"))
            {
                //new key for thumbnail
                var newKey = $"thumbnails/{s3EventObjectKey.Split("/").Last()}";
                using (GetObjectResponse response = await S3Client.GetObjectAsync(
                    s3EventBucketName,
                    s3EventObjectKey))
                {
                    using (Stream responseStream = response.ResponseStream)
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            using (var memstream = new MemoryStream())
                            {
                                var buffer = new byte[512];
                                var bytesRead = default(int);
                                while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                                    memstream.Write(buffer, 0, bytesRead);

                                // Perform image manipulation 
                                using (var transformedImage = GcImagingOperations.GetConvertedImage(memstream.ToArray()))
                                {

                                    PutObjectRequest putRequest = new PutObjectRequest()
                                    {
                                        BucketName = s3EventBucketName,
                                        Key = newKey,
                                        ContentType = rs.Headers.ContentType,
                                        InputStream = transformedImage,
                                    };
                                    await S3Client.PutObjectAsync(putRequest);

                                }
                            }
                        }
                    }
                }
            }

            return rs.Headers.ContentType;
        }
        catch(Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3EventObjectKey} from bucket {s3EventBucketName}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }
}
