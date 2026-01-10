using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

#if __APPLE_UIKIT__
using UIKit;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml_Input.Keyboard
{
	[Sample("Keyboard", "Keyboard_Clickthrough_Modal", null, true, "Currently only implemented for iOS")]
	public sealed partial class Keyboard_Clickthrough_Modal : Page
	{
		public Keyboard_Clickthrough_Modal()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
#if __APPLE_UIKIT__
			var alert = UIAlertController.Create("This is a title", "This is a message", UIAlertControllerStyle.Alert);
			alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));
			alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
			alert.AddTextField((tf) =>
			{
				tf.Placeholder = "placeholder";
			});
			UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);
#endif
		}
	}
}
