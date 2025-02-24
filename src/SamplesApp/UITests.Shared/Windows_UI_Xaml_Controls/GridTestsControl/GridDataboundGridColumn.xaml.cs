using UITests.Shared.Windows_UI_Xaml_Controls.GridTestsControl;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.GridTestsControl
{
	[SampleControlInfo("Grid", "GridDataboundGridColumn", typeof(GridTestsViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class GridDataboundGridColumn : UserControl
	{
		public GridDataboundGridColumn()
		{
			this.InitializeComponent();
		}
	}
}
