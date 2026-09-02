using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListViewFirstVisibleIndex", Description = "ListView with FirstVisibleIndex and LastVisibleIndex shown")]
	public sealed partial class ListViewFirstVisibleIndex : UserControl
	{
		public ListViewFirstVisibleIndex()
		{
			this.InitializeComponent();
		}


		private void ListView_Loaded(object sender, RoutedEventArgs e)
		{
			var sv = (sender as ListViewBase).FindFirstChild<global::Microsoft.UI.Xaml.Controls.ScrollViewer>();
			var panel = (sender as ListViewBase).ItemsPanelRoot as ItemsStackPanel;
			sv.ViewChanged += (_, __) =>
			{
				FirstVisibleIndexTextBlock.Text = panel.FirstVisibleIndex.ToString();
				LastVisibleIndexTextBlock.Text = panel.LastVisibleIndex.ToString();
			};
		}
	}
}
