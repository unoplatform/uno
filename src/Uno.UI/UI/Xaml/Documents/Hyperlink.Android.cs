#if __ANDROID__
using Android.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		private static Brush GetDefaultForeground()
		{
			var typedValue = new TypedValue();
			ContextHelper.Current.Theme.ResolveAttribute(Android.Resource.Attribute.TextColorLink, typedValue, true);
			var color = new Android.Graphics.Color(typedValue.Data);
			return new SolidColorBrush(color);
		}
	}
}
#endif
