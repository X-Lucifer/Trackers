using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace Trackers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var log = LogManager.GetCurrentClassLogger();
            try
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception e)
            {
                log.Error(e);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(x =>
                {
                    x.UseStartup<Startup>();
                    var config = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", true, true)
                        .Build();
                    var urls = config["ApplicationUrl"] ?? "";
                    if (!string.IsNullOrEmpty(urls))
                    {
                        x.UseUrls(urls);
                    }
                });
    }
}
