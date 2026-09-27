using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample renders a spinning triangle to an offscreen FBO, then samples it with a wavy distortion as a final composite. Exercises multi-pass rendering. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_PostProcess : UserControl
	{
		public GLCanvasElement_PostProcess()
		{
			this.InitializeComponent();
		}
	}
}
