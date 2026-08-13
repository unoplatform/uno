using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml_Media.Geometry
{
	[Sample("Geometry", Name = "Geometry_GeometryGroup_FillRule_Showcase", Description = "FillRule.EvenOdd vs Nonzero on self-intersecting and nested paths.", IgnoreInSnapshotTests = true)]
	public sealed partial class Geometry_GeometryGroup_FillRule_Showcase : Page
	{
		public Geometry_GeometryGroup_FillRule_Showcase()
		{
			this.InitializeComponent();
		}
	}
}
