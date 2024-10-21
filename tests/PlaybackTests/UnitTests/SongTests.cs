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

            Song song = Song.Create($"{Assets}/Compiled/Navigating");
            song.Play();

            // Just play the song for 5 seconds
            Thread.Sleep(TimeSpan.FromSeconds(5));
            ShutdownEngine = true;
        }
    }
}