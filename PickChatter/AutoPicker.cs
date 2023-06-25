using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PickChatter
{
    internal class AutoPicker : INotifyPropertyChanged
    {
        private Timer timer;
        private Timer notifyTimer;
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<EventArgs>? RemainingTimeChanged;

        private DateTime endTime;

        public bool Running => timer.Enabled;

        public string RemainingTimeString => Running ? TimeToString(endTime - DateTime.Now) : TimeToString(TimeSpan.Zero);

        public string StartButtonText => Running ? "Stop auto-picking" : "Start auto-picking";

        public void Start()
        {
            ChatterPicker.Instance.PickRandomChatter();
            timer.Start();
            notifyTimer.Start();
            endTime = DateTime.Now.AddSeconds(GetTimeSeconds());
            NotifyPropertyChanged(nameof(StartButtonText));
        }

        public void Stop()
        {
            timer.Stop();
            notifyTimer.Stop();
            NotifyPropertyChanged(nameof(StartButtonText));
            NotifyRemainingTimeChanged();
        }

        private string TimeToString(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
        }

        private int GetTime()
        {
            return GetTimeSeconds() * 1000;
        }

        private int GetTimeSeconds()
        {
            if (SettingsManager.Instance.AutoPickingTimeType == (int) SettingsManager.TimeType.Seconds)
            {
                return SettingsManager.Instance.AutoPickingTime;
            }
            else
            {
                return SettingsManager.Instance.AutoPickingTime * 60;
            }
        }

        private AutoPicker()
        {
            timer = new Timer();
            timer.Enabled = false;
            timer.AutoReset = true;
            timer.Interval = GetTime();
            timer.Elapsed += (_, _) =>
            {
                ChatterPicker.Instance.PickRandomChatter();
                endTime = DateTime.Now.AddSeconds(GetTimeSeconds());
            };
            SettingsManager.Instance.PropertySaved += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsManager.Instance.AutoPickingTime)
                || args.PropertyName == nameof(SettingsManager.Instance.AutoPickingTimeType))
                {
                    if (Running)
                    {
                        Stop();
                        timer.Interval = GetTime();
                        Start();
                    }
                    else
                    {
                        timer.Interval = GetTime();
                    }
                }

                if (args.PropertyName == nameof(SettingsManager.Instance.AutoPickingEnabled)
                && SettingsManager.Instance.AutoPickingEnabled == false)
                {
                    Stop();
                }
            };

            notifyTimer = new Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = 100
            };

            notifyTimer.Elapsed += (_, _) =>
            {
                NotifyRemainingTimeChanged();
            };
        }

        private void NotifyRemainingTimeChanged()
        {
            RemainingTimeChanged?.Invoke(this, EventArgs.Empty);
            NotifyPropertyChanged(nameof(RemainingTimeString));
        }

        private static AutoPicker instance = new();
        public static AutoPicker Instance => instance;

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new(name));
        }
    }
}
