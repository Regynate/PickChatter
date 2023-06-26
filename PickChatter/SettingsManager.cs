using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class SettingsManager : INotifyPropertyChanged
    {
        public enum StringContainType
        {
            Equals,
            Contains,
            StartsWith,
            EndsWith
        }

        public enum TimeType
        {
            Seconds,
            Minutes
        }

        public static List<string> ContainTypeItems => new() { "equals", "contains", "starts with", "ends with" };

        public static List<string> AutoPickingTimeTypeItems => new() { "seconds", "minutes" };

        private readonly HashSet<string> propertiesChanged = new();

        public string TwitchUsername
        {
            get => GetProperty<string>(nameof(Settings.Default.TwitchUsername));
            set => SetProperty(nameof(Settings.Default.TwitchUsername), value);
        }

        public string TwitchOauth
        {
            get => GetProperty<string>(nameof(Settings.Default.TwitchOauth));
            set => SetProperty(nameof(Settings.Default.TwitchOauth), value);
        }

        public string TwitchChannel
        {
            get => GetProperty<string>(nameof(Settings.Default.TwitchChannel));
            set => SetProperty(nameof(Settings.Default.TwitchChannel), value);
        }

        public bool Rule1Enabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule1Enabled));
            set => SetProperty(nameof(Settings.Default.Rule1Enabled), value);
        }

        public bool Rule2Enabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule2Enabled));
            set => SetProperty(nameof(Settings.Default.Rule2Enabled), value);
        }

        public int Rule1MessageCount
        {
            get => GetProperty<int>(nameof(Settings.Default.Rule1MessageCount));
            set => SetProperty(nameof(Settings.Default.Rule1MessageCount), value);
        }

        public int Rule1TimeLimit
        {
            get => GetProperty<int>(nameof(Settings.Default.Rule1TimeLimit));
            set => SetProperty(nameof(Settings.Default.Rule1TimeLimit), value);
        }

        public int Rule2ContainType
        {
            get => GetProperty<int>(nameof(Settings.Default.Rule2ContainType));
            set => SetProperty(nameof(Settings.Default.Rule2ContainType), value);
        }

        public string Rule2String
        {
            get => GetProperty<string>(nameof(Settings.Default.Rule2String));
            set => SetProperty(nameof(Settings.Default.Rule2String), value);
        }

        public bool Rule2CaseSensitive
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule2CaseSensitive));
            set => SetProperty(nameof(Settings.Default.Rule2CaseSensitive), value);
        }

        public int Rule2TimeLimit
        {
            get => GetProperty<int>(nameof(Settings.Default.Rule2TimeLimit));
            set => SetProperty(nameof(Settings.Default.Rule2TimeLimit), value);
        }

        public bool SpeechSynthesisEnabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.SpeechSynthesisEnabled));
            set => SetProperty(nameof(Settings.Default.SpeechSynthesisEnabled), value);
        }

        public string MicrosoftVoice
        {
            get => GetProperty<string>(nameof(Settings.Default.MicrosoftVoice));
            set => SetProperty(nameof(Settings.Default.MicrosoftVoice), value);
        }

        public string AmazonVoice
        {
            get => GetProperty<string>(nameof(Settings.Default.AmazonVoice));
            set => SetProperty(nameof(Settings.Default.AmazonVoice), value);
        }
        
        public string GoogleVoice
        {
            get => GetProperty<string>(nameof(Settings.Default.GoogleVoice));
            set => SetProperty(nameof(Settings.Default.GoogleVoice), value);
        }

        public int SpeechSynthesisType
        {
            get => GetProperty<int>(nameof(Settings.Default.SpeechSynthesisType));
            set => SetProperty(nameof(Settings.Default.SpeechSynthesisType), value);
        }

        public bool Rule3Enabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule3Enabled));
            set => SetProperty(nameof(Settings.Default.Rule3Enabled), value);
        }

        public bool Rule3Subscriber
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule3Subscriber));
            set => SetProperty(nameof(Settings.Default.Rule3Subscriber), value);
        }

        public bool Rule3VIP
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule3VIP));
            set => SetProperty(nameof(Settings.Default.Rule3VIP), value);
        }

        public bool Rule3Moderator
        {
            get => GetProperty<bool>(nameof(Settings.Default.Rule3Moderator));
            set => SetProperty(nameof(Settings.Default.Rule3Moderator), value);
        }

        public int Rule3SubscriberTime
        {
            get => GetProperty<int>(nameof(Settings.Default.Rule3SubscriberTime));
            set => SetProperty(nameof(Settings.Default.Rule3SubscriberTime), value);
        }

        public bool AutoPickingEnabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.AutoPickingEnabled));
            set => SetProperty(nameof(Settings.Default.AutoPickingEnabled), value);
        }

        public int AutoPickingTime
        {
            get => GetProperty<int>(nameof(Settings.Default.AutoPickingTime));
            set => SetProperty(nameof(Settings.Default.AutoPickingTime), value);
        }

        public int AutoPickingTimeType
        {
            get => GetProperty<int>(nameof(Settings.Default.AutoPickingTimeType));
            set => SetProperty(nameof(Settings.Default.AutoPickingTimeType), value);
        }

        public bool ExcludeUsersEnabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.ExcludeUsersEnabled));
            set => SetProperty(nameof(Settings.Default.ExcludeUsersEnabled), value);
        }

        public string ExcludeUsersString
        {
            get => GetProperty<string>(nameof(Settings.Default.ExcludeUsersString));
            set => SetProperty(nameof(Settings.Default.ExcludeUsersString), value);
        }

        public bool ExcludeCommandsEnabled
        {
            get => GetProperty<bool>(nameof(Settings.Default.ExcludeCommandsEnabled));
            set => SetProperty(nameof(Settings.Default.ExcludeCommandsEnabled), value);
        }

        public bool HasModifiedProperties()
        {
            return propertiesChanged.Count > 0;
        }

        public bool PropertyModified(string name)
        {
            return propertiesChanged.Contains(name);
        }

        public void ResetProperties()
        {
            Settings.Default.Reload();

            foreach (string name in propertiesChanged)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            propertiesChanged.Clear();
        }

        public void Save()
        {
            Settings.Default.Save();

            foreach (string property in propertiesChanged)
            {
                PropertySaved?.Invoke(this, new(property));
            }

            propertiesChanged.Clear();
        }

        private SettingsManager()
        {
            
        }

        private T GetProperty<T>(string propertyName)
        {
            object obj = Settings.Default;
            return (T)obj.GetType().GetProperty(propertyName)!.GetValue(obj)!;
        }

        private void SetProperty(string propertyName, object value)
        {
            object obj = Settings.Default;

            var property = obj.GetType().GetProperty(propertyName);
            if (property?.GetValue(obj) != value)
            {
                propertiesChanged.Add(propertyName);
                property?.SetValue(obj, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangedEventHandler? PropertySaved;

        private static SettingsManager instance = new();
        public static SettingsManager Instance { get => instance; }
    }
}
