using System;
using AppKit;

namespace Windows.UI.Core
{
	partial class CoreWindow
	{
		private NSWindow _window;

		internal void SetWindow(NSWindow window)
		{
			_window = window;
		}

		/// <summary>
		/// Gets a reference to the native macOS Window behind the <see cref="CoreWindow"/> abstraction.
		/// </summary>
		internal NSWindow NativeWindow => _window;
	}
}
