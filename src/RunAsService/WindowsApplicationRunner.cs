using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RunAsService
{
    public class WindowsApplicationRunner
    {
        private readonly IOptions<Applications> options;
        private readonly ILogger<WindowsApplicationRunner> logger;

        public WindowsApplicationRunner(IOptions<Applications> options, ILogger<WindowsApplicationRunner> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            var runnerTasks = new List<Task>();
            foreach(var processDetails in options.Value.Items ?? Enumerable.Empty<ApplicationInfo>())
            {
                ValidateOptions(processDetails);

                runnerTasks.Add(
                    Task.Run(() => RunProcessGuarded(processDetails, stoppingToken)));
            }

            return Task.WhenAll(runnerTasks);
        }

        private void ValidateOptions(ApplicationInfo processDetails)
        {
            if (string.IsNullOrEmpty(processDetails.FileName))
            {
                throw new ApplicationException($"Mandatory argument '{nameof(processDetails.FileName)}' is missing!");
            }
        }

        private void RunProcessGuarded(ApplicationInfo processDetails, CancellationToken stoppingToken)
        {
            string processFileName = "NA";
            try
            {
                processFileName = Path.GetFileNameWithoutExtension(processDetails.FileName!);
                var state = new[]{new KeyValuePair<string, object>("FileName", processFileName!)};

                using (logger.BeginScope(state))
                {
                    do
                    {
                        RunProcess(processDetails, stoppingToken);
                    }
                    while(!stoppingToken.IsCancellationRequested && processDetails.RestartIfExited);
                }
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"An unhandled exception occured while running thie given process ({processFileName})!");
                throw;
            }
        }

        private void RunProcess(ApplicationInfo processDetails, CancellationToken stoppingToken)
        {
            logger.LogDebug($"Starting process {processDetails.FileName}...");

            int processExitCode = 0;
            using (var process = new Process())
            {
                process.StartInfo = 
                    new ProcessStartInfo
                    {
                        FileName = processDetails.FileName!,
                        Arguments = processDetails.Arguments ?? "",
                        WorkingDirectory = processDetails.WorkDir ?? "",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                process.OutputDataReceived += (sender, args) => logger.LogDebug($"stdOut: {args.Data}");
                process.ErrorDataReceived += (sender, args) => logger.LogDebug($"errOut: {args.Data}");
                process.Exited += (sender, args) => processExitCode = process.ExitCode;

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occured while starting the given process!");
                }

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                logger.LogInformation($"Process (pid={process.Id}) started.");

                do
                {
                    stoppingToken.WaitHandle.WaitOne(1000);

                    if (!process.HasExited && stoppingToken.IsCancellationRequested)
                    {
                        logger.LogTrace("Cancellation requested. Wait for the application to close gracefully...");
                        process.CloseMainWindow();

                        if (!process.WaitForExit(2000))
                        {
                            logger.LogTrace("Application did not close in a given period. The process will be killed!");
                            process.Kill();
                        }
                    }
                }
                while(!process.HasExited);
            }

            if (processExitCode == 0)
            {
                logger.LogInformation("Process terminated succesfully...");
            }
            else
            {
                logger.LogError($"Process terminated with errors (exitcode: {processExitCode})");
            }
        }
    }
}