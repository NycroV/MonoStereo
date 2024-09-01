using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.AudioSources
{
    public interface ISongSource : ISeekableSampleProvider
    {
        public PlaybackState PlaybackState { get; set; }

        public Dictionary<string, string> Comments { get; }

        public long Length { get; }

        public bool IsLooped { get; set; }

        public virtual void Resume() => PlaybackState = PlaybackState.Playing;

        public virtual void Pause() => PlaybackState = PlaybackState.Paused;

        public virtual void Stop() => PlaybackState = PlaybackState.Stopped;

        public abstract void Close();
    }
}
