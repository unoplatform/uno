using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.GridTestsControl
{
	[Sample(Description = "Grid with MinWidth and MinHeight and VerticalAlignment Top and HorizontalAlignment Center. Star Children should at least take the min size.")]
	public sealed partial class Grid_with_MinWidth_MinHeight : UserControl
	{
		public Grid_with_MinWidth_MinHeight()
		{
			this.InitializeComponent();
		}
	}
}
