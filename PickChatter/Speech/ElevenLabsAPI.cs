using CSCore.Tags.ID3.Frames;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace PickChatter
{
    internal class ElevenLabsAPI
    {
        private static string ApiKey => SettingsManager.Instance.ElevenLabsAPIKey;

        public static bool GetAudio(string message, string voiceID, out byte[] audioData)
        {
            audioData = Array.Empty<byte>();
            if (string.IsNullOrEmpty(ApiKey))
            {
                audioData = Encoding.UTF8.GetBytes("Error: ElevenLabs API key not set.");
                return false;
            }

            var request = new RestRequest($"https://api.elevenlabs.io/v1/text-to-speech/{voiceID}?output_format=mp3_44100_128", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("xi-api-key", ApiKey);

            request.AddParameter("application/json", JsonSerializer.Serialize(new { text = message, model_id = "eleven_v3" }), ParameterType.RequestBody);
            var response = new RestClient().Execute(request);

            if (response.IsSuccessful && response.RawBytes != null)
            {
                audioData = response.RawBytes;
                return true;
            }

            audioData = response.RawBytes ?? [];

            return false;
        }

        public static List<(string Name, string ID)> GetVoices()
        {
            var request = new RestRequest("https://api.elevenlabs.io/v1/voices", Method.Get);
            request.AddHeader("xi-api-key", ApiKey);
            request.AddQueryParameter("page_size", "100");

            var response = new RestClient().Execute(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                return new();
            }

            var result = new List<(string Name, string ID)>();

            using JsonDocument doc = JsonDocument.Parse(response.Content);

            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("voices", out JsonElement voices))
            {
                foreach (JsonElement voice in voices.EnumerateArray())
                {
                    string id = voice.GetProperty("voice_id").GetString()!;
                    string name = voice.GetProperty("name").GetString()!;

                    result.Add((name, id));
                }
            }

            return result;
        }
    }
}
