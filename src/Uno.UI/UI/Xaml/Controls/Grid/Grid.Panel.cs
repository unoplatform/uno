using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Grid
	{
#if __ANDROID__ || __APPLE_UIKIT__
		private UIElementCollection GetChildren()
		{
			return Children;
		}
#endif
	}
}
