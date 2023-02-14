using Android.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
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
