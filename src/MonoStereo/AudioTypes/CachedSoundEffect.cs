using MonoStereo.Decoding;
using MonoStereo.Structures;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using System.IO;
using MonoStereo.Sources;

namespace MonoStereo
{
    /// <summary>
    /// The <see cref="CachedSoundEffect"/> provides a way to load a sound into memory for extremely performance-effective  playback at the cost of some memory overhead.<br/>
    /// <br/>
    /// In cases where a sound is expected to be played multiple times, this solution is typically far more effective than opening a new file stream for each instance.
    /// </summary>
    public class CachedSoundEffect : ILoopTags, IDisposable
    {
        public string FileName { get; private set; }

        public WaveFormat WaveFormat { get; private set; }

        public ImmutableDictionary<string, string> Comments { get; private set; }

        public float[] AudioData { get; private set; }

        public long Length { get; private set; }

        public long LoopStart { get; private set; }

        public long LoopEnd { get; private set; }

        /// <summary>
        /// Creates a new <see cref="CachedSoundEffect"/> from the file at the specified path.<br/>
        /// If the passed in file path does not have a file extension, MonoStereo will assume you have run the file through the content pipeline and append .xnb to the end.
        /// </summary>
        [UsedImplicitly]
        public static CachedSoundEffect Create(string fileName)
        {
            var source = new UniversalAudioSource(fileName, true);
            return CachedSoundEffect.Create(source, fileName, source.Comments);
        }

        /// <summary>
        /// Creates a new <see cref="CachedSoundEffect"/> from the given <see cref="ISampleProvider"/>, with the attached <paramref name="fileName"/> and <paramref name="comments"/> as metadata.
        /// </summary>
        [UsedImplicitly]
        public static CachedSoundEffect Create(ISampleProvider source, string fileName = "", IDictionary<string, string> comments = null)
        {
            return new CachedSoundEffect(source, fileName, comments);
        }

        [UsedImplicitly]
        protected CachedSoundEffect(ISampleProvider source, string fileName = "", IDictionary<string, string> comments = null)
        {
            FileName = fileName;
            comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            int samplesRead;
            float[] buffer = new float[AudioStandards.ReadBufferSize];
            List<float> audioData = [];

            long lengthPlaceholder = 0;
            source = UniversalAudioSource.Reformat(source, ref loopStart, ref loopEnd, ref lengthPlaceholder);

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            do
            {
                samplesRead = source.Read(buffer, 0, buffer.Length);
                audioData.AddRange(buffer.Take(samplesRead));
            }
            while (samplesRead > 0);

            WaveFormat = source.WaveFormat;
            AudioData = audioData.ToArray();

            Length = AudioData.LongLength;
            Comments = comments?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;

            audioData.Clear();
        }

        [UsedImplicitly]
        public SoundEffect GetInstance() => SoundEffect.Create(this);

        [UsedImplicitly]
        public SoundEffect PlayInstance()
        {
            SoundEffect instance = SoundEffect.Create(this);
            instance.Play();
            return instance;
        }

        public void Dispose()
        {
            FileName = null;
            WaveFormat = null;
            AudioData = null;
            Comments = null;

            GC.SuppressFinalize(this);
        }
    }
}
