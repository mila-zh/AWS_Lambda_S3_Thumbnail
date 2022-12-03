using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLab4
{
    [DynamoDBTable("images")]
    public class ImageEntity
    {
        public string filename { get; set; }

        public Dictionary<string, string> tags { get; set; }
    }
}
