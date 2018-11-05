#if !NET46

using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;

namespace Windows.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		public KeyRoutedEventArgs()
		{
		}

		public bool Handled { get; set; }
		public VirtualKey Key { get; internal set; }

		//TODO
		//public CorePhysicalKeyStatus KeyStatus { get; }
	}
}
#endif
