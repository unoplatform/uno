using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "Two quads driven by uniform buffer objects: a shared Scene block + a per-draw Transform block updated via glBufferSubData. Exercises WebGL2 UBO API.")]
	public sealed partial class GLCanvasElement_UniformBuffer : UserControl
	{
		public GLCanvasElement_UniformBuffer()
		{
			this.InitializeComponent();
		}
	}
}
