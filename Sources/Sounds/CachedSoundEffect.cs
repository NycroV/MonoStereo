using MonoStereo.Encoding;
using NAudio.Wave;
using System;
using System.Collections.Immutable;

namespace MonoStereo
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
            using var fileReader = new SoundEffectFileReader(fileName);

            FileName = fileName;
            WaveFormat = fileReader.WaveFormat;

            var buffer = new float[fileReader.Length];
            fileReader.Read(buffer, 0, buffer.Length);

            AudioData = buffer;
            Comments = fileReader.Comments;

            AudioManager.CachedSounds.Add(this);
        }

        public CachedSoundEffect(Pipeline.AudioFileReader source)
        {
            FileName = source.FileName;
            WaveFormat = source.WaveFormat;

            var buffer = new float[source.Length];
            source.Read(buffer, 0, buffer.Length);

            AudioData = buffer;
            Comments = source.Comments.ToImmutableDictionary();

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
