using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using NECS.Extensions.ThreadingSync;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(10)]
    public class TimerComponent : ECSComponent
    {
        static public new long Id { get; set; } = 10;
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public TimerEx componentTimer = new TimerEx();
        public double timerAwait = 0;

		public double timeRemaining;
        public double TimeRemaining { 
            get {
                if (this.componentTimer.inited)
                    return this.componentTimer.RemainingToElapsedTime();
                else
                    return timeRemaining;
            }
            set
            {
                if(!componentTimer.inited)
                {
                    timeRemaining = value;
                }
            }
        }
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public Action<ECSEntity, ECSComponent> onStart;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public Action<ECSEntity, ECSComponent> onEnd;

        public virtual ECSComponent TimerStart(double newUpdatedTime, ECSEntity entity, bool inSeconds = false, bool loop = false)
        {
            if(componentTimer.Dead)
            {
                componentTimer = new TimerEx(componentTimer);
            }
            if (newUpdatedTime == 0)
            {
                if(timerAwait != 0)
                {
                    componentTimer.Interval = (inSeconds ? timerAwait*1000 : timerAwait);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                timerAwait = (inSeconds ? newUpdatedTime * 1000 : newUpdatedTime);
                componentTimer.Interval = (inSeconds ? newUpdatedTime * 1000 : newUpdatedTime);
            }
            if(entity != null)
            {
                ownerEntity = entity;
            }
            if(onStart != null)
            {
                onStart(ownerEntity, this);
            }
            componentTimer.Elapsed += async (sender, e) => TaskEx.RunAsync(() => TimerEnd());
            componentTimer.AutoReset = loop;
            componentTimer.Start();
            return this;
        }

        public virtual void TimerEnd()
        {
            if(!componentTimer.AutoReset)
            {
                componentTimer.Stop();
            }
                
            if (ownerEntity != null && onEnd != null)
            {
                onEnd(ownerEntity, this);
            }
        }

        public virtual void TimerStop()
        {
            componentTimer.Stop();
        }

        public virtual void TimerReset()
        {
            componentTimer.Reset();
        }

        public virtual void TimerPause()
        {
            componentTimer.Pause();
        }

        public virtual void TimerResume()
        {
            componentTimer.Resume();
        }

        protected override void EnterToSerializationImpl()
        {
            timeRemaining = TimeRemaining;
        }
    }
}
