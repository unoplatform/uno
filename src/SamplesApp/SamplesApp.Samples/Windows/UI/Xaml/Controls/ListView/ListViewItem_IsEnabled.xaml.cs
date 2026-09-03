using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView")]
	public sealed partial class ListViewItem_IsEnabled : Page
	{
		public ListViewItem_IsEnabled()
		{
			this.InitializeComponent();
		}

		public void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			LastClickedButtonTextBlock.Text = button.Name;
		}

		private void ListItemPointerEntered(object sender, PointerRoutedEventArgs e)
		{

		}
	}

	public partial class CustomListViewItem : ListViewItem
	{
		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
		}
	}
}
