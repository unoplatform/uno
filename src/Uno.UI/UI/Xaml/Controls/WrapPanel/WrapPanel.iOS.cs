using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class WrapPanel : Panel
	{
		public WrapPanel()
		{
		}

		partial void OnItemHeightChangedPartial()
		{
			LayoutIfNeeded();
		}

		partial void OnItemWidthChangedPartial()
		{
			LayoutIfNeeded();
		}

		partial void OnOrientationChangedPartial()
		{
			LayoutIfNeeded();
		}
	}
}
