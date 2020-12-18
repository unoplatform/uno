using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarLayoutStrategy
	{
		/// <inheritdoc />
		public Orientation VirtualizationDirection
		{
			get
			{
				GetVirtualizationDirection(out var value);
				return value;
			}
			set => SetVirtualizationDirection(value);
		}
	}
}
