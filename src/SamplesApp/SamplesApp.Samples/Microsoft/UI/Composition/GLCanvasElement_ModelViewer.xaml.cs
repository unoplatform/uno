using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "A lit sphere encircled by a tilted ring, drawn through GLCanvasElement (port of the internal WASM-AOT GL repro). Exercises the 9-arg gl.TexImage2D paths (framebuffer color attachment + a white fallback texture) that abort under WebAssembly full AOT. Renders on Desktop and the WASM interpreter. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_ModelViewer : UserControl
	{
		public GLCanvasElement_ModelViewer()
		{
			this.InitializeComponent();
		}
	}
}
