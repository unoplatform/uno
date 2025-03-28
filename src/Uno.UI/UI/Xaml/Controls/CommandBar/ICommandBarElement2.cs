using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial interface ICommandBarElement2
	{
		int DynamicOverflowOrder { get; set; }
		bool IsInOverflow { get; }
	}
}
