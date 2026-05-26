using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace PickChatter
{
    /// <summary>
    /// Interaction logic for ChatterPage.xaml
    /// </summary>
    public partial class ChatterPage : Page
    {
        private readonly ChatterPicker _picker;
        private readonly AutoPicker _autoPicker;

        public ChatterPicker ChatterPicker => _picker;
        public AutoPicker AutoPicker => _autoPicker;

        public ChatterPage(ChatterPicker picker)
        {
            InitializeComponent();
            _picker = picker;
            _autoPicker = new AutoPicker(_picker);
            _autoPicker.RemainingTimeChanged += (_, _) =>
            {
                WebSocketServer.Instance.SendRemainingTime(picker.ID, _autoPicker.RemainingTimeString);
            };

            DataContext = this;
        }

        private void SayLastMessage()
        {
            var message = _picker.LastMessage;
            if (!string.IsNullOrWhiteSpace(message))
            {
                Dispatcher.Invoke(() => _picker.SpeechManager.Speak(_picker.ID, message));
            }
        }

        private void SelectSpecificChatterButton_Click(object sender, RoutedEventArgs e)
        {
            string chatter = PickSpecificTextBox.Text;
            _picker.PickChatter(chatter);
        }

        private void PickRandomChatterButton_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => _picker.SpeechManager.Stop(_picker.ID));
            if (!_picker.PickRandomChatter())
            {
                App.ShowMessage("There are no chatters to select from");
            }
        }

        private void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            if (_picker.SpeechManager.SpeechSpeaking)
            {
                _picker.SpeechManager.Stop(_picker.ID);
            }
            else
            {
                SayLastMessage();
            }
        }

        private void AutoPickButton_Click(object sender, RoutedEventArgs e)
        {
            if (_autoPicker.Running)
            {
                _autoPicker.Stop();
                _picker.PickChatter(null);
            }
            else
            {
                _autoPicker.Start();
            }
        }

        private void ChatterIDTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _picker.ID = ChatterIDTextBox.Text;
        }

        private void VoiceSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            VoiceSettingsWindow window = new(_picker.SpeechManager, _picker.VoiceSettings, _picker.ID)
            {
                Owner = Window.GetWindow(this),
                ShowInTaskbar = false
            };

            window.ShowDialog();

            _picker.UpdateSpeechSettings();
            SettingsManager.Instance.SetChatterVoiceSettings(_picker.ID, _picker.VoiceSettings);
        }
    }
}
