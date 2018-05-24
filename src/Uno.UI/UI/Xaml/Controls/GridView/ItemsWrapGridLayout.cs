#if !NET46 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
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