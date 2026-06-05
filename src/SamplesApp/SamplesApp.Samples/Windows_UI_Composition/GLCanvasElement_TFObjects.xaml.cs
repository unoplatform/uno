using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample shows a ring of orbiting points whose positions are recomputed each frame via a named transform-feedback object (with a Pause/Resume decoy draw in the middle), copied to the render buffer with glCopyBufferSubData after a server-side glWaitSync. A collapsed ring indicates a Pause/Resume bug. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_TFObjects : UserControl
	{
		public GLCanvasElement_TFObjects()
		{
			this.InitializeComponent();
		}
	}
}
