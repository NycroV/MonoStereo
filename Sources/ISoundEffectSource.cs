﻿using MonoStereo.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereo.AudioSources
{
    public interface ISoundEffectSource : ISeekableSampleProvider
    {
        public PlaybackState PlaybackState { get; set; }

        public Dictionary<string, string> Comments { get; }

        public long Length { get; }

        public bool IsLooped { get; set; }

        public virtual void OnPlay() { }

        public virtual void OnResume() { }

        public virtual void OnPause() { }

        public virtual void OnStop() { }

        public abstract void Close();
    }
}
