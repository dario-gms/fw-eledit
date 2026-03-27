using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowNavigationTimerService
    {
        public Timer CreateTimer(int intervalMs, Action onTick)
        {
            Timer timer = new Timer();
            timer.Interval = intervalMs;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (onTick != null)
                {
                    onTick();
                }
            };
            return timer;
        }
    }
}
