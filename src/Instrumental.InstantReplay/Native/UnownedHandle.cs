using System;
using System.Runtime.InteropServices;

namespace Instrumental.InstantReplay.Native
{
    internal sealed class UnownedHandle : SafeHandle
    {
        public UnownedHandle(IntPtr handle)
            : base(invalidHandleValue: IntPtr.Zero, ownsHandle: false)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle() => true;
    }
}
