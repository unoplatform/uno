#if __WASM__

namespace Windows.UI.Xaml.Controls.Primitives
{//TODO: Fully implement this class, matching UWP https://github.com/unoplatform/uno/issues/1872
	public partial class ProgressRingTemplateSettings : DependencyObject
	{
		public double EllipseDiameter { get; set; }

		public Thickness EllipseOffset { get; set; }

		public double MaxSideLength { get; set; }
	}
}

#endif
