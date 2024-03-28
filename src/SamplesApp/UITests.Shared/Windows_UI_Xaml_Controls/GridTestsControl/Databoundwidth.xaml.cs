using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using UITests.Shared.Windows_UI_Xaml_Controls.GridTestsControl;

namespace Uno.UI.Samples.Content.UITests.GridTestsControl
{
	[SampleControlInfo("Grid", "Databoundwidth", typeof(GridTestsViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class Databoundwidth : UserControl
	{
		public Databoundwidth()
		{
			this.InitializeComponent();
		}
	}
}
