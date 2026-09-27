using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Shapes.PathTestsControl
{
	[Sample("Path", Name = "Path_GeometryTransform", Description = "Verifies Geometry.Transform rendering — each row applies a different transform directly to the Geometry (not the Path element). Skia-specific feature (#3238).")]
	public sealed partial class Path_GeometryTransform : UserControl
	{
		public Path_GeometryTransform()
		{
			this.InitializeComponent();
		}
	}
}
