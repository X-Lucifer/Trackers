using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Trackers.Models
{
    [DisallowConcurrentExecution]
    public class TrackerJob : IJob
    {
        private readonly IConfiguration _config;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TrackerJob> _log;

        public TrackerJob(IConfiguration config, IDistributedCache cache, ILogger<TrackerJob> log)
        {
            _config = config;
            _cache = cache;
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var origins = new List<TrackerInfo>();
                _config.Bind("TrackerOrigins", origins);
                if (origins.Count <= 0)
                {
                    return;
                }

                var tasks = new Task[origins.Count];
                var dic = new ConcurrentDictionary<string, string>();
                for (int i = 0; i < tasks.Length; i++)
                {
                    var xi = i;
                    tasks[i] = Task.Run(async () =>
                    {
                        var result = await origins[xi].Url.GetStringAsync();
                        if (!string.IsNullOrEmpty(result))
                        {
                            _log.LogInformation(
                                $"BT Tracker: 【{origins[xi].Name}】 download has been completed , {DateTime.Now:G}");
                            if (!dic.ContainsKey(origins[xi].Name))
                            {
                                var txt = Regex.Replace(result, "\\s+", "|");
                                dic.TryAdd(origins[xi].Name, txt);
                            }
                        }
                    });
                }

                await Task.WhenAll(tasks);
                _log.LogInformation($"download all finished...");
                var trackers = new List<string>();
                StringBuilder sb = new StringBuilder();
                foreach (var item in dic)
                {
                    var txts = item.Value.Split('|');
                    foreach (var xitem in txts)
                    {
                        if (trackers.Contains(xitem) || string.IsNullOrEmpty(xitem))
                        {
                            continue;
                        }

                        trackers.Add(xitem);
                        sb.AppendLine(xitem);
                        sb.AppendLine();
                    }
                }

                var manualfile = _config["ManualTrackerFile"] ?? "";
                if (!string.IsNullOrEmpty(manualfile) && File.Exists(manualfile))
                {
                    var manual = await File.ReadAllTextAsync(manualfile, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(manual))
                    {
                        var xmanual = Regex.Replace(manual, "\\s+", "|");
                        var manuallist = xmanual.Split('|');
                        foreach (var item in manuallist)
                        {
                            if (trackers.Contains(item) || string.IsNullOrEmpty(item))
                            {
                                continue;
                            }

                            trackers.Add(item);
                            sb.AppendLine(item);
                            sb.AppendLine();
                        }
                    }
                }

                var xresult = sb.ToString();
                await _cache.SetStringAsync("best_trackers", xresult);
            }
            catch (Exception e)
            {
                _log.LogError(e.ToString());
            }
        }
    }
}