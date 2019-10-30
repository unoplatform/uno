#if __MACOS__
using System;
using System.Runtime.InteropServices;
using AppKit;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public partial class CoreWindow 
	{
        private readonly NSWindow _window;

        public CoreWindow(NSWindow window) : this()
        {
            _window = window;
        }
	}
}
#endif
