using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class ChatterPickerList
    {
        private readonly List<ChatterPicker> pickers = new();

        public IReadOnlyList<ChatterPicker> ChatterPickers => pickers;

        public void UpdateIDs()
        {
            for (int i = 0; i < ChatterPickers.Count; ++i)
            {
                ChatterPickers[i].Index = i;
            }
        }

        internal ChatterPicker AddChatterPicker()
        {
            ChatterPicker picker = new();
            pickers.Add(picker);
            picker.Index = pickers.Count - 1;
            SettingsManager.Instance.EnsureChatterCount(pickers.Count);
            var settings = SettingsManager.Instance.GetChatterSettings(picker.Index);
            picker.ID = settings.ID;
            picker.VoiceSettings = settings.VoiceSettings;

            picker.MessageChanged += (_, args) =>
            {
                WebSocketServer.Instance.SendMessage(picker.ID, args.Message, args.Color, args.TokenizedMessage);
                picker.SpeechManager.Speak(picker.ID, args.Message);
            };

            picker.MessageDeleted += (_, args) =>
            {
                picker.SpeechManager.Stop(picker.ID);
            };

            picker.ChatterChanged += (_, args) =>
            {
                WebSocketServer.Instance.SendChatter(picker.ID, args.Chatter);
            };

            WebSocketServer.Instance.ConnectionOpen += (_, args) =>
            {
                if (picker.ChatterName != null)
                {
                    WebSocketServer.Instance.SendChatter(args.Connection, picker.ID, picker.ChatterName);
                    WebSocketServer.Instance.SendMessage(args.Connection, picker.ID, picker.LastMessage ?? "", "", picker.TokenizedLastMessage ?? "");
                }
                else
                {
                    WebSocketServer.Instance.SendChatter(args.Connection, picker.ID, "");
                    WebSocketServer.Instance.SendMessage(args.Connection, picker.ID, "", "", "");
                }
            };

            return picker;
        }

        public void Clear()
        {
            while (pickers.Count > 0)
            {
                RemovePicker(pickers.First(), false);
            }
        }

        public void ClearChatters()
        {
            foreach (var picker in pickers)
            {
                picker.ClearChatters();
            }
        }

        internal void RemovePicker(ChatterPicker picker, bool update = true)
        {
            SettingsManager.Instance.RemoveChatter(picker.Index);

            picker.Dispose();
            pickers.Remove(picker);

            if (update)
            {
                UpdateIDs();
            }
        }

        public void SetPickerCount(int count)
        {
            Clear();
            for (int i = pickers.Count; i < count; i++)
            {
                AddChatterPicker();
            }
        }

        private ChatterPickerList()
        {
        }

        private static readonly ChatterPickerList _instance = new();
        public static ChatterPickerList Instance => _instance;
    }
}
