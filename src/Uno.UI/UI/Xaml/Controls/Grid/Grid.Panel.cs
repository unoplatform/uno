using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Grid
	{
#if __ANDROID__ || __APPLE_UIKIT__ || __MACOS__
		private UIElementCollection GetChildren()
		{
			return Children;
		}
#endif
	}
}
