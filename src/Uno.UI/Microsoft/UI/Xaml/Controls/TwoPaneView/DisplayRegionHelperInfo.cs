using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal struct DisplayRegionHelperInfo
	{
		public TwoPaneViewMode Mode { get; set; }
		public Rect[] Regions { get; set; }
	}
}
