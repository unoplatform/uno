using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class RangeBaseValueChangedEventArgs : RoutedEventArgs
	{
		internal RangeBaseValueChangedEventArgs(object originalSource)
			: base(originalSource)
		{
		}

		public double NewValue { get; internal set; }
		public double OldValue { get; internal set; }
	}
}
