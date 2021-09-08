using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Quartz;
using Trackers.Models;

namespace Trackers
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Init(services);
            ConfigureQuartz(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
        }

        private void Init(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddLogging(x =>
            {
                x.ClearProviders();
                x.SetMinimumLevel(LogLevel.Trace);
                x.AddNLog(_config);
            });
            var redis = _config["RedisConfig"] ?? "";
            if (!string.IsNullOrEmpty(redis))
            {
                RedisHelper.Initialization(new CSRedisClient(redis));
                services.AddSingleton<IDistributedCache>(new CSRedisCache(RedisHelper.Instance));
            }
            else
            {
                services.AddDistributedMemoryCache(x => { x.ExpirationScanFrequency = TimeSpan.FromMilliseconds(10); });
            }
        }

        private void ConfigureQuartz(IServiceCollection services)
        {
            services.AddQuartz(x =>
            {
                var xkey = "bt-tracker-scheduler";
                x.SchedulerId = xkey;
                x.UseInMemoryStore();
                x.UseMicrosoftDependencyInjectionJobFactory();
                x.UseSimpleTypeLoader();
                x.UseDefaultThreadPool(w => { w.MaxConcurrency = 10; });
                var id = Guid.NewGuid().ToString("N");
                x.ScheduleJob<TrackerJob>(w =>
                    w.WithIdentity(id).StartNow().WithSimpleSchedule(z => z
                            .WithIntervalInHours(8)
                            .RepeatForever())
                        .WithDescription(xkey));
            });

            services.AddQuartzServer(x => { x.WaitForJobsToComplete = true; });

            services.AddQuartzHostedService(x => { x.WaitForJobsToComplete = true; });
        }
    }
}