#if __WASM__

namespace Windows.UI.Xaml.Controls.Primitives
{//TODO: I'm not an expert in TemplateSettings logic so it's just a stub to make standard ProgressRing UWP style bindings work in Wasm
	public partial class ProgressRingTemplateSettings : DependencyObject
	{
		public double EllipseDiameter { get; set; }

		public Thickness EllipseOffset { get; set; }

		public double MaxSideLength { get; set; }
	}
}

#endif
