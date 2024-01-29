using System;
using System.Collections.Generic;
using System.Text;

#if NETFX_CORE
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public static class ImageSourceHelper
	{
#if NETFX_CORE
		public static ImageSource Create(string source)
		{
			return new BitmapImage(new Uri(source));
		}
#elif XAMARIN
		public static string Create(string source)
		{
			return source;
		}
#endif
	}
}
