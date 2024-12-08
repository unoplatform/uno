using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample should show a simple triangle. This only works with hardware acceleration (i.e. might not work in a VM).")]
	public sealed partial class GLCanvasElement_Simple : UserControl
	{
		public GLCanvasElement_Simple()
		{
			this.InitializeComponent();
		}
	}
}
