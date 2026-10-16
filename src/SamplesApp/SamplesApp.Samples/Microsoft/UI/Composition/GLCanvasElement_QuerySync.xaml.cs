using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample renders waving palette-colored bars with a hidden probe quad behind them measured by an occlusion query: the top-left indicator turns green whenever any probe samples pass and red while it is fully occluded. Each frame is also fenced with glFenceSync. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_QuerySync : UserControl
	{
		public GLCanvasElement_QuerySync()
		{
			this.InitializeComponent();
		}
	}
}
