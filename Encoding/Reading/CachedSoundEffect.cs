using MonoStereo.Encoding;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MonoStereo
{
    // The cached sound effect provides a way to load a sound into memory for extremely performance-effective
    // playback at the cost of some memory overhead. In cases where a sound is expected to be played multiple
    // times, this solution is typically far more effective than opening a new file stream for each instance.
    public class CachedSoundEffect : IDisposable
    {
        public string FileName { get; private set; }

        public WaveFormat WaveFormat { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; private set; }

        public float[] AudioData { get; private set; }

        public long LoopStart { get; private set; }

        public long LoopEnd { get; private set; }

        public CachedSoundEffect(string fileName)
        {
            using var fileReader = new SoundEffectFileReader(fileName);

            FileName = fileName;
            WaveFormat = fileReader.WaveFormat;

            var buffer = new float[fileReader.Length];
            fileReader.Read(buffer, 0, buffer.Length);

            AudioData = buffer;
            Comments = fileReader.Comments;

            Comments.ParseLoop(out long loopStart, out long loopEnd, WaveFormat.Channels);
            AudioManager.CachedSounds.Add(this);

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public CachedSoundEffect(ISampleProvider source, string fileName = "", IDictionary<string, string> comments = null)
        {
            FileName = fileName;

            int samplesRead;
            float[] buffer = new float[AudioStandards.ReadBufferSize];
            List<float> audioData = [];
            comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            if (source.WaveFormat.SampleRate != AudioStandards.SampleRate)
            {
                float scalar = AudioStandards.SampleRate / (float)source.WaveFormat.SampleRate;
                source = new WdlResamplingSampleProvider(source, AudioStandards.SampleRate);

                if (loopStart >= 0)
                {
                    loopStart = (long)(loopStart * scalar);
                    loopStart -= loopStart % source.WaveFormat.Channels;
                }

                if (loopEnd >= 0)
                {
                    loopEnd = (long)(loopEnd * scalar);
                    loopEnd -= loopEnd % source.WaveFormat.Channels;
                }
            }

            if (source.WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (source.WaveFormat.Channels != 1)
                    throw new ArgumentException("Source must be in either stereo or mono!", nameof(source));

                source = new MonoToStereoSampleProvider(source);
            }

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            do
            {
                samplesRead = source.Read(buffer, 0, buffer.Length);
                audioData.AddRange(buffer.Take(samplesRead));
            }
            while (samplesRead > 0);

            AudioData = audioData.ToArray();
            Comments = comments?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;

            audioData.Clear();
            AudioManager.CachedSounds.Add(this);
        }

        public SoundEffect GetInstance() => new(this);

        public SoundEffect PlayInstance()
        {
            SoundEffect instance = new(this);
            instance.Play();
            return instance;
        }

        public void Dispose()
        {
            AudioManager.CachedSounds.Remove(this);

            FileName = null;
            WaveFormat = null;
            AudioData = null;

            GC.SuppressFinalize(this);
        }
    }
}
