using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NECS.Core.Logging;
using NECS.Extensions.ThreadingSync;

namespace NECS.Extensions
{
    public static partial class DateTimeExtensions
    {
        private static long ServerTime;
        private static long LocalTime;

        public static long TicksToMilliseconds(long ticks) => ticks / 10000;
        public static long MillisecondToTicks(long ms) => ms * 10000;
        public static float TicksToSeconds(long ticks) => (float)Math.Round(Math.Round((double)ticks / 10000) / 1000, 3);
        public static long NowServerTicks => DateTime.Now.Ticks + (ServerTime-LocalTime);

        public static void UpdateServerTime(long ServerTicks)
        {
            ServerTime = ServerTicks;
            LocalTime = DateTime.Now.Ticks;
        }
        private static int DateValue(this DateTime dt)
        {
            return dt.Year * 372 + (dt.Month - 1) * 31 + dt.Day - 1;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2)
        {
            return dt.MonthsBetween(dt2) / 12;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            return dt.MonthsBetween(dt2, includeLastDay) / 12;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2, bool includeLastDay, out int excessMonths)
        {
            int months = dt.MonthsBetween(dt2, includeLastDay);
            excessMonths = months % 12;
            return months / 12;
        }

        public static int MonthsBetween(this DateTime dt, DateTime dt2)
        {
            int months = (dt2.DateValue() - dt.DateValue()) / 31;
            return Math.Abs(months);
        }

        public static int MonthsBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            if (!includeLastDay) return dt.MonthsBetween(dt2);
            int days;
            if (dt2 >= dt)
                days = dt2.AddDays(1).DateValue() - dt.DateValue();
            else
                days = dt.AddDays(1).DateValue() - dt2.DateValue();
            return days / 31;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2)
        {
            return dt.DaysBetween(dt2) / 7;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            return dt.DaysBetween(dt2, includeLastDay) / 7;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2, bool includeLastDay, out int excessDays)
        {
            int days = dt.DaysBetween(dt2, includeLastDay);
            excessDays = days % 7;
            return days / 7;
        }

        public static int DaysBetween(this DateTime dt, DateTime dt2)
        {
            return (dt2.Date - dt.Date).Duration().Days;
        }

        public static int DaysBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            int days = dt.DaysBetween(dt2);
            if (!includeLastDay) return days;
            return days + 1;
        }
    }
    public class TimerEx : TimerCompat
    {
        private long TimerStart = 0;
        private long TimerPaused = 0;
        private long TimerStopped = 0;
        private double baseInterval = 0f;
        private double interval = 0f;
		public bool inited = false;
        public bool Dead = false;
        public new double Interval
        {
            get
            {
                return interval;
            }
            set
            {
                if(value > 0)
                {
                    baseInterval = value;
                    interval = value;
                    base.Interval = value;
                }
            }
        }

        public bool AutoReset { get => this.timerData.AutoReset; set => this.timerData.AutoReset = value; }
        public EventHandler Elapsed { get => this.timerData.Elapsed;  set => this.timerData.Elapsed = value; }

        public TimerEx() : base()
        {
            this.timerData.Elapsed += (async (sender, e) => { 
                if(base.timerData.AutoReset)
                    this.Interval = this.baseInterval; 
                TimerStart = TimerDateTime.DateTimeNowTicks; TimerPaused = 0;});
            //base.AutoReset = true;
            this.timerData.Disposed += new EventHandler(this.OnDisposeTimer);
        }

        public TimerEx(TimerCompat.TimerInstance timerInstance) : base(timerInstance)
        {
            this.timerData.Elapsed += (async (sender, e) => { 
                if(base.timerData.AutoReset)
                    this.Interval = this.baseInterval; 
                TimerStart = TimerDateTime.DateTimeNowTicks; TimerPaused = 0;});
            this.timerData.Disposed += new EventHandler(this.OnDisposeTimer);
        }

        public TimerEx(TimerEx oldTimer) : base()
        {
            this.timerData.Elapsed += (async (sender, e) => {
                if (!base.timerData.AutoReset)
                    this.Interval = this.baseInterval; 
                TimerStart = TimerDateTime.DateTimeNowTicks; TimerPaused = 0; });
            Interval = oldTimer.Interval;
			inited = true;
            //base.AutoReset = true;
            this.timerData.Disposed += new EventHandler(this.OnDisposeTimer);
        }

        public void OnDisposeTimer(object sender, EventArgs args)
        {
            Dead = true;
        }

        public new void Start()
        {
            TimerStart = TimerDateTime.DateTimeNowTicks;
			inited = true;
            base.Start();
        }

        public new void Stop()
        {
            TimerStopped = TimerDateTime.DateTimeNowTicks;
            base.Stop();
        }

        public new void Pause()
        {
            if(this.Enabled)
            {
                this.Stop();
                TimerPaused = TimerDateTime.DateTimeNowTicks;
            }
        }

        public new void Resume()
        {
            if(TimerPaused != 0 && !this.Enabled)
            {
                this.interval = TimerPaused - TimerStart;
                base.Interval = TimerPaused - TimerStart;
                TimerPaused = 0;
                TimerStart = TimerDateTime.DateTimeNowTicks;
                this.Start();
            }
        }

        public void Reset()
        {
            this.Interval = baseInterval;
            this.Stop();
            TimerStart = 0;
            TimerPaused = 0;
            TimerStopped = 0;
            this.Start();
        }

        public double RemainingToElapsedTime()
        {
            return baseInterval - TimeSpan.FromTicks((TimerPaused == 0 ? TimerDateTime.DateTimeNowTicks - TimerStart : TimerPaused - TimerStart)).TotalMilliseconds;
        }
    }

    public class TimerCompat : IDisposable
    {
        public static Func<(Action, Action)> TimerThreadOverridable = () => { NLogger.LogError("TimerThreadOverridable not set"); return (null, null); };
        public static int baseTick => Defines.TimerMinimumMSTick;
        public double Interval
        {
            get{
                return timerData.MSInterval;
            }
            set{
                timerData.MSInterval = Convert.ToInt64(value);
            }
        }
        
        public static void InitTimerInfrastructure() { new TimerCompat().Dispose(); }

        public class TimerInstance
        {
            public bool IsEnabled { get; set; }
            public long MSInterval { get => Convert.ToInt64((double)TicksInterval * 0.0001f); set => TicksInterval = value * 10000; }
            public long RemainingMS { get => Convert.ToInt64((double)RemainingTicks * 0.0001f); set => RemainingTicks = value * 10000; }
            public long TicksInterval;
            public long RemainingTicks;

            public EventHandler Elapsed { get; set; } = (sender, e) => { };
            public EventHandler Disposed { get; set; } = (sender, e) => { };
            public bool AutoReset { get; set; }
            public bool IsPaused { get; set; }
            public bool IsAsync { get; set; } = true;
        }

        public class TimerDateTime
        {
            public static long DateTimeNowTicks
            {
                get
                {
                    return Ticks;
                }
                set => Ticks = value;
            }
            private static long Ticks;

            public static void UpdateTicks()
            {
                Ticks += DateTimeExtensions.MillisecondToTicks(Defines.TimerMinimumMSTick);
            }
        }

        private class GlobalTimerManager : IDisposable
        {
            public static readonly GlobalTimerManager Instance = new GlobalTimerManager();
            private readonly Thread _timerThread;
            private volatile bool _isRunning;
            private readonly HashSet<TimerInstance> _timers = new HashSet<TimerInstance>();
            private long _currentTicks;

            private GlobalTimerManager()
            {
                if (Defines.OneThreadMode)
                {
                    TimerCompat.TimerThreadOverridable = GetTimerLoop;
                }
                else
                {
                    _timerThread = new Thread(TimerLoop)
                    {
                        IsBackground = true,
                        Name = "GlobalTimerThread",
                        Priority = ThreadPriority.AboveNormal
                    };
                    _timerThread.Start();
                }
            }

            private void TimerLoop()
            {
                var loop = GetTimerLoop();
                while (_isRunning)
                {
                    loop.Item1();
                    Thread.Sleep(baseTick);
                    loop.Item2();
                }
            }

            private (Action, Action) GetTimerLoop()
            {
                _isRunning = true;
                TimerDateTime.DateTimeNowTicks = DateTime.Now.Ticks;
                long offset = 0;
                long externalTimeCache = TimerDateTime.DateTimeNowTicks;
                Stopwatch externalStopwatch = new Stopwatch();
                Stopwatch internalStopwatch = new Stopwatch();
                Action tickAction = () =>
                {
                    internalStopwatch.Start();
                    try
                    {
                        lock (_timers)
                        {
                            //var timersToProcess = _timers.ToList();
                            foreach (var timer in _timers)
                            {
                                if (timer.IsEnabled && !timer.IsPaused)
                                {
                                    timer.RemainingTicks -= TimerDateTime.DateTimeNowTicks - externalTimeCache + offset;
                                    if (timer.RemainingTicks <= 0)
                                    {
                                        try
                                        {
                                            if (timer.IsAsync)
                                                TaskEx.RunAsync(() => timer.Elapsed?.Invoke(timer, EventArgs.Empty));
                                            else
                                                timer.Elapsed?.Invoke(timer, EventArgs.Empty);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Timer callback error: {ex.Message}");
                                        }

                                        if (timer.AutoReset)
                                        {
                                            timer.RemainingMS = timer.MSInterval;
                                        }
                                        else
                                        {
                                            timer.IsEnabled = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        internalStopwatch.Stop();
                        if (internalStopwatch.ElapsedMilliseconds < 100)//NOT DEBUGGED
                            TimerDateTime.DateTimeNowTicks += (internalStopwatch.ElapsedTicks / 100);
                        internalStopwatch.Reset();
                        externalTimeCache = TimerDateTime.DateTimeNowTicks;
                        externalStopwatch.Start();
                    }
                };
                Action fixTick = () =>
                {
                    externalStopwatch.Stop();
                    if (externalStopwatch.ElapsedMilliseconds < baseTick + 100)//NOT DEBUGGED
                    {
                        TimerDateTime.DateTimeNowTicks += (externalStopwatch.ElapsedTicks / 100);
                    }
                    else
                        TimerDateTime.UpdateTicks();
                    externalStopwatch.Reset();
                };
                return (tickAction, fixTick);
            }

            internal void RegisterTimer(TimerInstance timer)
            {
                lock (_timers)
                {
                    _timers.Add(timer);
                }
            }

            internal void UnregisterTimer(TimerInstance timer)
            {
                lock (_timers)
                {
                    _timers.Remove(timer);
                }
            }

            public void Dispose()
            {
                _isRunning = false;
                _timerThread?.Join();
            }
        }

        public readonly TimerInstance timerData = new TimerInstance();
        private static GlobalTimerManager Manager => GlobalTimerManager.Instance;

        public bool Enabled => timerData.IsEnabled;
        public bool IsPaused => timerData.IsPaused;

        bool registered = false;

        public TimerCompat()
        {
            var initTimer = Manager.ToString();
        }

        public TimerCompat(TimerInstance timerInstance)
        {
            timerData = timerInstance;
            Manager.RegisterTimer(timerData);
            registered = true;
        }

        public TimerCompat(int intervalMs, EventHandler callback, bool loop = false, bool asyncRun = true)
        {
            if (intervalMs < 2) intervalMs = 2;
            timerData = new TimerInstance
            {
                IsEnabled = false,
                MSInterval = intervalMs,
                RemainingMS = intervalMs,
                Elapsed = callback,
                AutoReset = loop,
                IsPaused = false,
                IsAsync = asyncRun
            };
            Manager.RegisterTimer(timerData);
            registered = true;
        }

        public void Start()
        {
            if(!registered)
            {
                Manager.RegisterTimer(timerData);
                registered = true;
            }
            timerData.IsEnabled = true;
            timerData.IsPaused = false;
            if (timerData.RemainingMS <= 0)
            {
                timerData.RemainingMS = timerData.MSInterval;
            }
        }

        public void Pause()
        {
            if(!registered)
            {
                Manager.RegisterTimer(timerData);
                registered = true;
            }
            timerData.IsPaused = true;
        }

        public void Resume()
        {
            if(!registered)
            {
                Manager.RegisterTimer(timerData);
                registered = true;
            }
            if (timerData.RemainingMS > 0)
            {
                timerData.IsPaused = false;
                timerData.IsEnabled = true;
            }
        }

        public void Stop()
        {
            if(!registered)
            {
                Manager.RegisterTimer(timerData);
                registered = true;
            }
            timerData.IsEnabled = false;
            timerData.IsPaused = false;
            timerData.RemainingMS = timerData.MSInterval;
        }

        public TimeSpan GetRemainingTime()
        {
            return TimeSpan.FromMilliseconds(timerData.RemainingMS * 5);
        }

        public void Dispose()
        {
            if(registered)
            {
                Manager.UnregisterTimer(timerData);
            }
            timerData.Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}