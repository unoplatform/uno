using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "This sample shows a cycling 4-layer checkerboard array texture on the left (the sampler object's mip filtering alternates between nearest and trilinear) and a continuously-scrolling rainbow texture updated via glTexSubImage2D on the right. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_TextureArray : UserControl
	{
		public GLCanvasElement_TextureArray()
		{
			this.InitializeComponent();
		}
	}
}
