using System;
using System.ComponentModel;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.StationsAndDesktops;

namespace Instrumental.InstantReplay.Tests
{
    /// <summary>
    /// Runs the tests in a class on a dedicated thread that is attached to a desktop which is never shown, so that the
    /// windows the tests create don't appear on screen. The real mouse cursor is not drawn into GIFs captured there.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class HiddenDesktopAttribute : RequiresThreadAttribute, ITestAction
    {
        private const string DesktopNamePrefix = "Instrumental.InstantReplay.Tests ";

        private HDESK originalDesktop;
        private CloseDesktopSafeHandle? hiddenDesktop;

        // SetThreadDesktop fails for a thread that already has windows, and initializing a single-threaded COM
        // apartment creates a window.
        public HiddenDesktopAttribute() : base(ApartmentState.MTA)
        {
        }

        public ActionTargets Targets => ActionTargets.Suite;

        public void BeforeTest(ITest test)
        {
            originalDesktop = TestPInvoke.GetThreadDesktop(TestPInvoke.GetCurrentThreadId());

            hiddenDesktop = TestPInvoke.CreateDesktop(DesktopNamePrefix + Guid.NewGuid(), 0, (uint)GENERIC_ACCESS_RIGHTS.GENERIC_ALL, null);
            if (hiddenDesktop.IsInvalid) throw new Win32Exception();

            if (!TestPInvoke.SetThreadDesktop(hiddenDesktop)) throw new Win32Exception();
        }

        public void AfterTest(ITest test)
        {
            // The hidden desktop can't be closed while this thread is still attached to it.
            if (!TestPInvoke.SetThreadDesktop(originalDesktop)) throw new Win32Exception();
            hiddenDesktop!.Dispose();
        }

        public static bool IsCurrentThreadOnHiddenDesktop
        {
            get
            {
                var desktop = TestPInvoke.GetThreadDesktop(TestPInvoke.GetCurrentThreadId());
                if (desktop.IsNull) throw new Win32Exception();

                unsafe
                {
                    const int nameCapacity = 256;
                    var name = stackalloc char[nameCapacity];

                    if (!TestPInvoke.GetUserObjectInformation((HANDLE)desktop.Value, USER_OBJECT_INFORMATION_INDEX.UOI_NAME, name, nameCapacity * sizeof(char), null))
                        throw new Win32Exception();

                    return new string(name).StartsWith(DesktopNamePrefix, StringComparison.Ordinal);
                }
            }
        }
    }
}
