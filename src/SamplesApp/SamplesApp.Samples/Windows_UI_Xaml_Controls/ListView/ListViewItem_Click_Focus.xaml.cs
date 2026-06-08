using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView")]
	public sealed partial class ListViewItem_Click_Focus : Page
	{
		public ListViewItem_Click_Focus()
		{
			this.InitializeComponent();
			ClearButton.Click += (s, e) => OutputTextBlock.Text = "?";
			TestListViewItem.GettingFocus += (s, e) => OutputTextBlock.Text = "F";
		}
	}
}
