using CppNet;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using MonoStereo.Decoding;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace MonoStereo.Pipeline
{
    [ContentTypeWriter]
    public class SongWriter : ContentTypeWriter<OggWriter>
    {
        protected override void Write(ContentWriter output, OggWriter value)
        {
            value.Logger.LogMessage("Writing song file: {0} (Quality: {1})", value.FileName, value.Quality);

            Stream stream = output.BaseStream;
            long length = stream.Length;
            stream.Position = 0;

            ISampleProvider resampleSource = value.Reader;

            if (resampleSource.WaveFormat.SampleRate != AudioStandards.SampleRate)
                resampleSource = new WdlResamplingSampleProvider(resampleSource, AudioStandards.SampleRate);

            if (resampleSource.WaveFormat.Channels != AudioStandards.ChannelCount)
                resampleSource = new MonoToStereoSampleProvider(resampleSource);

            WdlResamplingSampleProvider resampler = new(resampleSource, AudioStandards.SampleRate);
            value.WriteToOgg(resampler, stream);

            if (stream.Position < length)
                stream.SetLength(stream.Position);

            value.Logger.LogMessage("Finished writing audio file: {0}", value.FileName);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform) => "";

        public override string GetRuntimeType(TargetPlatform targetPlatform) => "";
    }
}