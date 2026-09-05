using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "A rotating cubemap skybox - six colored faces sampled via samplerCube. Exercises TEXTURE_CUBE_MAP target with six gl.TexImage2D uploads.")]
	public sealed partial class GLCanvasElement_CubeMap : UserControl
	{
		public GLCanvasElement_CubeMap()
		{
			this.InitializeComponent();
		}
	}
}
