using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public partial class CoreWindow
	{
		public CoreDispatcher Dispatcher
			=> CoreDispatcher.Main;

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetAsyncKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;
	}
}
