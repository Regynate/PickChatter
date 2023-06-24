using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PickChatter;
using TwitchLib.Communication.Interfaces;

namespace PickChatter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void SayLastMessage()
        {
            var message = ChatterPicker.Instance.LastMessage;
            if (!string.IsNullOrWhiteSpace(message))
            {
                Dispatcher.Invoke(() => SpeechManager.Instance.Speak(message));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectSpecificChatterButton_Click(object sender, RoutedEventArgs e)
        {
            string chatter = PickSpecificTextBox.Text;
            ChatterPicker.Instance.PickChatter(chatter);
        }

        private void PickRandomChatterButton_Click(object sender, RoutedEventArgs e)
        {
            ChatterPicker.Instance.PickRandomChatter();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new()
            {
                Owner = this,
                ShowInTaskbar = false
            };

            window.Show();
        }

        private void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpeechManager.Instance.SpeechSpeaking)
            {
                SpeechManager.Instance.Stop();
            }
            else
            {
                SayLastMessage();
            }
        }
    }
}
