using MonoStereo.Encoding;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace MonoStereo.Pipeline
{
    public class SoundEffectWriter(AudioFileReader reader)
    {
        public AudioFileReader Reader { get; private set; } = reader;

        public void Write(string fileName)
        {
            FileStream stream = File.Create(fileName);
            SoundEffectFileWriter writer = new()
            {
                Reader = Reader,
                FileName = fileName
            };

            ISampleProvider resampleSource = Reader;
            if (resampleSource.WaveFormat.Channels != AudioStandards.ChannelCount)
                resampleSource = new MonoToStereoSampleProvider(resampleSource);

            WdlResamplingSampleProvider resampler = new(resampleSource, AudioStandards.SampleRate);
            writer.WriteToWav(resampler, stream);

            stream.Dispose();
        }
    }
}
