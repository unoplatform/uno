using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "Stencil masking: a moving circular stencil clips a colorful gradient fill. Exercises stencil func/op/mask + color mask + multi-pass.")]
	public sealed partial class GLCanvasElement_StencilMask : UserControl
	{
		public GLCanvasElement_StencilMask()
		{
			this.InitializeComponent();
		}
	}
}
