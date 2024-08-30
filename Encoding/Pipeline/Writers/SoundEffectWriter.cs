using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace MonoStereo.Pipeline
{
    [ContentTypeWriter]
    public class SoundEffectWriter : ContentTypeWriter<WavWriter>
    {
        protected override void Write(ContentWriter output, WavWriter value)
        {
            value.Logger.LogMessage("Writing song file: {0}", value.FileName);

            Stream stream = output.BaseStream;
            long length = stream.Length;
            stream.Position = 0;

            WdlResamplingSampleProvider resampler = new(value.Reader, AudioStandards.StandardSampleRate);
            value.WriteToWav(resampler, stream);

            if (stream.Position < length)
                stream.SetLength(stream.Position);

            value.Logger.LogMessage("Finished writing audio file: {0}", value.FileName);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform) => "";

        public override string GetRuntimeType(TargetPlatform targetPlatform) => "";
    }
}
