using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Windows.UI.Text;
using Windows.UI.Xaml;

namespace Uno.UI
{
	internal static class TypefaceStyleHelper
	{
		internal static TypefaceStyle GetTypefaceStyle(bool italic, FontWeight fontWeight)
		{
			var style = TypefaceStyle.Normal;

			if (fontWeight.Weight > 500)
			{
				style |= TypefaceStyle.Bold;
			}

			if (italic)
			{
				style |= TypefaceStyle.Italic;
			}

			return style;
		}
	}
}
