using System;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Bluetooth;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "BluetoothSelectors", description: "Demonstrates use of Windows.Devices.Bluetooth*.GetDeviceSelector*", ignoreInSnapshotTests: true)]
	public sealed partial class BluetoothSelectors : UserControl
	{
		public BluetoothSelectors()
		{
			this.InitializeComponent();
		}

		private void uiCreate_Click(object sender, RoutedEventArgs e)
		{
			uiResultMsg.Text = "Creating query...";
			bool bBLE = (uiBTtype.SelectedValue as ComboBoxItem).Content.ToString().Contains("LE");
			switch ((uiQuery.SelectedValue as ComboBoxItem).Content.ToString())
			{
				case "GetDeviceSelector":
					if (bBLE)
					{
						uiResultMsg.Text = BluetoothLEDevice.GetDeviceSelector();
					}
					else
					{
						uiResultMsg.Text = BluetoothDevice.GetDeviceSelector();
					}
					break;
				case "GetDeviceSelectorFromPairingState(true)":
					if (bBLE)
					{
						uiResultMsg.Text = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
					}
					else
					{
						uiResultMsg.Text = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
					}
					break;
				case "GetDeviceSelectorFromPairingState(false)":
					if (bBLE)
					{
						uiResultMsg.Text = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
					}
					else
					{
						uiResultMsg.Text = BluetoothDevice.GetDeviceSelectorFromPairingState(false);
					}
					break;
				case "GetDeviceSelectorFromConnectionStatus(Connected)":
					if (bBLE)
					{
						uiResultMsg.Text = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
					}
					else
					{
						uiResultMsg.Text = BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
					}
					break;
				case "GetDeviceSelectorFromConnectionStatus(Disconnected)":
					if (bBLE)
					{
						uiResultMsg.Text = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected);
					}
					else
					{
						uiResultMsg.Text = BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected);
					}
					break;
				default:
					uiResultMsg.Text = "Unknown value in second ComboBox";
					break;
			}
		}
	}
}
