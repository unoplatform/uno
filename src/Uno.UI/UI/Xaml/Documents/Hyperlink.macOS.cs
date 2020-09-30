#if __MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Windows.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		private static Brush GetDefaultForeground()
		{
			// https://developer.apple.com/macos/human-interface-guidelines/visual-design/color/
			return new SolidColorBrush(Color.FromArgb(255, 0, 122, 255));
		}
	}
}
#endif
