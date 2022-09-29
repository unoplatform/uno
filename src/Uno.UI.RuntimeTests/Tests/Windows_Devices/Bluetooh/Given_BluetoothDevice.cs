#nullable disable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using System.Linq;
using Windows.Devices.Bluetooth;

namespace Uno.UI.RuntimeTests.Tests
{

	[TestClass]
	public class Given_BluetoothDevice
	{
		readonly private string _deviceSelectorPrefix = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND ";
		readonly private string _deviceSelectorIssueInquiry = "System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean";


		[TestMethod]
		public void When_GetSelector()
		{
			string testSelector;

			testSelector = _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelector());

			testSelector = _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelectorFromPairingState(true));
			testSelector = _deviceSelectorPrefix + "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#False OR " + _deviceSelectorIssueInquiry + "#True)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelectorFromPairingState(false));

			testSelector = _deviceSelectorPrefix + "(System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#True OR " + _deviceSelectorIssueInquiry + "#False)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected));
			testSelector = _deviceSelectorPrefix + "(System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#False OR " + _deviceSelectorIssueInquiry + "#True)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected));

			string deviceName = "TESTNAME";
			testSelector = _deviceSelectorPrefix + "(System.ItemNameDisplay:=\"" + deviceName + "\" OR " + _deviceSelectorIssueInquiry + "#True)";
			Assert.AreEqual(testSelector, BluetoothDevice.GetDeviceSelectorFromDeviceName(deviceName));

		}

	}
}
