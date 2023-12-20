using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{
		public override Orientation ScrollOrientation { get { return Orientation; } }
	}
}
