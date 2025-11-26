#if !IS_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsWrapGridLayout : VirtualizingPanelLayout
	{

		public override Orientation ScrollOrientation
		{
			get
			{
				if (Orientation == Orientation.Horizontal)
				{
					return Orientation.Vertical;
				}
				else
				{
					return Orientation.Horizontal;
				}

			}
		}
	}
}

#endif
