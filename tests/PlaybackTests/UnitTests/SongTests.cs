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

        [TestMethod]
        public void PlaySong()
        {
            bool shutDownEngine = false;

            MonoStereoEngine.Initialize(() => shutDownEngine, 1f, 1f, 1f);
            Logger.LogMessage("Audio engine initialized");

            string songPath = $"{CompiledAssets}/Navigating";
            Song song = Song.CreateBuffered(songPath);
            Logger.LogMessage("Song {0} loaded", songPath);

            song.Play();
            Logger.LogMessage("Song playback started");

            int secondsToSleep = 10;
            Logger.LogMessage("Sleeping for {0} seconds", secondsToSleep);
            Thread.Sleep(TimeSpan.FromSeconds(secondsToSleep));

            Logger.LogMessage("Sleep finished - shutting down engine");
            shutDownEngine = true;

            while (MonoStereoEngine.IsRunning)
                Thread.Sleep(100);
        }

        [TestMethod]
        public void PlaySoundEffect()
        {
            bool shutDownEngine = false;

            MonoStereoEngine.Initialize(() => shutDownEngine, 1f, 1f, 1f);
            Logger.LogMessage("Audio engine initialized");

            string soundPath = $"{CompiledAssets}/Mumble";
            SoundEffect sound = SoundEffect.Create(soundPath);
            Logger.LogMessage("Sound effect {0} loaded", soundPath);

            sound.Play();
            Logger.LogMessage("Sound effect playback started");

            int secondsToSleep = 4;
            Logger.LogMessage("Sleeping for {0} seconds", secondsToSleep);
            Thread.Sleep(TimeSpan.FromSeconds(secondsToSleep));

            Logger.LogMessage("Sleep finished - shutting down engine");
            shutDownEngine = true;

            while (MonoStereoEngine.IsRunning)
                Thread.Sleep(100);
        }
    }
}