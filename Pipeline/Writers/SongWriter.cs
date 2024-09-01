using MonoStereo.Encoding;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace MonoStereo.Pipeline
{
    public class SongWriter(AudioFileReader reader)
    {
        public AudioFileReader Reader { get; private set; } = reader;

        public void Write(string fileName, int quality)
        {
            FileStream stream = File.Create(fileName);
            OggWriter writer = new()
            {
                Quality = quality,
                Reader = Reader,
                FileName = fileName,
            };

            ISampleProvider resampleSource = Reader;
            if (resampleSource.WaveFormat.Channels != AudioStandards.ChannelCount)
                resampleSource = new MonoToStereoSampleProvider(resampleSource);

            WdlResamplingSampleProvider resampler = new(resampleSource, AudioStandards.SampleRate);
            writer.WriteToOgg(resampler, stream);

            stream.Dispose();
        }
    }
}
