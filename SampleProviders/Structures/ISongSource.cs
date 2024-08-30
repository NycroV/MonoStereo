using NAudio.Wave;
using System.Collections.Immutable;

namespace MonoStereo.SampleProviders
{
    public interface ISongSource : ISeekableSampleProvider
    {
        public PlaybackState PlaybackState { get; set; }

        public ImmutableDictionary<string, string> Comments { get; }

        public long Length { get; }

        public bool IsLooped { get; set; }

        public virtual void Resume() => PlaybackState = PlaybackState.Playing;

        public virtual void Pause() => PlaybackState = PlaybackState.Paused;

        public virtual void Stop() => PlaybackState = PlaybackState.Stopped;

        public abstract void Close();
    }
}
