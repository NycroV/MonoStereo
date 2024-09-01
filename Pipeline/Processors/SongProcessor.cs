using Microsoft.Xna.Framework.Content.Pipeline;
using MonoStereo.Encoding;
using System.ComponentModel;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Song - MonoStereo")]
    public class MonoStereoSongProcessor : ContentProcessor<AudioFileReader, OggWriter>
    {
        [DefaultValue(5)]
        public int Quality = 5;

        public override OggWriter Process(AudioFileReader input, ContentProcessorContext context)
        {
            OggWriter file = new()
            {
                FileName = context.OutputFilename,
                Reader = input,
                Logger = context.Logger,
                Quality = Quality
            };

            return file;
        }
    }
}
