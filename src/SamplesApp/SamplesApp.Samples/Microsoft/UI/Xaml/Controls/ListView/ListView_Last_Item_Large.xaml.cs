using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_Last_Item_Large")]
	public sealed partial class ListView_Last_Item_Large : UserControl
	{
		public ListView_Last_Item_Large()
		{
			this.InitializeComponent();

			MyListView.ItemsSource = Enumerable
				.Range(0, 50)
				.Select(x => x.ToString())
				.Concat(new[] { "9999999999999999999999999999999999999999999999999999999999999" });
		}
	}
}
