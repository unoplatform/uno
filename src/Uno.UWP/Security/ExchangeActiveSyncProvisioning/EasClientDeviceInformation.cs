#nullable disable

using System;
using Uno;

namespace Windows.Security.ExchangeActiveSyncProvisioning
{
	/// <summary>
	/// Provides the app with the ability to retrieve device information from the local device.
	/// </summary>
	public partial class EasClientDeviceInformation
	{
		/// <summary>
		/// Creates an instance of an object that allows the caller
		/// app to retrieve device information from the local device.
		/// </summary>
		public EasClientDeviceInformation() => Initialize();

		partial void Initialize();

		/// <summary>
		/// Gets the friendly name of the local device.
		/// This value might come from a NetBIOS computer name.
		/// </summary>
		[NotImplemented("__ANDROID__", "__WASM__", "__SKIA__", "__MACOS__")]
		public string FriendlyName { get; private set; } = "";

		/// <summary>
		/// Returns the identifier of the local device.
		/// </summary>
		[NotImplemented("__ANDROID__", "__IOS__", "__WASM__", "__SKIA__", "__MACOS__")]
		public Guid Id { get; private set; } = Guid.Empty;

		/// <summary>
		/// Gets the name of the operating system of the local device.
		/// </summary>
		public string OperatingSystem { get; private set; } = "";

		/// <summary>
		/// Gets the system manufacturer of the local device.
		/// Use SystemManufacturer only if the value of SystemSku is empty.
		/// </summary>
		public string SystemManufacturer { get; private set; } = "System manufacturer";

		/// <summary>
		/// Gets the system product name of the local device.
		/// Use SystemProductName only if the value of SystemSku is empty.
		/// </summary>
		public string SystemProductName { get; private set; } = "System Product Name";

		/// <summary>
		/// Gets the system SKU of the local device.
		/// </summary>
		public string SystemSku { get; private set; } = "SKU";

		/// <summary>
		/// Gets the system firmware version of the local device.
		/// </summary>
		public string SystemFirmwareVersion { get; private set; } = "";

		/// <summary>
		/// Gets the system hardware version of the local device.
		/// </summary>
		public string SystemHardwareVersion { get; private set; } = "";
	}
}
