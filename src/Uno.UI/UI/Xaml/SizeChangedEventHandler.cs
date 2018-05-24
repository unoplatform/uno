using System;
using System.Linq;

#if XAMARIN_ANDROID
using _Size = Windows.Foundation.Size;
#elif XAMARIN_IOS_UNIFIED
using _Size = Windows.Foundation.Size;
#else
using _Size = Windows.Foundation.Size;
#endif

namespace Windows.UI.Xaml
{
	public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs args);

	public partial class SizeChangedEventArgs : Windows.UI.Xaml.RoutedEventArgs
	{
		public SizeChangedEventArgs(_Size previousSize, _Size newSize)
		{
			PreviousSize = previousSize;
			NewSize = newSize;
		}

		public _Size NewSize { get; }

		public _Size PreviousSize { get; }
	}
}