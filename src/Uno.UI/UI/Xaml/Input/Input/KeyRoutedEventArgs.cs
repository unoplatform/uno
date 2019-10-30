#if !NET461

using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		internal KeyRoutedEventArgs(object originalSource, VirtualKey key)
			: base(originalSource)
		{
			Key = key;
		}

		public bool Handled { get; set; }
		public VirtualKey Key { get; }

		//TODO
		//public CorePhysicalKeyStatus KeyStatus { get; }
	}
}
#endif
