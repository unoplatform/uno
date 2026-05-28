using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample should show a scrolling textured quad with a procedural checkerboard. This only works with hardware acceleration (i.e. might not work in a VM).")]
	public sealed partial class GLCanvasElement_TexturedQuad : UserControl
	{
		public GLCanvasElement_TexturedQuad()
		{
			this.InitializeComponent();
		}
	}
}
