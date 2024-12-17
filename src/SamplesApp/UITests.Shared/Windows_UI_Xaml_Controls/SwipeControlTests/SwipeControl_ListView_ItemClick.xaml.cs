using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

using SwipeItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItem;
using SwipeItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItemInvokedEventArgs;

namespace UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[Sample("SwipeControl", "ListView")]
	public sealed partial class SwipeControl_ListView_ItemClick : Page
	{
		public SwipeControl_ListView_ItemClick()
		{
			this.InitializeComponent();
		}

		private void SwipeItemInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			LastSwipeInvoked.Text = $"{args.SwipeControl.DataContext}.{sender.Text}";
		}

		private void ItemClicked(object sender, ItemClickEventArgs e)
		{
			LastClicked.Text = e.ClickedItem?.ToString() ?? "--null--";
		}

		private void ItemSelected(object sender, SelectionChangedEventArgs e)
		{
			LastSelected.Text = e.AddedItems.Cast<object>().FirstOrDefault()?.ToString() ?? "--null--";
		}
	}
}
