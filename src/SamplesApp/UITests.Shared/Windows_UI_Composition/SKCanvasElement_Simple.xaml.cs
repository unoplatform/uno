using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition")]
	public sealed partial class SKCanvasElement_Simple : UserControl
	{
#if __SKIA__
		public int MaxSampleIndex => SKCanvasElementImpl.SampleCount - 1;
#endif

		public SKCanvasElement_Simple()
		{
			this.InitializeComponent();
		}
	}
}
