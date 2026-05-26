using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal sealed class AmazonSpeechManager : ServerSpeechManager
    {
        public override List<string> AvailableVoices => [
            "Isabelle", "Danielle", "Gregory", "Burcu", "Jitka", "Sabrina", "Joanna", "Ruth", "Lupe", "Jasmine", "Jihye", "Kevin", "Filiz", "Elin", "Astrid",
            "Tatyana", "Maxim", "Carmen", "Ines", "Cristiano", "Vitoria", "Ricardo", "Camila", "Maja", "Jan", "Jacek", "Ewa", "Ola", "Lisa", "Ruben", "Lotte",
            "Laura", "Ida", "Liv", "Seoyeon", "Kazuha", "Tomoko", "Takumi", "Mizuki", "Bianca", "Giorgio", "Carla", "Karl", "Dora", "Mathieu", "Lea", "Celine",
            "Chantal", "Gabrielle", "Penelope", "Miguel", "Mia", "Lucia", "Enrique", "Conchita", "Geraint", "Salli", "Matthew", "Kimberly", "Kendra", "Justin",
            "Joey", "Ivy", "Aria", "Ayanda", "Raveena", "Aditi", "Emma", "Brian", "Amy", "Russell", "Nicole", "Olivia", "Vicki", "Marlene", "Hans", "Naja",
            "Mads", "Sofie", "Gwyneth", "Zhiyu", "Zeina", "Hala", "Arlet", "Hannah", "Stephen", "Kajal", "Hiujin", "Suvi", "Niamh", "Tiffany", "Arthur", "Daniel",
            "Liam", "Pedro", "Sergio", "Andres", "Remi", "Adriano", "Thiago", "Zayd", "Lennart", "Ambre", "Florian", "Beatrice", "Lorenzo"
        ];

        protected override string VoiceID => CurrentVoice;

        public AmazonSpeechManager() { }

        private static AmazonSpeechManager instance = new();
        public static AmazonSpeechManager Instance => instance;

        protected override string GetAudioUrl(string message)
        {
            return $"https://regynate.com/tts/polly?voice={VoiceID}&text={Uri.EscapeDataString(message)}";
        }
    }
}
