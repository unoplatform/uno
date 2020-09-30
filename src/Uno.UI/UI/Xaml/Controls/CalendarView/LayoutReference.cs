using System;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	internal struct LayoutReference
	{
		internal Windows.UI.Xaml.Controls.ReferenceIdentity RelativeLocation { get; set; }
		internal Windows.Foundation.Rect ReferenceBounds { get; set; }
		Windows.Foundation.Rect HeaderBounds { get; set; }
		bool ReferenceIsHeader { get; set; }
	};
}
