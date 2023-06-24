using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PickChatter
{
    internal class MediaPlayer : Component
    {
        private ISoundOut? soundOut;
        private IWaveSource? waveSource;

        public event EventHandler<PlaybackStoppedEventArgs>? MediaEnded;
        public event EventHandler<UnhandledExceptionEventArgs>? MediaFailed;
        public event EventHandler<EventArgs>? MediaOpened;

        public TimeSpan Position
        {
            get => waveSource?.GetPosition() ?? TimeSpan.Zero;
            set => waveSource?.SetPosition(value);
        }

        public TimeSpan Length
        {
            get => waveSource?.GetLength() ?? TimeSpan.Zero;
        }

        public int Volume
        {
            get
            {
                if (soundOut != null)
                {
                    return Math.Min(100, Math.Max((int) (soundOut.Volume * 100), 0));
                }
                return 100;
            }
            set
            {
                if (soundOut != null)
                {
                    soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
                }
            }
        }

        private void OpenTask(string filename, MMDevice device)
        {
            Exception? exception = null;

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    waveSource = new MediaFoundationDecoder(filename)
                                .ToSampleSource()
                                .ToMono()
                                .ToWaveSource();
                    soundOut = new WasapiOut() { Latency = 100, Device = device };
                    soundOut.Initialize(waveSource);
                    if (MediaEnded != null)
                    {
                        soundOut.Stopped += MediaEnded;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(100);
                }
            }

            throw exception!;
        }

        public void Open(string filename, MMDevice device)
        {
            CleanupPlayback();
            Task.Run(() => OpenTask(filename, device)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    CleanupPlayback();
                    MediaFailed?.Invoke(this, new UnhandledExceptionEventArgs(t.Exception!, false));
                }
                if (t.IsCompletedSuccessfully)
                {
                    MediaOpened?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        public void Open(string filename)
        {
            Open(filename, new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Console));
        }

        public void Play()
        {
            Task.Run(() => soundOut?.Play());
        }

        public void Pause()
        {
            soundOut?.Pause();
        }

        public void Stop()
        {
            soundOut?.Stop();
        }

        private void CleanupPlayback()
        {
            if (soundOut != null)
            {
                try
                {
                    soundOut.Dispose();
                }
                catch { }
                soundOut = null;
            }
            if (waveSource != null)
            {
                try
                {
                    waveSource.Dispose();
                }
                catch { }
                waveSource = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CleanupPlayback();
        }
    }
}
