using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Notifications
{
	[SampleControlInfo("Windows.UI.Notifications")]
	[ActivePlatforms(Platform.Android)]
	public sealed partial class ToastNotification : Page
    {
        public ToastNotification()
        {
            this.InitializeComponent();
        }


		public sealed partial class BadgeNotificationTests : Page
		{
			public BadgeNotificationTests()
			{
				this.InitializeComponent();
			}

			private void sendToast_Click(object sender, RoutedEventArgs e)
			{
				string toastLine1 = toastLine1.Text;
				string toastLine2 = toastLine2.Text;

				if (string.IsNullOrEmpty(toastLine1))
				{
					errorMsg.Text = "First string cannot be empty!";
					return;
				}

				var sXml = "<visual><binding template='ToastGeneric'><text>" + toastLine1;
				if (!string.IsNullOrEmpty(toastLine2))
				{
					sXml = sXml + "</text><text>" + toastLine2;
				}
				sXml = sXml + "</text></binding></visual>";
				var oXml = new Windows.Data.Xml.Dom.XmlDocument();
				oXml.LoadXml("<toast>" + sXml + "</toast>");

				try
				{
					var oToast = new Windows.UI.Notifications.ToastNotification(oXml);
					Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast);
				}
				catch (Exception ex)
				{
					errorMsg.Text = "Catched exception: " + ex.Message;
				}

			}

		}

	}
}
