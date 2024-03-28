using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfoAttribute("ListView", "RotatedListView_WithRotatedItems", typeof(RotatedListViewViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class RotatedListView_WithRotatedItems : UserControl
	{
		public RotatedListView_WithRotatedItems()
		{
			this.InitializeComponent();
		}
	}
}
