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

        public static List<string> ContainTypeItems { get =>
                new()
                {
                    "equals", "contains", "starts with", "ends with"
                };
        }

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

        private static SettingsManager instance = new();
        public static SettingsManager Instance { get => instance; }
    }
}
