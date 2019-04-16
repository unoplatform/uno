using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public  partial class AppBarTemplateSettings : DependencyObject
    {
		private readonly AppBar _appBar;

		public AppBarTemplateSettings(AppBar appBar)
		{
			_appBar = appBar;
		}

		// TODO: Implement
		public Rect ClipRect => new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity); // No clipping
		public Thickness CompactRootMargin => new Thickness(0);
		public double CompactVerticalDelta => -16; // Imported from UWP runtime value
		public Thickness HiddenRootMargin => new Thickness(0);
		public double HiddenVerticalDelta => -56; // Imported from UWP runtime value
        public Thickness MinimalRootMargin => new Thickness(0);
		public double MinimalVerticalDelta => -32; // Imported from UWP runtime value
    }
}
