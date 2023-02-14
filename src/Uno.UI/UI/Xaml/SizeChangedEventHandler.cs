using System;
using System.Linq;

#if XAMARIN_ANDROID
using _Size = Windows.Foundation.Size;
#elif XAMARIN_IOS_UNIFIED
using _Size = Windows.Foundation.Size;
#else
using _Size = Windows.Foundation.Size;
#endif

namespace Microsoft.UI.Xaml
{
	public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs args);

	public partial class SizeChangedEventArgs : Microsoft.UI.Xaml.RoutedEventArgs
	{
		internal SizeChangedEventArgs(object originalSource, _Size previousSize, _Size newSize)
			: base(originalSource)
		{
			PreviousSize = previousSize;
			NewSize = newSize;
		}

		public _Size NewSize { get; }

		public _Size PreviousSize { get; }
	}
}
