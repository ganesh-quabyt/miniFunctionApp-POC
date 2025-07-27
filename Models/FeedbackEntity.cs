using Azure;
using Azure.Data.Tables;
using System;

namespace FeedbackProcessor.Models
{
    public class FeedbackEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "feedback";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string FileName { get; set; }
        public string ContainerName { get; set; }
        public string Content { get; set; }

        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

}
