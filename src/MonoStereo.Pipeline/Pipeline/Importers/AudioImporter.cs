using Microsoft.Xna.Framework.Content.Pipeline;
using MonoStereo.Sources;

namespace MonoStereo.Pipeline
{
    [ContentImporter(".wav", ".mp3", ".ogg", DefaultProcessor = nameof(MonoStereoSongProcessor), DisplayName = "Audio Importer - MonoStereo")]
    public class MonoStereoImporter : ContentImporter<UniversalAudioSource>
    {
        public override UniversalAudioSource Import(string filename, ContentImporterContext context)
        {
            context.Logger.LogMessage("Importing MonoStereo audio: {0}", filename);
            var reader = new UniversalAudioSource(filename);
            context.Logger.LogMessage("Audio imported: {0}", filename);
            return reader;
        }
    }
}