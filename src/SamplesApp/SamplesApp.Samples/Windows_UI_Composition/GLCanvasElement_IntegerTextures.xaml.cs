using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample renders spinning triangles into RGBA8UI (left) and RGBA8I (right) integer textures, cleared each frame with time-cycling values via glClearBufferuiv/glClearBufferiv, then visualized through usampler2D/isampler2D. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_IntegerTextures : UserControl
	{
		public GLCanvasElement_IntegerTextures()
		{
			this.InitializeComponent();
		}
	}
}
