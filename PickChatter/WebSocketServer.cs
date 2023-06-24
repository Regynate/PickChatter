using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PickChatter
{
    internal class WebSocketServer
    {
        private readonly List<IWebSocketConnection> connections = new List<IWebSocketConnection>();

        public event EventHandler<EventArgs>? ConnectionOpen;

        private readonly Fleck.WebSocketServer wss;

        private WebSocketServer()
        {
            wss = new Fleck.WebSocketServer($"ws://0.0.0.0:9871");
            // Set the websocket listeners
            wss.Start(connection =>
            {
                connection.OnOpen += () => OnWsConnectionOpen(connection);
                connection.OnClose += () => OnWSConnectionClose(connection);
            });
        }

        private void Broadcast(string message)
        {
            connections.ForEach(connection =>
            {
                // If the connection is not available for some reason, we just close it
                if (!connection.IsAvailable)
                {
                    connection.Close();
                }
                else
                {
                    connection.Send(message);
                }
            });
        }

        private void OnWSConnectionClose(IWebSocketConnection connection)
        {
            try
            {
                connections.Remove(connection);
            }
            catch (Exception)
            {

            }
        }

        private void OnWsConnectionOpen(IWebSocketConnection connection)
        {
            try
            {
                connections.Add(connection);
                ConnectionOpen?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {

            }
        }

        private void Broadcast(object message)
        {
            Broadcast(JsonConvert.SerializeObject(message));
        }

        public void SendMessage(string message, string color)
        {
            Broadcast(new { type = "message", message, color });
        }

        public void SendChatter(string chatter)
        {
            Broadcast(new { type = "chatter", chatter });
        }


        private static readonly WebSocketServer instance = new WebSocketServer();
        public static WebSocketServer Instance => instance;
    }
}
