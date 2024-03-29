using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;


using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using Amazon.S3;
using Amazon.S3.Model;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLab4;

public class Function
{
    /// <summary>
    /// The default minimum confidence used for detecting labels.
    /// </summary>
    public const float DEFAULT_MIN_CONFIDENCE = 90f;

    /// <summary>
    /// The name of the environment variable to set which will override the default minimum confidence level.
    /// </summary>
    public const string MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME = "MinConfidence";

    IAmazonS3 S3Client { get; }

    IAmazonRekognition RekognitionClient { get; }

    IAmazonDynamoDB DynamoDB { get; }

    float MinConfidence { get; set; } = DEFAULT_MIN_CONFIDENCE;

    HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".png", ".jpg", ".jpeg" };

    /// <summary>
    /// Default constructor used by AWS Lambda to construct the function. Credentials and Region information will
    /// be set by the running Lambda environment.
    /// 
    /// This constuctor will also search for the environment variable overriding the default minimum confidence level
    /// for label detection.
    /// </summary>
    public Function()
    {
        this.S3Client = new AmazonS3Client();
        this.RekognitionClient = new AmazonRekognitionClient();
        this.DynamoDB = new AmazonDynamoDBClient();

        var environmentMinConfidence = System.Environment.GetEnvironmentVariable(MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME);
        if(!string.IsNullOrWhiteSpace(environmentMinConfidence))
        {
            float value;
            if(float.TryParse(environmentMinConfidence, out value))
            {
                this.MinConfidence = value;
                Console.WriteLine($"Setting minimum confidence to {this.MinConfidence}");
            }
            else
            {
                Console.WriteLine($"Failed to parse value {environmentMinConfidence} for minimum confidence. Reverting back to default of {this.MinConfidence}");
            }
        }
        else
        {
            Console.WriteLine($"Using default minimum confidence of {this.MinConfidence}");
        }
    }

    /// <summary>
    /// Constructor used for testing which will pass in the already configured service clients.
    /// </summary>
    /// <param name="s3Client"></param>
    /// <param name="rekognitionClient"></param>
    /// <param name="minConfidence"></param>
    public Function(IAmazonS3 s3Client, IAmazonRekognition rekognitionClient, float minConfidence, IAmazonDynamoDB dynamoDB)
    {
        this.S3Client = s3Client;
        this.RekognitionClient = rekognitionClient;
        this.MinConfidence = minConfidence;
        this.DynamoDB = dynamoDB;
    }

    /// <summary>
    /// A function for responding to S3 create events. It will determine if the object is an image and use Amazon Rekognition
    /// to detect labels and add the labels as tags on the S3 object.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(EventBridgeEvent input, ILambdaContext context)
    {
        var recordS3BucketName = input.detail.bucket.name;
        var recordS3ObjectKey = input.detail.@object.key;
        if (recordS3ObjectKey.StartsWith("uploads/"))
        {
            if (!SupportedImageTypes.Contains(Path.GetExtension(recordS3ObjectKey)))
            {
                Console.WriteLine($"Object {recordS3BucketName}:{recordS3ObjectKey} is not a supported image type");
                return;
            }

            Console.WriteLine($"Looking for labels in image {recordS3BucketName}:{recordS3ObjectKey}");
            var detectResponses = await this.RekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
            {
                MinConfidence = MinConfidence,
                Image = new Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = recordS3BucketName,
                        Name = recordS3ObjectKey
                    }
                }
            });

            Console.WriteLine($"Detected {detectResponses.Labels.Count} labels.");

            var tags = new List<Tag>();
            foreach (var label in detectResponses.Labels)
            {
                if (tags.Count < 10)
                {
                    Console.WriteLine($"\tFound Label {label.Name} with confidence {label.Confidence}");
                    tags.Add(new Tag { Key = label.Name, Value = label.Confidence.ToString() });
                }
                else
                {
                    Console.WriteLine($"\tSkipped label {label.Name} with confidence {label.Confidence} because the maximum number of tags has been reached");
                }
            }

            Console.WriteLine($"Saving {tags.Count} tags into S3.");

            await this.S3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = recordS3BucketName,
                Key = recordS3ObjectKey,
                Tagging = new Tagging
                {
                    TagSet = tags
                }
            });

            Console.WriteLine($"Saving metadata into the Dynamo DB.");

            // an image with all labels with confidence > 90
            var imageEntity = new ImageEntity
            {
                filename = recordS3ObjectKey,
                tags = detectResponses.Labels.Where(l => l.Confidence > 90).ToDictionary(l => l.Name, l => l.Confidence.ToString())
            };

            // save all metadata for the image
            var dbContext = new ImagesContext(this.DynamoDB);
            await dbContext.SaveAsync(imageEntity);
        }
    }
}
