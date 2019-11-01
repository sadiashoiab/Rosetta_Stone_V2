﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rosetta.Services
{
    public class CacheRefreshService: IHostedService, IDisposable
    {
        private readonly ILogger<CacheRefreshService> _logger;
        private readonly IRosettaStoneService _rosettaStoneService;
        private Timer _occurrenceTimer;

        public CacheRefreshService(ILogger<CacheRefreshService> logger, IRosettaStoneService rosettaStoneService)
        {
            _logger = logger;
            _rosettaStoneService = rosettaStoneService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var absoluteExpirationInSeconds = await _rosettaStoneService.GetAbsoluteExpiration();
            var occurrenceInSeconds = absoluteExpirationInSeconds / 2;

            if (occurrenceInSeconds > 0)
            {
                _logger.LogInformation($"OccurrenceInSeconds is set to: {occurrenceInSeconds}, Creating CacheRefeshService timer to refresh the cache");
                
                _occurrenceTimer = new Timer(RefreshCache,
                    null,
                    TimeSpan.FromSeconds(occurrenceInSeconds),
                    TimeSpan.FromSeconds(occurrenceInSeconds));
            }
            else
            {
                _logger.LogError($"CacheRefeshService timer will NOT be created.  OccurrenceInSeconds is set to: {occurrenceInSeconds}");
            }
        }

        private async void RefreshCache(object state)
        {
            _logger.LogInformation("CacheRefeshService is refreshing the cache");
            await _rosettaStoneService.RefreshCache();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CacheRefeshService timer is stopping.");
            _occurrenceTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _occurrenceTimer?.Dispose();
        }
    }
}