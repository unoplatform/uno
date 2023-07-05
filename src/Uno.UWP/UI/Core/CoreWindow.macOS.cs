#nullable disable

#if __MACOS__
using System;
using AppKit;

namespace Windows.UI.Core
{
	partial class CoreWindow
	{
		private readonly NSWindow _window;

		public CoreWindow(NSWindow window)
			: this()
		{
			_window = window;
		}

		/// <summary>
		/// Gets a reference to the native macOS Window behind the <see cref="CoreWindow"/> abstraction.
		/// </summary>
		internal NSWindow NativeWindow => _window;
	}
}
#endif
