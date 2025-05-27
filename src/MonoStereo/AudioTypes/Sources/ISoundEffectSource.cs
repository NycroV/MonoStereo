using System;
using MonoStereo.Structures;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.Sources
{
    public interface ISoundEffectSource : ISampleProvider, IDisposable
    {
        public virtual ISoundEffectSource BaseSource { get => this; }

        public PlaybackState PlaybackState { get; set; }

        public Dictionary<string, string> Comments { get; }

        public bool IsLooped { get; set; }

        public virtual void OnPlay() { }

        public virtual void OnResume() { }

        public virtual void OnPause() { }

        public virtual void OnStop() { }

        public virtual void OnRemoveInput() { }
    }

    public interface ISeekableSoundEffectSource : ISoundEffectSource, ISeekable { }
}
