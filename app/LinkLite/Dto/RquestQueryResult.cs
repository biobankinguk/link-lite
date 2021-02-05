using System.Text.Json.Serialization;

namespace LinkLite.Dto
{
    public class RquestQueryTaskResult
    {
        public RquestQueryTaskResult(string taskId, int count)
        {
            TaskId = taskId;
            QueryResult = new() { Count = count };
        }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }

        [JsonPropertyName("query_result")]
        public RquestQueryResult QueryResult { get; set; } = new();
    }

    public class RquestQueryResult
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
