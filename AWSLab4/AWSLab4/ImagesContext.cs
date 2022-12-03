using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLab4
{
    public class ImagesContext : DynamoDBContext
    {
        public ImagesContext(IAmazonDynamoDB client) : base(client) { }
    }
}
