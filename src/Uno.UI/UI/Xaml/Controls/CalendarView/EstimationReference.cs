using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct EstimationReference
	{
		internal ElementType ElementType { get; set; }
		internal int ElementIndex { get; set; }
		internal Rect ElementBounds { get; set; }
	};
}
