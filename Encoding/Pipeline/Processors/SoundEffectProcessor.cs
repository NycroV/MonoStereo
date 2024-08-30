using Microsoft.Xna.Framework.Content.Pipeline;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Sound Effect - MonoStereo")]
    public class MonoStereoSoundEffectProcessor : ContentProcessor<AudioFileReader, WavWriter>
    {
        public override WavWriter Process(AudioFileReader input, ContentProcessorContext context)
        {
            WavWriter file = new()
            {
                FileName = context.OutputFilename,
                Reader = input,
                Logger = context.Logger
            };

            return file;
        }
    }
}
