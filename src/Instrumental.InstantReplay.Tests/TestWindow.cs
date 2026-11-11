using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Instrumental.InstantReplay.Tests
{
    /// <summary>
    /// A borderless window showing solid-colored areas. It is shown without being activated, and it paints
    /// synchronously so that it can be captured right away.
    /// </summary>
    internal sealed class TestWindow : Form
    {
        private readonly List<(Rectangle Area, Color Color)> coloredAreas = new();

        public TestWindow(Rectangle bounds, Color color)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;

            // Windows enlarges windows that are created smaller than the minimum window size, but not windows that are
            // resized after they are created.
            CreateHandle();
            Bounds = bounds;

            Show();
            Fill(color);
        }

        protected override bool ShowWithoutActivation => true;

        public Rectangle ClientBounds => RectangleToScreen(ClientRectangle);

        public void Fill(Color color)
        {
            coloredAreas.Clear();
            Fill(ClientRectangle, color);
        }

        /// <param name="area">In client coordinates.</param>
        public void Fill(Rectangle area, Color color)
        {
            coloredAreas.Add((area, color));
            Invalidate(area);
            Update();
        }

        /// <summary>
        /// Places this window directly above <paramref name="other"/> in the z-order without activating it.
        /// </summary>
        public void PlaceAbove(TestWindow other)
        {
            // Placing a window after another puts it below that window, so the other window is placed below this one.
            if (!TestPInvoke.SetWindowPos((HWND)other.Handle, (HWND)Handle, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
                throw new Win32Exception();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            foreach (var (area, color) in coloredAreas)
            {
                using var brush = new SolidBrush(color);
                e.Graphics.FillRectangle(brush, area);
            }
        }
    }
}
