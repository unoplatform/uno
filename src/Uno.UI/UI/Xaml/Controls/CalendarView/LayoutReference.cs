using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct LayoutReference
	{
		internal Microsoft.UI.Xaml.Controls.ReferenceIdentity RelativeLocation { get; set; }
		internal Windows.Foundation.Rect ReferenceBounds { get; set; }
		//Windows.Foundation.Rect HeaderBounds { get; set; }
		//bool ReferenceIsHeader { get; set; }
	};
}
