using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif
using Instrumental.InstantReplay;

namespace InstantReplaySamples
{
    public static class SampleGif
    {
        public const string ButtonText = "Save GIF";
        public const string OpenWindowButtonText = "Open another window";
        public const string Instructions = "Instrumental.InstantReplay is recording every window in this app. Type, click around, or open more windows, then save a GIF of the last ten seconds.";

#if !NET35
        /// <summary>
        /// Saves a GIF of the last ten seconds to the Instrumental.InstantReplay folder on the desktop and opens it.
        /// The GIF is encoded on a background thread so that the UI doesn't pause.
        /// </summary>
        /// <returns>A message describing the result.</returns>
        public static Task<string> SaveToDesktopAndOpenAsync() => Task.Run(SaveToDesktopAndOpen);
#endif

        /// <summary>
        /// Saves a GIF of the last ten seconds to the Instrumental.InstantReplay folder on the desktop and opens it.
        /// The GIF is encoded on a background thread so that the UI doesn't pause. For apps targeting .NET Framework 3.5,
        /// where <c>Task</c> isn't available.
        /// </summary>
        /// <param name="onCompleted">Called on a background thread with a message describing the result.</param>
        public static void SaveToDesktopAndOpenInBackground(Action<string> onCompleted)
        {
            ThreadPool.QueueUserWorkItem(_ => onCompleted(SaveToDesktopAndOpen()));
        }

        private static string SaveToDesktopAndOpen()
        {
            try
            {
                if (InstantReplayCamera.SaveGif() is not { } bytes)
                    return "No frames have been recorded yet.";

                var directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Instrumental.InstantReplay");
                Directory.CreateDirectory(directoryPath);

                var filePath = Path.Combine(directoryPath, $"{DateTime.Now:yyyy-MM-dd HH.mm.ss}.gif");
                File.WriteAllBytes(filePath, bytes);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true })?.Dispose();

                return "Saved " + filePath;
            }
#pragma warning disable CA1031 // The sample reports every failure in its UI
            catch (Exception ex)
#pragma warning restore CA1031
            {
                return "Could not save the GIF: " + ex.Message;
            }
        }
    }
}
