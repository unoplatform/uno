using System;

namespace Windows.Devices.Bluetooth
{
	/// <summary>Indicates the service capabilities of a device.</summary>
	[Flags]
	public enum BluetoothServiceCapabilities : uint
	{
		/// <summary>None.</summary>
		None = 0U,
		/// <summary>Limited Discoverable Mode.</summary>
		LimitedDiscoverableMode = 1U,
		/// <summary>Positioning or location identification.</summary>
		PositioningService = 8U,
		/// <summary>Networking, for example, LAN, Ad hoc.</summary>
		NetworkingService = 16U,
		/// <summary>Rendering, for example, printer, speakers.</summary>
		RenderingService = 32U,
		/// <summary>Capturing, for example, scanner, microphone.</summary>
		CapturingService = 64U,
		/// <summary>Object Transfer, for example, v-Inbox, v-folder.</summary>
		ObjectTransferService = 128U,
		/// <summary>Audio, for example, speaker, microphone, headset service.</summary>
		AudioService = 256U,
		/// <summary>Telephony, for example cordless, modem, headset service.</summary>
		TelephoneService = 512U,
		/// <summary>Information, for example, web server, WAP server.</summary>
		InformationService = 1024U,
	}
}
