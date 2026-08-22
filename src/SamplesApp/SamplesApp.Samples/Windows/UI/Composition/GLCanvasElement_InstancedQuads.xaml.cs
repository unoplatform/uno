using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample should show a grid of alpha-blended quads drawn via instanced rendering, with a pulsing tint. This only works with hardware acceleration (i.e. might not work in a VM).")]
	public sealed partial class GLCanvasElement_InstancedQuads : UserControl
	{
		public GLCanvasElement_InstancedQuads()
		{
			this.InitializeComponent();
		}
	}
}
