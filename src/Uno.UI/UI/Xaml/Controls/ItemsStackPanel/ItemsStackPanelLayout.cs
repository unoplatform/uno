#if !NET46 && !__MACOS__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{
		public override Orientation ScrollOrientation { get { return Orientation; } }
	}
}

#endif
