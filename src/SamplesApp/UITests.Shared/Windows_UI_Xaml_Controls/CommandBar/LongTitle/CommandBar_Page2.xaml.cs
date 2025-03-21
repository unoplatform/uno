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

#if __IOS__
using UIKit;
using Uno.UI.Controls;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.LongTitle
{
	public sealed partial class CommandBar_Page2 : Page
	{
		public CommandBar_Page2()
		{
			this.InitializeComponent();
		}

		public void OnBackButtonClicked(object sender, object args)
			=> Frame.GoBack();

		public void OnButtonClicked(object sender, RoutedEventArgs e)
			=> Frame.Navigate(typeof(CommandBar_Page3));

		public void OnCalculateSizeClicked(object sender, RoutedEventArgs e)
		{
#if __IOS__
			UIView parent = this;
			while (parent.HasParent())
			{
				parent = parent.Superview;
			}

			var titleView = parent.FindFirstChild<TitleView>();
			var titleViewParent = titleView.Superview;

			//When the error occurs the TitleView Frame.Width get bigger than its parent Frame.Width
			//making the TitleView overlap any AppBarButton of the CommandBar
			Result.Text = titleView.Frame.Width > titleViewParent.Frame.Width ? "FAILED" : "PASSED";
#endif
		}
	}
}
