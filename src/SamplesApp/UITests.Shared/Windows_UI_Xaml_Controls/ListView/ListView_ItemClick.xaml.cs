using Uno.Extensions;
using Uno.UI.Samples.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Windows.UI.Xaml.Controls;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_ItemClick", typeof(ListViewViewModel))]
	public sealed partial class ListView_ItemClick : UserControl
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#endif
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
