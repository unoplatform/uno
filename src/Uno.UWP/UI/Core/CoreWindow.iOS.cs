#nullable disable

using System;
using System.Runtime.InteropServices;
using UIKit;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Windows.UI.Core
{
	public partial class CoreWindow  : ICoreWindowEvents
	{
		public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyDown;

		public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyUp;

        private readonly UIWindow _window;

        public CoreWindow(UIWindow window) : this()
        {
            _window = window;
        }

		void ICoreWindowEvents.RaiseKeyDown(KeyEventArgs eventArgs) =>		
			KeyDown?.Invoke(this, eventArgs);
		

		void ICoreWindowEvents.RaiseKeyUp(KeyEventArgs eventArgs) =>
			KeyUp?.Invoke(this, eventArgs);
		
	}
}