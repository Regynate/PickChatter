using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PickChatter
{
    public class AutoPicker : INotifyPropertyChanged
    {
        private ChatterPicker _picker;

        private readonly Timer _timer;
        private readonly Timer _notifyTimer;
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<EventArgs>? RemainingTimeChanged;

        private DateTime _endTime;

        public bool Running => _timer.Enabled;

        public string RemainingTimeString => Running ? TimeToString(_endTime - DateTime.Now) : TimeToString(TimeSpan.Zero);

        public string StartButtonText => Running ? "Stop auto-picking" : "Start auto-picking";

        public void Start()
        {
            _picker.PickRandomChatter();
            _timer.Start();
            _notifyTimer.Start();
            _endTime = DateTime.Now.AddSeconds(GetTimeSeconds());
            NotifyPropertyChanged(nameof(StartButtonText));
        }

        public void Stop()
        {
            _timer.Stop();
            _notifyTimer.Stop();
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

        internal AutoPicker(ChatterPicker picker)
        {
            _picker = picker;
            _timer = new Timer();
            _timer.Enabled = false;
            _timer.AutoReset = true;
            _timer.Interval = GetTime();
            _timer.Elapsed += (_, _) =>
            {
                _picker.PickRandomChatter();
                _endTime = DateTime.Now.AddSeconds(GetTimeSeconds());
            };
            SettingsManager.Instance.PropertySaved += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsManager.Instance.AutoPickingTime)
                || args.PropertyName == nameof(SettingsManager.Instance.AutoPickingTimeType))
                {
                    if (Running)
                    {
                        Stop();
                        _timer.Interval = GetTime();
                        Start();
                    }
                    else
                    {
                        _timer.Interval = GetTime();
                    }
                }

                if (args.PropertyName == nameof(SettingsManager.Instance.AutoPickingEnabled)
                && SettingsManager.Instance.AutoPickingEnabled == false)
                {
                    Stop();
                }
            };

            _notifyTimer = new Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = 100
            };

            _notifyTimer.Elapsed += (_, _) =>
            {
                NotifyRemainingTimeChanged();
            };
        }

        private void NotifyRemainingTimeChanged()
        {
            RemainingTimeChanged?.Invoke(this, EventArgs.Empty);
            NotifyPropertyChanged(nameof(RemainingTimeString));
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new(name));
        }
    }
}
