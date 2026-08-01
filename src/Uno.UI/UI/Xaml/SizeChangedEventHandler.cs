using System;
using System.Linq;

using _Size = Windows.Foundation.Size;

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
