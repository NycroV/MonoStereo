using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using MonoStereo.Sources.Songs;
using System.Windows.Input;

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

            Song song = Song.Create($"{CompiledAssets}/Navigating");
            song.Play();

            float seconds = 5; // (float)(song.Source as SongReader).Length / AudioStandards.SampleRate / AudioStandards.ChannelCount;
            Logger.LogMessage("Sleeping for {0} seconds", seconds);

            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            ShutdownEngine = true;
        }

        [TestMethod]
        public void PlayBufferedSong()
        {
            ShutdownEngine = false;
            AudioManager.Initialize(() => ShutdownEngine, 1f, 1f, 1f);

            Song song = Song.CreateBuffered($"{CompiledAssets}/Navigating");
            song.Play();

            float seconds = 5; // (float)(song.Source as SeekableBufferedSongReader).Length / AudioStandards.SampleRate / AudioStandards.ChannelCount;
            Logger.LogMessage("Sleeping for {0} seconds", seconds);

            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            ShutdownEngine = true;
        }
    }
}