using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class TextChangedEventArgs : RoutedEventArgs
	{
		internal TextChangedEventArgs(object originalSource)
			: base(originalSource)
		{
		}
	}
}
