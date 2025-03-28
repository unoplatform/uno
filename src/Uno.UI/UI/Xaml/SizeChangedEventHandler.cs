using System;
using System.Linq;

#if __ANDROID__
using _Size = Windows.Foundation.Size;
#elif __IOS__
using _Size = Windows.Foundation.Size;
#else
using _Size = Windows.Foundation.Size;
#endif

namespace Windows.UI.Xaml
{
	public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs args);

	public partial class SizeChangedEventArgs : Windows.UI.Xaml.RoutedEventArgs
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
