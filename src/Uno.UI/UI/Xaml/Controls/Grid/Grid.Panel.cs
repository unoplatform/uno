using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Grid
	{
#if __ANDROID__ || __IOS__ || __MACOS__
		private UIElementCollection GetChildren()
		{
			return Children;
		}
#endif
	}
}
