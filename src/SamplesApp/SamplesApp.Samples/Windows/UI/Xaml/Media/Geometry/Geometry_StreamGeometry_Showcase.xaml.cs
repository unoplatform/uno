using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml_Media.Geometry
{
	[Sample("Geometry", Name = "Geometry_StreamGeometry_Showcase", Description = "Same shape built via StreamGeometry mini-language vs explicit PathGeometry, side-by-side.", IgnoreInSnapshotTests = true)]
	public sealed partial class Geometry_StreamGeometry_Showcase : Page
	{
		public Geometry_StreamGeometry_Showcase()
		{
			this.InitializeComponent();
		}
	}
}
