using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", IgnoreInSnapshotTests = true, IsManualTest = true, Description = "Self-checking sample: sets every uniform variant (all int/uint/float scalar, vector, array and matrix forms), reads them back, and walks the introspection and getter surface (GetActiveAttrib/Uniform, uniform blocks, the glIs* family, parameter getters). A pulsing green quad means every check passed; a failure throws in Init and renders nothing. Hardware acceleration required.")]
	public sealed partial class GLCanvasElement_UniformZoo : UserControl
	{
		public GLCanvasElement_UniformZoo()
		{
			this.InitializeComponent();
		}
	}
}
