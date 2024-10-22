using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace PlaybackTests.UnitTests
{
    [TestClass]
    public class SongTests
    {
        static bool ShutdownEngine;

        const string Assets = @"../../../../TestAssets";

        [TestMethod]
        public void PlaySong()
        {
            ShutdownEngine = false;
            AudioManager.Initialize(() => ShutdownEngine, 1f, 1f, 1f);
            Logger.LogMessage("Audio engine initialized");

            string songPath = $"{Assets}/Compiled/Navigating";
            Song song = Song.CreateBuffered(songPath);
            Logger.LogMessage("Song {0} loaded", songPath);

            song.Play();
            Logger.LogMessage("Song playback started");

            int secondsToSleep = 10;
            Logger.LogMessage("Sleeping for {0} seconds", secondsToSleep);
            Thread.Sleep(TimeSpan.FromSeconds(secondsToSleep));

            Logger.LogMessage("Sleep finished - shutting down engine");
            ShutdownEngine = true;
        }
    }
}