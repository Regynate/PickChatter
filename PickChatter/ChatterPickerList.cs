using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class ChatterPickerList
    {
        private readonly List<ChatterPicker> pickers = new();

        public IReadOnlyList<ChatterPicker> ChatterPickers => pickers;

        internal ChatterPicker AddChatterPicker()
        {
            ChatterPicker picker = new();
            pickers.Add(picker);

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
                RemovePicker(pickers.First());
            }
        }

        public void ClearChatters()
        {
            foreach (var picker in pickers)
            {
                picker.ClearChatters();
            }
        }

        internal void RemovePicker(ChatterPicker picker)
        {
            WebSocketServer.Instance.SendMessage(picker.ID, "", "", "");
            WebSocketServer.Instance.SendChatter(picker.ID, "");
            WebSocketServer.Instance.SendRemainingTime(picker.ID, "0:00");
            pickers.Remove(picker);
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
