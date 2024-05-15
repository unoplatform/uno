using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
#if __IOS__
using UIKit;
#endif

namespace UITests.Toolkit
{
	public partial class VisibleBoundsPadding_Modal : Page
	{
		public VisibleBoundsPadding_Modal()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			LayoutRectangle.Visibility = LayoutRectangle.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
		}

		private void CloseModalClick(object sender, RoutedEventArgs e)
		{
#if __IOS__
			UIApplication.SharedApplication.KeyWindow.RootViewController.DismissModalViewController(animated: false);
#endif
		}
	}
}
