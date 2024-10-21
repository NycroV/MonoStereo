using Microsoft.Xna.Framework.Content.Pipeline;
using MonoStereo.Decoding;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Sound Effect - MonoStereo")]
    public class MonoStereoSoundEffectProcessor : ContentProcessor<AudioFileReader, SoundEffectFileWriter>
    {
        public override SoundEffectFileWriter Process(AudioFileReader input, ContentProcessorContext context)
        {
            SoundEffectFileWriter file = new()
            {
                FileName = context.OutputFilename,
                Reader = input,
                Logger = context.Logger
            };

            return file;
        }
    }
}