using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NumberBoxValueChangedEventArgs
	{
		internal NumberBoxValueChangedEventArgs(double oldValue, double newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		public double OldValue { get; }

		public double NewValue { get; }
	}
}
