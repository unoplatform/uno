using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "1024 GPU-updated particles via transform feedback with ping-pong VBOs. Exercises RASTERIZER_DISCARD + BeginTransformFeedback. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_TransformFeedback : UserControl
	{
		public GLCanvasElement_TransformFeedback()
		{
			this.InitializeComponent();
		}
	}
}
