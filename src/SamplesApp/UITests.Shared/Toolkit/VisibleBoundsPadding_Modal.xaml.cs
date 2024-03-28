using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
