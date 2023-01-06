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

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.BackButtonTitle
{
	public sealed partial class CommandBar_Page2 : Page
	{
		public CommandBar_Page2()
		{
			this.InitializeComponent();
		}

		public void OnButtonClicked(object sender, object args)
		{

#if __IOS__
			UIView parent = this;
			while (parent.HasParent())
			{
				parent = parent.Superview;
			}

			var navigationBar = parent.FindFirstChild<UnoNavigationBar>();

			var uiLabels = navigationBar?.FindSubviewsOfType<UILabel>();

			var result = uiLabels?.Any(x => x.Text == "Back") ?? true;

			InfoTextBlock.Text = result ? "FAILED" : "PASSED";
#endif
		}
	}
}
