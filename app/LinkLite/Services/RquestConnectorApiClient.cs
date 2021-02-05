using Flurl;

using LinkLite.Dto;
using LinkLite.OptionsModels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkLite.Services
{
    public class RquestConnectorApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<RquestConnectorApiClient> _logger;
        private readonly RquestConnectorApiOptions _apiOptions;

        public RquestConnectorApiClient(
            HttpClient client,
            ILogger<RquestConnectorApiClient> logger,
            IOptions<RquestConnectorApiOptions> apiOptions)
        {
            _client = client;
            _logger = logger;
            _apiOptions = apiOptions.Value;

            _client.BaseAddress = new Uri(Url.Combine(_apiOptions.BaseUrl, "/"));
        }

        /// <summary>
        /// Try and get a job for a biobank
        /// </summary>
        /// <param name="collectionId">RQUEST Collection Id (Biobank Id)</param>
        /// <returns></returns>
        public async Task<RquestQueryTask?> FetchQuery(string collectionId)
        {
            var result = await _client.PostAsJsonAsync(
                _apiOptions.FetchQueryEndpoint,
                new { collection_id = collectionId });

            if (result.IsSuccessStatusCode)
            {
                if (result.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogInformation(
                        "No Query Tasks waiting for {collectionId}",
                        collectionId);
                    return null;
                }

                try
                {
                    var task = await result.Content.ReadFromJsonAsync<RquestQueryTask>();

                    // TODO: a null task is impossible because the necessary JSON payload
                    // to achieve it would fail deserialization?
                    _logger.LogInformation($"Found Query Task with Id: {task!.TaskId}");
                    return task;
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Invalid Response Format from Fetch Query Endpoint");

                    // TODO: might make this conditional?
                    var body = await result.Content.ReadAsStringAsync();
                    _logger.LogDebug("Invalid Response Body: {body}", body);

                    throw;
                }
            }
            else
            {
                var message = $"Fetch Query Endpoint Request failed: {result.StatusCode}";
                _logger.LogError(message);
                throw new ApplicationException(message);
            }
        }

        /// <summary>
        /// Submit the result of a query
        /// </summary>
        /// <param name="taskId">ID of the query task</param>
        /// <param name="count">The result</param>
        public async Task SubmitQueryResult(string taskId, int count)
            => (await _client.PostAsJsonAsync(
                    _apiOptions.SubmitResultEndpoint,
                    new RquestQueryTaskResult(taskId, count)))
                .EnsureSuccessStatusCode();

        /// <summary>
        /// Cancel a query task
        /// </summary>
        /// <param name="taskId">ID of the query task</param>
        public async Task CancelQueryTask(string taskId)
            => (await _client.PostAsJsonAsync(
                    _apiOptions.SubmitResultEndpoint,
                    new RquestQueryTaskResult(taskId)))
                .EnsureSuccessStatusCode();
    }
}
