#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	public sealed partial class TabViewTabClosingBehaviorPage : Page
	{
		private int _newTabNumber = 0;

		public TabViewTabClosingBehaviorPage()
		{
			this.InitializeComponent();
		}

		public void AddButtonClick(object sender, object e)
		{
			if (Tabs != null)
			{
				TabViewItem item = new TabViewItem();
				item.Header = "New Tab " + _newTabNumber;
				item.Content = item.Header;

				Tabs.TabItems.Add(item);

				_newTabNumber++;
			}
		}

		private void TabViewTabCloseRequested(object sender, Microsoft/* UWP don't rename */.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs e)
		{
			Tabs.TabItems.Remove(e.Tab);

			TabViewWidth.Text = Tabs.ActualWidth.ToString();

			var scrollButtonStateValue = "";

			var scrollIncreaseButton = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollIncreaseButton") as RepeatButton;
			var scrollDecreaseButton = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollDecreaseButton") as RepeatButton;

			scrollButtonStateValue += scrollIncreaseButton.IsEnabled + ";";
			scrollButtonStateValue += scrollDecreaseButton.IsEnabled + ";";

			ScrollButtonStatus.Text = scrollButtonStateValue;
		}

		public void GetActualWidthsButton_Click(object sender, RoutedEventArgs e)
		{
			// This is the smallest width that fits our content without any scrolling.
			TabViewWidth.Text = Tabs.ActualWidth.ToString();
		}

	}
}
