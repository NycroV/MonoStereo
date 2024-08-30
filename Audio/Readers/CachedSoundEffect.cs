using MonoStereo.Encoding;
using NAudio.Wave;
using System;
using System.Collections.Immutable;

namespace MonoStereo.Audio
{
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
            using var fileReader = new WavReader(fileName);

            FileName = fileName;
            WaveFormat = fileReader.WaveFormat;

            var buffer = new float[fileReader.Length / AudioStandards.BytesPerSample];
            fileReader.Read(buffer, 0, buffer.Length);

            AudioData = buffer;
            Comments = fileReader.Comments;
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
            FileName = null;
            WaveFormat = null;
            AudioData = null;

            GC.SuppressFinalize(this);
        }
    }
}
