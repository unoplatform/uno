#if __IOS__
using System;
using System.Runtime.InteropServices;
using UIKit;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public partial class CoreWindow 
	{
        private readonly UIWindow _window;

        public CoreWindow(UIWindow window) : this()
        {
            _window = window;
        }
	}
}
#endif
