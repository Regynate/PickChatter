using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Client.Models;

namespace PickChatter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }
            TwitchClient.Instance.TryInitialize();

            ChatterPickerList.Instance.SetPickerCount(Math.Max(SettingsManager.Instance.ChattersSettings.Count, 1));
        }

        private static void Invoke(Action action)
        {
            Current.Dispatcher.Invoke(action);
        }

        private static T Invoke<T>(Func<T> action)
        {
            return Current.Dispatcher.Invoke(action);
        }

        public static void ShowMessage(string message)
        {
            Invoke(() => MessageBox.Show(Current.MainWindow, message));
        }

        public static void ShowMessage(string message, string title)
        {
            Invoke(() => MessageBox.Show(Current.MainWindow, message, title));
        }

        public static MessageBoxResult ShowMessage(string message, string title, MessageBoxButton button, MessageBoxImage image, MessageBoxResult result)
        {
            return Invoke(() => MessageBox.Show(Current.MainWindow, message, title, button, image, result));
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ChatterPickerList.Instance.Clear();
        }
    }
}
