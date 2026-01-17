using UITests.Shared.Windows_UI_Xaml_Controls.GridTestsControl;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.GridTestsControl
{
	[Sample("Grid", "GridDataboundGridColumn", typeof(GridTestsViewModel), IgnoreInSnapshotTests: true)]
	public sealed partial class GridDataboundGridColumn : UserControl
	{
		public GridDataboundGridColumn()
		{
			this.InitializeComponent();
		}
	}
}
