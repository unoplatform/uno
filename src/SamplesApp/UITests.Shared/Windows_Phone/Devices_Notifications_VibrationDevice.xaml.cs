using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Devices.Haptics;
using Windows.Phone.Devices.Notification;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Phone
{
	[SampleControlInfo("Windows.Phone", "Devices.Notifications.VibrationDevice", description: "Verifies the Windows.Phone.Devices.Notifications.VibrationDevice API")]
	public sealed partial class Devices_Notifications_VibrationDevice : UserControl
	{
		private VibrationDevice _vibrationDevice;

		public Devices_Notifications_VibrationDevice()
		{
			this.InitializeComponent();
			if (ApiInformation.IsTypePresent("Windows.Phone.Devices.Notification.VibrationDevice, Uno"))
			{
				try
				{
					_vibrationDevice = VibrationDevice.GetDefault();
					if (_vibrationDevice == null)
					{
						ErrorMessage.Text = "VibrationDevice not available";
					}
				}
				catch (Exception ex)
				{
					ErrorMessage.Text = ex.Message;
				}
			}
			else
			{
				ErrorMessage.Text = "VibrationDevice API not present";
			}
		}

		private void Vibrate_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(VibrationDurationTextBox.Text, out int milliseconds))
			{
				try
				{
					_vibrationDevice?.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
				}
				catch (Exception ex)
				{
					ErrorMessage.Text = ex.Message;
				}
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(VibrationDurationTextBox.Text, out int milliseconds))
			{
				try
				{
					_vibrationDevice?.Cancel();
				}
				catch (Exception ex)
				{
					ErrorMessage.Text = ex.Message;
				}
			}
		}
	}
}
