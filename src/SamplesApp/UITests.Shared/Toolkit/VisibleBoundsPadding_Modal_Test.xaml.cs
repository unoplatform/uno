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
using Uno.UI.Samples.Controls;

#if __IOS__
using UIKit;
#endif

namespace UITests.Toolkit
{
	[Sample("Toolkit")]
	public partial class VisibleBoundsPadding_Modal_Test : Page
	{
		public VisibleBoundsPadding_Modal_Test()
		{
			this.InitializeComponent();
		}
		private void LaunchModalSample(object sender, RoutedEventArgs e)
		{
#if __IOS__
			var vc = new UIViewController { View = new VisibleBoundsPadding_Modal() };
			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
			{
				// Esnure the behavior of the iPad modal presentation mimics that of the iPhone
				vc.PreferredContentSize = Windows.UI.Xaml.Window.Current.Bounds.ToCGRect().Size;
				vc.ModalPresentationStyle = UIModalPresentationStyle.FormSheet;
			}
			UIApplication.SharedApplication.KeyWindow.RootViewController.PresentModalViewController(vc, true);
#endif
		}
	}
}
