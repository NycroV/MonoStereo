using Microsoft.Xna.Framework.Content.Pipeline;

namespace MonoStereo.Pipeline
{
    [ContentImporter(".wav", ".mp3", ".ogg", DefaultProcessor = nameof(SongProcessor), DisplayName = "Audio Importer - MonoStereo")]
    public class MonoStereoImporter : ContentImporter<AudioFileReader>
    {
        public override AudioFileReader Import(string filename, ContentImporterContext context)
        {
            context.Logger.LogMessage("Importing MonoStereo audio: {0}", filename);
            var reader = new AudioFileReader(filename);
            context.Logger.LogMessage("Audio imported: {0}", filename);
            return reader;
        }
    }
}
