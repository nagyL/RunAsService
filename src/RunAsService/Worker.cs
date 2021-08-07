using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RunAsService
{
    public class Worker : BackgroundService
    {
        private readonly IHostApplicationLifetime lifeTime;
        private readonly WindowsApplicationRunner applicationRunner;
        private readonly ILogger<Worker> logger;

        public Worker(IHostApplicationLifetime lifeTime, WindowsApplicationRunner applicationRunner, ILogger<Worker> logger)
        {
            this.lifeTime = lifeTime;
            this.applicationRunner = applicationRunner;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogDebug("ExecuteAsync...");

            // wait for the BackgroundService.StartAsync() task to complete...
            await Task.Delay(500, stoppingToken);

            try
            {
                await applicationRunner.RunAsync(stoppingToken);
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occured while running ApplicationRunner!");
                throw;
            }
            finally
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    lifeTime.StopApplication();
                }
            }
        }
    }
}
