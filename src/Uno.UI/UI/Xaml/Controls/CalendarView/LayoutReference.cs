using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	internal struct LayoutReference
	{
		internal Windows.UI.Xaml.Controls.ReferenceIdentity RelativeLocation { get; set; }
		internal Rect ReferenceBounds { get; set; }
		//Rect HeaderBounds { get; set; }
		//bool ReferenceIsHeader { get; set; }
	};
}
