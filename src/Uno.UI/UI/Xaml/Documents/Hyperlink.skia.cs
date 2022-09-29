#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Windows.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		private static Brush GetDefaultForeground()
		{
			return new SolidColorBrush(Color.FromArgb(255, 0, 122, 255));
		}
	}
}
