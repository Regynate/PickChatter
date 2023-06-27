using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
            ChatterPicker.Instance.MessageChanged += (_, args) =>
            {
                Invoke(() => WebSocketServer.Instance.SendMessage(args.Message, args.Color));
                Invoke(() => SpeechManager.Instance.Speak(args.Message));
            };

            ChatterPicker.Instance.ChatterChanged += (_, args) =>
            {
                Invoke(() => WebSocketServer.Instance.SendChatter(args.Chatter));
            };

            WebSocketServer.Instance.ConnectionOpen += (_, args) =>
            {
                if (ChatterPicker.Instance.ChatterName != null)
                {
                    Invoke(() => WebSocketServer.Instance.SendChatter(args.Connection, ChatterPicker.Instance.ChatterName));
                    Invoke(() => WebSocketServer.Instance.SendMessage(args.Connection, ChatterPicker.Instance.LastMessage ?? "", ""));
                }
                else
                {
                    Invoke(() => WebSocketServer.Instance.SendChatter(args.Connection, ""));
                    Invoke(() => WebSocketServer.Instance.SendMessage(args.Connection, "", ""));
                }
            };

            AutoPicker.Instance.RemainingTimeChanged += (_, _) =>
            {
                Invoke(() => WebSocketServer.Instance.SendRemainingTime(AutoPicker.Instance.RemainingTimeString));
            };
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
            WebSocketServer.Instance.SendMessage("", "");
            WebSocketServer.Instance.SendChatter("");
            WebSocketServer.Instance.SendRemainingTime("0:00");
        }
    }
}
