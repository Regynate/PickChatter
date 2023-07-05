using System;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;

namespace PickChatter
{
    internal class Chatter
    {
        private class Message
        {
            public Message(string content, DateTime timestamp, string id)
            {
                PlainContent = content;
                Timestamp = timestamp;
                ID = id;
            }

            public string PlainContent { get; }
            public DateTime Timestamp { get; }
            public string ID { get; }
        }

        public Chatter(string username, string displayname)
        {
            Username = username;
            DisplayName = displayname;
        }

        public Chatter(string username, ChatMessage message) : this(username, message.DisplayName)
        {
            Update(message);
        }

        private readonly List<Message> messages = new();

        public string Username { get; }
        public string DisplayName { get; private set; }
        private string? color;
        public string Color => color ?? "#ff7f50";
        public bool IsSubscriber { get; private set; }
        public int SubscriberTime { get; private set; }
        public bool IsModerator { get; private set; }
        public bool IsVIP { get; private set; }
        public bool HasMessage => messages.Count > 0;
        public DateTime Timestamp => HasMessage ? messages.Last().Timestamp : DateTime.MinValue;
        public string LastMessage => HasMessage ? messages.Last().PlainContent : "";
        private EmoteSet? emoteSet;
        public string? TokenizedLastMessage => HasMessage ? TwitchClient.Instance.ConvertToEmoteJson(LastMessage, emoteSet!) : "";
        public bool ChosenBefore { get; private set; }

        public void Update(ChatMessage message)
        {
            messages.Add(new Message(message.Message, DateTime.Now, message.Id));
            DisplayName = message.DisplayName;
            color = message.ColorHex;
            IsSubscriber = message.IsSubscriber;
            IsModerator = message.IsModerator;
            IsVIP = message.IsVip;
            SubscriberTime = message.SubscribedMonthCount;
            emoteSet = message.EmoteSet;
        }

        public int MessageCount()
        {
            return messages.Count;
        }

        public int MessageCount(TimeSpan timeLimit)
        {
            var now = DateTime.Now;
            return messages.Count - messages.FindLastIndex(m => now - m.Timestamp > timeLimit) - 1;
        }

        public bool ContainsMessage(TimeSpan timeLimit, Predicate<string> selector)
        {
            int count = MessageCount(timeLimit);
            if (count == 0)
            {
                return false;
            }
            return messages.FindLastIndex(messages.Count - 1, count, m => selector(m.PlainContent)) != -1;
        }

        public bool RemoveMessage(string id)
        {
            foreach (var message in messages.ToList())
            {
                if (message.ID == id)
                {
                    messages.Remove(message);
                    return true;
                }
            }
            return false;
        }

        public void Choose()
        {
            ChosenBefore = true;
        }
    }
}
