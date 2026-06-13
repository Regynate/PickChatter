using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

        public string? Token { get; private set; }

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
                var path = req.Url?.AbsolutePath;

                if (path == "/oauth/redirect")
                {
                    data = File.ReadAllBytes(".\\redirect.html");
                }
                else if (req?.QueryString.Get("state") == stateString)
                {
                    if (path == "/error")
                    {
                        data = File.ReadAllBytes(".\\error.html");
                    }
                    else if (path == "/oauth")
                    {
                        data = File.ReadAllBytes(".\\success.html");
                        Token = req.QueryString.Get("access_token");
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Token)));
                    }
                }
                else if (path == "/11labs")
                {
                    if (ElevenLabsAPI.GetAudio(req.QueryString?.Get("message") ?? "", req.QueryString?.Get("voice") ?? "", out data))
                    {
                        res.ContentType = "audio/mp3";
                    }
                }
                else if (path?.StartsWith("/overlay") ?? false)
                {
                    if (path == "/overlay" || path == "/overlay/")
                    {
                        path = path + "/index.html";
                    }
                    try
                    {
                        data = File.ReadAllBytes("..//" + path);
                        var contentType = new Dictionary<string, string> {
                            { ".ico", "image/x-icon" },
                            {".html", "text/html"},
                            {".js", "text/javascript"},
                            {".json", "application/json"},
                            {".css", "text/css"},
                            {".png", "image/png"},
                            {".jpg", "image/jpeg"},
                            {".gif", "image/gif"},
                            {".wav", "audio/wav"},
                            {".mp3", "audio/mpeg"},
                            {".svg", "image/svg+xml" },
                            {".pdf", "application/pdf"},
                            {".doc", "application/msword" }
                        };
                        var ext = Path.GetExtension(path);

                        res.ContentType = contentType.GetValueOrDefault(ext, "text/html");
                    }
                    catch
                    {
                        data = Encoding.UTF8.GetBytes("Not found");
                    }
                }
                else
                {
                    data = Encoding.UTF8.GetBytes("Not found");
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
