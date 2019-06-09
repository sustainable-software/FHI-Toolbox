using System;
using System.Windows;
using System.Windows.Input;

namespace Fhi.Controls.Utils
{
    public class WaitCursor : IDisposable
    {
        private readonly Cursor _previousCursor;

        public WaitCursor(bool background = false)
        {
            if (Application.Current == null) return;
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = background ?  Cursors.AppStarting: Cursors.Wait;
        }

        public void Dispose()
        {
            if (Application.Current == null) return;
            Mouse.OverrideCursor = _previousCursor;
        }
    }
}