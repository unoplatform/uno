using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample renders a spinning indexed cube (glDrawElements with an EBO) with black wireframe edges, back-face culling, and a two-tone scissored background. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_IndexedMesh : UserControl
	{
		public GLCanvasElement_IndexedMesh()
		{
			this.InitializeComponent();
		}
	}
}
