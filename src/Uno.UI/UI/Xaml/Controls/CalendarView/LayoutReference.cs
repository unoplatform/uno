using System;
using System.Linq;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct LayoutReference
	{
		internal Microsoft.UI.Xaml.Controls.ReferenceIdentity RelativeLocation { get; set; }
		internal Rect ReferenceBounds { get; set; }
		//Rect HeaderBounds { get; set; }
		//bool ReferenceIsHeader { get; set; }
	};
}
