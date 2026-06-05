using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample renders an instanced grid of translucent wobbling quads driven by an integer per-instance attribute (glVertexAttribIPointer), with constant attributes, separate blend/stencil state, and a polygon-offset highlight pair. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_IntAttribsBlend : UserControl
	{
		public GLCanvasElement_IntAttribsBlend()
		{
			this.InitializeComponent();
		}
	}
}
