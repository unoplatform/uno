using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Devices.Enumeration.Internal
{
	/// <summary>
	/// Represents full device identifier.
	/// </summary>
	internal class DeviceIdentifier
	{
		/// <summary>
		/// Initializes device identifier.
		/// </summary>
		/// <param name="id">Id of the device.</param>
		/// <param name="deviceClass">Class the device belongs to.</param>
		public DeviceIdentifier(string id, Guid deviceClass)
		{
			Id = id;
			DeviceClass = deviceClass;
		}

		/// <summary>
		/// Gets the device class.
		/// </summary>
		public Guid DeviceClass { get; }

		/// <summary>
		/// Gets the device ID.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Attempts to parse the device class identifier from a string.
		/// </summary>
		/// <param name="deviceId">Device ID.</param>
		/// <param name="deviceIdentifier">Parsed device identifier.</param>
		/// <returns>Value indicating whether the ID can be parsed.</returns>
		internal static bool TryParse(string deviceId, out DeviceIdentifier deviceIdentifier)
		{
			deviceIdentifier = null;
			var parts = deviceId.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

			// serialized identifier must have exactly two parts
			if (parts.Length != 2)
			{
				return false;
			}

			var id = Uri.UnescapeDataString(parts[0]);
			var deviceClassString = parts[1].Trim(new[] { '{', '}' });

			// second part must be valid GUID
			if (!Guid.TryParse(deviceClassString, out var deviceClassGuid))
			{
				return false;
			}

			// everything valid, create identifier
			deviceIdentifier = new DeviceIdentifier(id, deviceClassGuid);
			return true;
		}

		/// <summary>
		/// Serializes the device identifier as a string.
		/// </summary>
		/// <returns>Formatted device identifier.</returns>
		public override string ToString() =>
			$"{Uri.EscapeDataString(Id)}#{{{DeviceClass}}}";
	}
}
