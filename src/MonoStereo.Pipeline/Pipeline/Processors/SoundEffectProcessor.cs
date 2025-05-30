using Microsoft.Xna.Framework.Content.Pipeline;
using MonoStereo.Decoding;
using MonoStereo.Sources;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Sound Effect - MonoStereo")]
    public class MonoStereoSoundEffectProcessor : ContentProcessor<UniversalAudioSource, SoundEffectFileWriter>
    {
        public override SoundEffectFileWriter Process(UniversalAudioSource input, ContentProcessorContext context)
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