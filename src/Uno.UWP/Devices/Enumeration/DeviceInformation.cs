using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Enumeration
{
	/// <summary>
	/// Represents a device. This class allows access to well-known
	/// device properties as well as additional properties specified
	/// during device enumeration.
	/// </summary>
	public partial class DeviceInformation
	{
		internal DeviceInformation(DeviceIdentifier deviceIdentifier)
			: this(deviceIdentifier, new Dictionary<string, object>())
		{
		}

		internal DeviceInformation(DeviceIdentifier deviceIdentifier, Dictionary<string, object> properties)
		{
			if (deviceIdentifier is null)
			{
				throw new ArgumentNullException(nameof(deviceIdentifier));
			}

			Id = deviceIdentifier.ToString();
			Properties = properties;
		}

		/// <summary>
		/// A string representing the identity of the device.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Indicates whether this device is the default device for the class.
		/// </summary>
		public bool IsDefault { get; internal set; }

		/// <summary>
		/// Indicates whether this device is enabled.
		/// </summary>
		public bool IsEnabled { get; internal set; } = true;

		/// <summary>
		/// The name of the device. This name is in the best
		/// available language for the app.
		/// </summary>
		public string? Name { get; internal set; }

		/// <summary>
		/// Property store containing well-known values as well as additional
		/// properties that can be specified during device enumeration.
		/// </summary>
		public IReadOnlyDictionary<string, object> Properties { get; internal set; }
	}
}
