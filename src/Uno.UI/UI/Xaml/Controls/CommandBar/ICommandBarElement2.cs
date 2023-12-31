using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial interface ICommandBarElement2
	{
		int DynamicOverflowOrder { get; set; }
		bool IsInOverflow { get; }
	}
}
