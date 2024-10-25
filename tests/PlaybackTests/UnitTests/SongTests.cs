using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace PlaybackTests.UnitTests
{
    [TestClass]
    public class SongTests
    {
        static readonly QueuedLock TestLock = new();

        const string Assets = @"../../../../TestAssets";

        const string CompiledAssets = $"{Assets}/Compiled";

        const string RawAssets = $"{Assets}/Raw";

        static bool ShutdownEngine = false;

        [TestMethod]
        public void PlaySong()
        {
            ShutdownEngine = false;

            AudioManager.Initialize(() => ShutdownEngine, 1f, 1f, 1f);
            Logger.LogMessage("Audio engine initialized");

            string songPath = $"{CompiledAssets}/Navigating";
            Song song = Song.CreateBuffered(songPath);
            Logger.LogMessage("Song {0} loaded", songPath);

            song.Play();
            Logger.LogMessage("Song playback started");

            int secondsToSleep = 60;
            Logger.LogMessage("Sleeping for {0} seconds", secondsToSleep);
            Thread.Sleep(TimeSpan.FromSeconds(secondsToSleep));

            Logger.LogMessage("Sleep finished - shutting down engine");
            ShutdownEngine = true;
        }
    }
}