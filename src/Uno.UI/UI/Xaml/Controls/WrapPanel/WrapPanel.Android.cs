using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class WrapPanel : Panel
	{
		public WrapPanel()
		{
		}

		partial void OnItemHeightChangedPartial()
		{
			RequestLayout();
		}

		partial void OnItemWidthChangedPartial()
		{
			RequestLayout();
		}

		partial void OnOrientationChangedPartial()
		{
			RequestLayout();
		}
	}
}
