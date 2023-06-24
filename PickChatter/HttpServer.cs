using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class HttpServer
    {
        private static readonly HttpServer instance = new();
        public static HttpServer Instance { get => instance; }

        private readonly HttpListener listener;
        private readonly string url = "http://localhost:8876/";
        public string TwitchURL { get => url + "oauth/redirect"; }
        private bool running = false;

        public string? Token { get; private set;  }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string stateString;

        public string GenerateStateString()
        {
            return stateString = RandomHelper.String(32);
        }

        public string GetStateString()
        {
            return stateString;
        }

        private HttpServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            running = true;
            stateString = GenerateStateString();

            Task.Run(HandleIncomingConnections);
        }

        private async Task HandleIncomingConnections()
        {
            while (running)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;

                byte[] data = Array.Empty<byte>();

                if (req?.Url?.AbsolutePath == "/oauth/redirect")
                {
                    data = File.ReadAllBytes(".\\redirect.html");
                }
                else if (req?.QueryString.Get("state") == stateString)
                { 
                    if (req?.Url?.AbsolutePath == "/error")
                    {
                        data = File.ReadAllBytes(".\\error.html");
                    }
                    else if (req?.Url?.AbsolutePath == "/oauth")
                    {
                        data = File.ReadAllBytes(".\\success.html");
                        Token = req.QueryString.Get("access_token");
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Token)));
                    }
                }

                await res.OutputStream.WriteAsync(data);
                res.Close();
            }
        }

        public void Stop()
        {
            running = false;
            listener.Stop();
        }
    }
}
