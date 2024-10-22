using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Runtime.InteropServices;

namespace PipelineTests
{
    [TestClass]
    public class NativeLibraryTests
    {
        [TestMethod]
        public void CopyAndLoadNativeLibrary()
        {
            // Resolve the file and contianing folder (platform specific).
            string platform = "win";
            string file = "portaudio.dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "linux";
                file = "libportaudio.so";
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "osx";
                file = "libportaudio.dylib";
            }

            // Create the full path for the library we want to access.
            string portAudioPath = $"runtimes/{platform}-x64/native/{file}";
            Logger.LogMessage("Designation portaudio path: {0}", portAudioPath);

            // Delete any copied files from previous tests.
            if (File.Exists(file))
            {
                File.Delete(file);
                Logger.LogMessage("Existing file found - deleted.");
            }

            // Attempt to load the library before copying. Should fail.
            bool firstLoad = NativeLibrary.TryLoad(file, out _);
            Assert.IsFalse(firstLoad);
            Logger.LogMessage("First load was unsuccessful");

            // Copy the designated library.
            var bytes = File.ReadAllBytes(portAudioPath);
            File.WriteAllBytes(file, bytes);
            Logger.LogMessage("Copied library file to {0}", file);

            // Attempt to load the library again now that it's been copied. Should succeed.
            bool secondLoad = NativeLibrary.TryLoad(file, out IntPtr handle2);
            Assert.IsTrue(secondLoad);
            Logger.LogMessage("Second load was successful");

            // Release the library handle so we can delete the copied file.
            NativeLibrary.Free(handle2);
            Logger.LogMessage("Library handle freed");

            // Delete the copied file.
            File.Delete(file);
            Logger.LogMessage("Copied library file deleted");
        }
    }
}