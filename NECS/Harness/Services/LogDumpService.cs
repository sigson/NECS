
using NECS.Core.Logging;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NECS.ECS.ECSCore;

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class LogDumpService : IService
    {
        private static LogDumpService cacheInstance;
        public static LogDumpService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<LogDumpService>();
                return cacheInstance;
            }
        }

        DateTime lastLogDumpTime = DateTime.UtcNow;
        int salt = new Random().Next(0, 9999);
        
        
        public override void InitializeProcess()
        {
            var timer = new TimerCompat(5, (obj, arg) => LogDump(), true).Start();
        }

        public void LogDump()
        {
            if(!Defines.RedirectAllLogsToExeFile)
            {
                while (!NLogger.logsBag.IsEmpty)
                {
                    if (NLogger.logsBag.TryTake(out var log))
                    {
                        NLogger.PrintErrorBase(log.Item1, log.Item2, log.Item3);
                    }
                }
            }
            else
            {
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{lastLogDumpTime.ToString("yyyy-MM-dd-HH-mm-ss")}-{salt}.txt");
                using (var writer = new StreamWriter(logFilePath, true))
                {
                    while (!NLogger.logsBag.IsEmpty)
                    {
                        if (NLogger.logsBag.TryTake(out var log))
                        {
                            writer.WriteLine(log.Item3);
                        }
                    }
                }
            }
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }

        protected override Action<int>[] GetInitializationSteps()
        {
            return new Action<int>[]
            {
                (step) => {
                    InitializeProcess();
                },
                (step) => {
                    PostInitializeProcess();
                }
            };
        }

        protected override void SetupCallbacks(List<IService> allServices)
        {
            
        }
    }
}
