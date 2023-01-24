using Android.Views;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class IconElement
	{
		partial void UnregisterSubView()
		{
			RemoveViewAt(0);
		}

		partial void RegisterSubView(View child)
		{
			AddView(child);
		}
	}
}
