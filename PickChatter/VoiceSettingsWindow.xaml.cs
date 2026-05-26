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
using System.Windows.Shapes;

namespace PickChatter
{
    /// <summary>
    /// Interaction logic for VoiceSettingsWindow.xaml
    /// </summary>
    public partial class VoiceSettingsWindow : Window
    {
        private SpeechManager _manager;
        private VoiceSettings _settings;
        private string _id;

        public VoiceSettingsWindow(SpeechManager manager, VoiceSettings settings, string id)
        {
            InitializeComponent();
            _manager = manager;
            _settings = settings;
            _id = id;

            DataContext = _settings;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TestVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            _manager.SetSettings(_settings);

            _manager.Speak("", "Hello, my name is " + _manager.CurrentVoice);
        }
    }
}
