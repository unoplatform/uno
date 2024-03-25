#nullable enable

using Windows.Devices.Enumeration;
using Windows.Security.Cryptography.Certificates;
using Windows.System;

namespace Windows.UI.Core
{
	public partial class KeyEventArgs : ICoreWindowEventArgs
	{
		internal KeyEventArgs(string deviceId, VirtualKey virtualKey, VirtualKeyModifiers modifiers, CorePhysicalKeyStatus keyStatus, char? unicodeKey = null)
		{
			DeviceId = deviceId;
			VirtualKey = virtualKey;
			KeyboardModifiers = modifiers;
			KeyStatus = keyStatus;
			UnicodeKey = unicodeKey;
		}

		public bool Handled { get; set; }

		public CorePhysicalKeyStatus KeyStatus { get; }

		public VirtualKey VirtualKey { get; }

		public string DeviceId { get; }

		internal VirtualKeyModifiers KeyboardModifiers { get; }

		/// <summary>
		/// This gets the Unicode Key associated with the event. This is not limited to the
		/// VirtualKey options. Currently, this is only implemented for skia.
		/// </summary>
		internal char? UnicodeKey { get; }
	}
}
