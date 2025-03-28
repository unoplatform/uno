using System;
using System.Linq;

namespace Windows.UI.Xaml.Controls.Primitives
{
	internal enum CalendarPanelType
	{
		Invalid,
		Primary,                  // desired size is natural size
		Secondary,                // desired size is {0, 0}, children will be arranged by given dimension, they will be clipped if won't fit.
		Secondary_SelfAdaptive    // desired size is {0, 0}, children will be arranged based on their desired sizes, Panel will adjust the dimensions to make all of them can fit.
	};
}
