
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.UI;
using System.Windows.Input;
using Windows.UI.Xaml.Automation;

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	[Sample("TabView", "WinUI")]
	public sealed partial class TabViewSizingPage : Page
	{
		public TabViewSizingPage()
		{
			this.InitializeComponent();
		}

		int _newTabNumber = 1;

		private void SetSmallWidth_Click(object sender, object args)
		{
			LayoutRoot.Width = 500;
		}

		private void SetLargeWidth_Click(object sender, object args)
		{
			LayoutRoot.Width = 800;
		}

		private void GetWidthsButton_Click(object sender, object args)
		{
			WidthEqualText.Text = TabViewEqual.ActualWidth.ToString();
			WidthSizeToContentText.Text = TabViewSizeToContent.ActualWidth.ToString();
		}

		private void Tabview_AddTabButtonClick(TabView sender, object args)
		{
			TabViewItem item = new TabViewItem();
			item.IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Accept };
			item.Header = "New Tab " + _newTabNumber++;
			item.Content = item.Header;

			sender.TabItems.Add(item);
		}

		private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs e)
		{
			sender.TabItems.Remove(e.Tab);
		}
	}
}
