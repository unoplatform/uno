using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Bluetooth
{
	public partial class BluetoothDevice : global::System.IDisposable
	{
		private static string _deviceSelectorPrefix = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND ";
		private static string _deviceSelectorIssueInquiry = "System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean";

		public static string GetDeviceSelector()
		{
			return _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
		}

		public static string GetDeviceSelectorFromPairingState(bool pairingState)
		{
			if (pairingState)
			{
				return _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
			}
			else
			{
				return _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#False OR " + _deviceSelectorIssueInquiry + "#True)";
			}
		}

		public static string GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus connectionStatus)
		{
			if (connectionStatus == BluetoothConnectionStatus.Connected)
			{
				return _deviceSelectorPrefix + "(System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
			}
			else
			{
				return _deviceSelectorPrefix + "(System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#False OR " + _deviceSelectorIssueInquiry + "#True)";
			}
		}

		public static string GetDeviceSelectorFromDeviceName(string deviceName)
		{
			return _deviceSelectorPrefix + "(System.ItemNameDisplay:=\"" + deviceName + "\" OR " + _deviceSelectorIssueInquiry + "#True)";
		}

		public static string GetDeviceSelectorFromBluetoothAddress(ulong bluetoothAddress)
		{
			string macAddr = string.Format("{0:x12}", bluetoothAddress);
			return _deviceSelectorPrefix + "(System.DeviceInterface.Bluetooth.DeviceAddress:=\"" + macAddr + "\" OR " + _deviceSelectorIssueInquiry + "#True)";
		}

		//method public static string GetDeviceSelectorFromClassOfDevice( BluetoothClassOfDevice classOfDevice)
		//{
		// missing "AND" looks like:
		// AND System.Devices.Aep.Bluetooth.Cod.Services.Information:=System.StructuredQueryType.Boolean#True)
		// so, somehow it should iterate all ServiceCapabilities, and use bits from it to construct this part of query
		//	_deviceSelectorPrefix +
		//		"((System.Devices.Aep.Bluetooth.Cod.Major:=" + classOfDevice.MajorClass +
		//		"AND System.Devices.Aep.Bluetooth.Cod.Minor:=" + classOfDevice.MinorClass +
		//		"AND  " +
		//		") OR " + _deviceSelectorIssueInquiry + "#True)";
		//}

	}
}
