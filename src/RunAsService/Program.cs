using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace RunAsService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "RunAsService";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<Applications>(hostContext.Configuration.GetSection("Applications"));
                    services.AddHostedService<Worker>();
                    services.AddTransient<WindowsApplicationRunner>();
                })
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddNLog("nlog.config");
                });
    }
}
