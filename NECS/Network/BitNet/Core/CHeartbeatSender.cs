using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NECS;
using NECS.Extensions;

namespace BitNet
{
    class CHeartbeatSender
    {
        CUserToken server;
        TimerCompat timer_heartbeat;
        uint interval;

        float elapsed_time;


        public CHeartbeatSender(CUserToken server, uint interval)
        {
            this.server = server;
            this.interval = interval;
            this.timer_heartbeat = new TimerCompat((int)(this.interval * 1000), this.on_timer, true);
        }


        void on_timer(object state, EventArgs args)
        {
            send();
        }


        void send()
        {
            CPacket msg = CPacket.create((short)CUserToken.SYS_UPDATE_HEARTBEAT);
            this.server.send(msg);
        }


        public void update(float time)
        {
            this.elapsed_time += time;
            if (this.elapsed_time < this.interval)
            {
                return;
            }

            this.elapsed_time = 0.0f;
            send();
        }


        public void stop()
        {
            this.elapsed_time = 0;
            this.timer_heartbeat.Stop();
        }


        public void play()
        {
            this.elapsed_time = 0;
            this.timer_heartbeat.Resume();
        }
    }
}
