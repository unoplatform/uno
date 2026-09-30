using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample shows a 2x2 grid alternating each second between ETC2 compressed textures (a red 2D texture with a green center and a cycling blue/yellow array layer) and framebuffer copies of two spinning triangles (glCopyTexImage2D / glCopyTexSubImage3D / glFramebufferTextureLayer). Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_CompressedCopy : UserControl
	{
		public GLCanvasElement_CompressedCopy()
		{
			this.InitializeComponent();
		}
	}
}
