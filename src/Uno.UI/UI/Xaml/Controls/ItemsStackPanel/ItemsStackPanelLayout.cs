using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{
		public override Orientation ScrollOrientation { get { return Orientation; } }
	}
}
