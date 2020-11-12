#nullable enable

using Windows.Devices.Enumeration;
using Windows.Security.Cryptography.Certificates;
using Windows.System;

namespace Windows.UI.Core
{
	public partial class KeyEventArgs : ICoreWindowEventArgs
	{
		internal KeyEventArgs(string deviceId, VirtualKey virtualKey, CorePhysicalKeyStatus keyStatus)
		{
			DeviceId = deviceId;
			VirtualKey = virtualKey;
			KeyStatus = keyStatus;
		}

		public bool Handled { get; set; }

		public CorePhysicalKeyStatus KeyStatus { get; }

		public VirtualKey VirtualKey { get; }

		public string DeviceId { get; }
	}
}
