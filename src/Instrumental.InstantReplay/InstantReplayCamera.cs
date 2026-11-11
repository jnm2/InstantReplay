using System;
using System.Threading;

namespace Instrumental.InstantReplay
{
    /// <summary>
    /// <para>
    /// Buffers timed screenshots for all windows in the current process and tracks the mouse cursor so that an animated
    /// GIF can be created on demand, for inclusion in a crash report for example.
    /// </para>
    /// <para>
    /// Call <see cref="Start"/> once when the application starts, and then call <see cref="SaveGif"/> to obtain a GIF
    /// of the last ten seconds up to that point in time.
    /// </para>
    /// </summary>
    public static class InstantReplayCamera
    {
        private static Camera? camera;

        /// <summary>
        /// <para>
        /// Begins buffering up to ten seconds of screenshots for all windows in the current process, including windows
        /// that have not been created yet, as well as the mouse cursor.
        /// </para>
        /// <para>
        /// Call this during the start of your application. <see cref="SaveGif"/> will only have access to frames that
        /// occurred after this call. Subsequent calls to this method have no effect.
        /// </para>
        /// <para>
        /// This method is thread-safe and does not behave differently when called from the UI thread or any other
        /// thread.
        /// </para>
        /// </summary>
        /// <param name="reportBackgroundException">
        /// <para>
        /// Please report exceptions just as you would for <see cref="AppDomain.UnhandledException"/> and other
        /// top-level exception events such as <c>TaskScheduler.UnobservedTaskException</c> and
        /// <c>Application.ThreadException</c>. When you come across an exception that appears to be a flaw in
        /// Instrumental.InstantReplay, please report it at <see href="https://github.com/jnm2/InstantReplay/issues"/>.
        /// </para>
        /// <para>
        /// Ideally there will be no unhandled exceptions, but they are a normal part of the development cycle. This
        /// parameter is provided so that the runtime does not forcibly terminate your app due to an exception in the
        /// timer callback in Instrumental.InstantReplay.
        /// </para>
        /// </param>
        public static void Start(Action<Exception> reportBackgroundException)
        {
            if (reportBackgroundException is null) throw new ArgumentNullException(nameof(reportBackgroundException));

            var newCamera = new Camera(reportBackgroundException, TimeProvider.System);

            if (Interlocked.CompareExchange(ref camera, newCamera, null) is not null)
            {
                // This method has been called before. Ignore.
                return;
            }

            newCamera.Start();
        }

#if !NET35
        /// <summary>
        /// <para>
        /// Blocks while synchronously compositing, quantizing, and encoding all buffered screenshots and cursor
        /// movements and writing them to the array that is returned. No frames are erased by this call, and no new
        /// frames are buffered while this method is executing.
        /// </para>
        /// <para>
        /// ⚠ Consider using <see cref="System.Threading.Tasks.Task.Run(Action)"/> to prevent the CPU-intensive
        /// quantizing and encoding from making the application unresponsive.
        /// </para>
        /// <para>
        /// This method is thread-safe and does not behave differently when called from the UI thread or any other
        /// thread.
        /// </para>
        /// </summary>
#else
        /// <summary>
        /// <para>
        /// Generates a GIF of the currently-buffered screenshots and cursor movements. Returns <see langword="null"/>
        /// if there are no screenshots currently buffered. No frames are erased by this call, and no new frames are
        /// buffered while this method is executing.
        /// </para>
        /// <para>
        /// This method is thread-safe and does not behave differently when called from the UI thread or any other
        /// thread.
        /// </para>
        /// </summary>
#endif
        public static byte[]? SaveGif() => camera?.SaveGif();
    }
}
