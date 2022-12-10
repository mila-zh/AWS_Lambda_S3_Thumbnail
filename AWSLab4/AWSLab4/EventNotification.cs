using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLab4
{
    public class EventBridgeEvent
    {
        public EventBridgeEventDetail detail { get; set; }
    }

    public class EventBridgeEventDetail
    {
        public EventBridgeEventDetailBucket bucket { get; set; }
        public EventBridgeEventDetailObject @object { get; set; }
    }

    public class EventBridgeEventDetailBucket
    {
        public string name { get; set; }
    }

    public class EventBridgeEventDetailObject
    {
        public string key { get; set; }
    }
}

/*
{
  "version": "0",
  "id": "1db5f4ad-6a49-6ba4-700b-f77c49220534",
  "detail-type": "Object Created",
  "source": "aws.s3",
  "account": "069796323532",
  "time": "2022-12-08T23:43:55Z",
  "region": "us-east-1",
  "resources": [
    "arn:aws:s3:::lab4images"
  ],
  "detail": {
    "version": "0",
    "bucket": {
      "name": "lab4images"
    },
    "object": {
      "key": "uploads/cat.jpg",
      "size": 146880,
      "etag": "809eecfa2f2a9d901efb6d538df6957a",
      "sequencer": "00639276BB5414CFC4"
    },
    "request-id": "0V4RNXX51JF5N06E",
    "requester": "069796323532",
    "source-ip-address": "107.179.131.237",
    "reason": "PutObject"
  }
}
*/
