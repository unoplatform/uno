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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Extensions
{
	[SampleControlInfo("Windows_Extensions", "PermissionsHelperAndroid", description: "Checks if system dialog asking for permission is displayed")]
	public sealed partial class PermissionsHelperAndroid : UserControl
	{
		public PermissionsHelperAndroid()
		{
			this.InitializeComponent();
		}

		private async void TryPermission_Click(object sender, RoutedEventArgs e)
		{

#if __ANDROID__
			string[] testPermissions = { Android.Manifest.Permission.AccessFineLocation };
			string errorMsg = "";


			// first check, mainly if Manifest declares AccessFineLocation
			// at this time, Exception is unexpected (no 'throw' in called method)
			try
			{
				var missingPermissions = Windows.Extensions.PermissionsHelper.MissingPermissions(testPermissions, null);
				if (missingPermissions is null)
				{
					errorMsg = "MissingPermissions returned NULL, check if app manifest includes AccessFineLocation";
				}
			}
			catch (Exception ex)
			{
				errorMsg = "Should not happen: " + ex.Message;
			}

			if (!string.IsNullOrEmpty(errorMsg))
			{
				ErrorMessage.Text = errorMsg;
				return;
			}

			// second check: is dialog displayed
			// at this time, Exception is unexpected (no 'throw' in called method)
			try
			{
				ErrorMessage.Text = "you should see system dialog asking for permission now";

				bool granted = await Windows.Extensions.PermissionsHelper.TryAskPermissionAsync(testPermissions, null);
				if (!granted)
				{
					errorMsg = "permission is not granted?";
				}
				else
				{
					errorMsg = "";
				}
			}
			catch (Exception ex)
			{
				errorMsg = "Should not happen: " + ex.Message;
			}

			ErrorMessage.Text = errorMsg;

#else
			ErrorMessage.Text = "this is only for Android";
#endif


		}

	}
}

