using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample shows three vertical bands: a spinning triangle, its color-inverted twin (both from one MRT pass), and a counter-spinning triangle resolved from a 4x MSAA target via glBlitFramebuffer. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_MRTBlit : UserControl
	{
		public GLCanvasElement_MRTBlit()
		{
			this.InitializeComponent();
		}
	}
}
