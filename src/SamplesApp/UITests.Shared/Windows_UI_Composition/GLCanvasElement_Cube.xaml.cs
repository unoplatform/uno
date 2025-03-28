using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample should show a 3d rotating cube. This only works with hardware acceleration (i.e. might not work in a VM).")]
	public sealed partial class GLCanvasElement_Cube : UserControl
	{
		public GLCanvasElement_Cube()
		{
			this.InitializeComponent();
		}
	}
}
