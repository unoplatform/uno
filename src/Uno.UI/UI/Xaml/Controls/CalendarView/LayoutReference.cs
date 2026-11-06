using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct LayoutReference
	{
		internal ReferenceIdentity RelativeLocation { get; set; }
		internal Rect ReferenceBounds { get; set; }
		internal Rect HeaderBounds { get; set; }
		internal bool ReferenceIsHeader { get; set; }
	};
}
