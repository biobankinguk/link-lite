using LinkLite.Data;
using LinkLite.OptionsModels;
using LinkLite.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinkLite.HostedServices
{
    public class RquestPollingService : IHostedService, IDisposable
    {
        private readonly ILogger<RquestPollingService> _logger;
        private readonly RquestConnectorApiClient _rquestApi;
        private readonly OmopContext _db;
        private readonly RquestPollingServiceOptions _config;
        private Timer? _timer;

        public RquestPollingService(
            ILogger<RquestPollingService> logger,
            RquestConnectorApiClient rquestApi,
            IOptions<RquestPollingServiceOptions> config,
            OmopContext db)
        {
            _logger = logger;
            _rquestApi = rquestApi;
            _db = db;
            _config = config.Value;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RQUEST Polling Service started.");
            _timer = new Timer(PollRquest);
            StartTimer();
            return Task.CompletedTask;
        }

        private async void PollRquest(object? state)
        {
            // async void here is intentional to meet the TimerCallback signature
            // Stephen Cleary says it's ok:
            // https://stackoverflow.com/a/38918443


            // OMOP DB TESTING
            _logger.LogInformation($"People in DB: {_db.Person.Count()}");
            return;


            _logger.LogInformation(
                "Polling RQUEST for Queries on Collection: {_collectionId}",
                _config.RquestCollectionId);

            try
            {
                var task = await _rquestApi.FetchQuery(_config.RquestCollectionId);

                if (task is null)
                {
                    _logger.LogInformation(
                          "No Queries on Collection: {_collectionId}",
                          _config.RquestCollectionId);
                    return;
                }
            } catch (Exception e)
            {
                _logger.LogError(e, "Error fetching RQUEST query");
                return;
            }

            // TODO: Threading / Parallel query handling?
            // affects timer stoppage, the process logic will need to be
            // threaded using Task.Run or similar.

            // TODO: pause polling while processing (allow up to max parallellism)?
            StopTimer();

            _logger.LogInformation("Pretend I'm processing a query");

            // TODO: Process query - Query OMOP
            // await Process()

            // TODO: submit results via Rquest API
            // await _rquestApi.SubmitResults();

            StartTimer();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RQUEST Polling Service stopping.");
            StopTimer();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        /// <summary>
        /// Change the timer to execute its callback regularly
        /// at the configured polling interval
        /// </summary>
        private void StartTimer()
            => _timer?.Change(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_config.QueryPollingInterval));

        /// <summary>
        /// Change the timer to stop regular callback execution
        /// </summary>
        private void StopTimer()
            => _timer?.Change(Timeout.Infinite, 0);
    }
}
