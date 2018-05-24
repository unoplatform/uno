#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml
{
	public  partial class MediaFailedRoutedEventArgs : global::Windows.UI.Xaml.ExceptionRoutedEventArgs
	{
		public MediaFailedRoutedEventArgs() : base("")
		{
			throw new NotImplementedException();
		}
	}
}
