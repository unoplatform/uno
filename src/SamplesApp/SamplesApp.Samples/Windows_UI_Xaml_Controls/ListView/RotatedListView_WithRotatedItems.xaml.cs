using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "RotatedListView_WithRotatedItems", ViewModelType = typeof(RotatedListViewViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class RotatedListView_WithRotatedItems : UserControl
	{
		public RotatedListView_WithRotatedItems()
		{
			this.InitializeComponent();
		}
	}
}
