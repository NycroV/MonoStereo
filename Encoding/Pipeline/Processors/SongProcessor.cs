using Microsoft.Xna.Framework.Content.Pipeline;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Song - MonoStereo")]
    
    public class SongProcessor : ContentProcessor<AudioFileReader, OggWriter>
    {
        public override OggWriter Process(AudioFileReader input, ContentProcessorContext context)
        {
            OggWriter file = new()
            {
                FileName = context.OutputFilename,
                Reader = input,
                Logger = context.Logger
            };

            return file;
        }
    }
}
