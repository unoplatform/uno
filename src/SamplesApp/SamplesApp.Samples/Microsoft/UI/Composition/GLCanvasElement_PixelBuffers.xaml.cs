using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample exercises pixel buffer objects: a checkerboard texture is uploaded from a PIXEL_UNPACK_BUFFER, read back through a PIXEL_PACK_BUFFER + glGetBufferSubData with a byte-exact Init-time self-check, and a rainbow band scrolls through it sourced from non-zero PBO offsets. A visible quad means the round-trip passed. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_PixelBuffers : UserControl
	{
		public GLCanvasElement_PixelBuffers()
		{
			this.InitializeComponent();
		}
	}
}
