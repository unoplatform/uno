using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.Logging;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_ItemClick", typeof(ListViewViewModel))]
	public sealed partial class ListView_ItemClick : UserControl
	{
		public ListView_ItemClick()
		{
			this.InitializeComponent();
		}

		private void ListView_ItemClick_1(object sender, ItemClickEventArgs e)
		{
			ItemClickedTextBlock.Text = e.ClickedItem.ToString();
			this.Log().Warn($"{e.ClickedItem.ToString()} was clicked.");
		}
	}
}
