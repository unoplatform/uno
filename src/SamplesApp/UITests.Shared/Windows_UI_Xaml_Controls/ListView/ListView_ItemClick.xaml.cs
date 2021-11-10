using Uno.Extensions;
using Uno.UI.Samples.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Windows.UI.Xaml.Controls;
using Uno.Foundation.Logging;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_ItemClick", typeof(ListViewViewModel))]
	public sealed partial class ListView_ItemClick : UserControl
	{
#pragma warning disable CS0109
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#pragma warning restore CS0109

		public ListView_ItemClick()
		{
			this.InitializeComponent();
		}

		private void ListView_ItemClick_1(object sender, ItemClickEventArgs e)
		{
			ItemClickedTextBlock.Text = e.ClickedItem.ToString();
			_log.Warn($"{e.ClickedItem.ToString()} was clicked.");
		}
	}
}
