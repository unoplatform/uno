using System;
using System.Collections.Generic;
using Uno.UI.Samples.Controls;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Security.ExchangeActivesyncProvisioning
{
	[Sample("Windows.Security", Name = nameof(EasClientDeviceInformation), IsManualTest = true)]
	public sealed partial class EasClientDeviceInformationTests : Page
	{
		public EasClientDeviceInformationTests()
		{
			InitializeComponent();

			var easClientDeviceInformation = new EasClientDeviceInformation();

			Properties = new[]{
				new KeyValuePair<string, string>("Id", easClientDeviceInformation.Id.ToString()),
				new KeyValuePair<string, string>("Friendly name", easClientDeviceInformation.FriendlyName),
				new KeyValuePair<string, string>("Operating system", easClientDeviceInformation.OperatingSystem),
				new KeyValuePair<string, string>("System firmeware version", easClientDeviceInformation.SystemFirmwareVersion),
				new KeyValuePair<string, string>("System hardware version", easClientDeviceInformation.SystemHardwareVersion),
				new KeyValuePair<string, string>("System manufacturer", easClientDeviceInformation.SystemManufacturer),
				new KeyValuePair<string, string>("System product name", easClientDeviceInformation.SystemProductName),
				new KeyValuePair<string, string>("System SKU", easClientDeviceInformation.SystemSku)
			};
		}

		public KeyValuePair<string, string>[] Properties { get; }
	}
}
