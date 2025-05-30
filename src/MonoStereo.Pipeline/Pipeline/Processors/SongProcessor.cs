using Microsoft.Xna.Framework.Content.Pipeline;
using MonoStereo.Decoding;
using MonoStereo.Sources;
using System.ComponentModel;

namespace MonoStereo.Pipeline
{
    [ContentProcessor(DisplayName = "Song - MonoStereo")]
    public class MonoStereoSongProcessor : ContentProcessor<UniversalAudioSource, OggWriter>
    {
        [DefaultValue(5)]
        public int Quality { get; set; } = 5;

        public override OggWriter Process(UniversalAudioSource input, ContentProcessorContext context)
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
