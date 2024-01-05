using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		private static readonly char[] _bracesArray = new[] { '{', '}' };

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
		internal static bool TryParse(string deviceId, [NotNullWhen(true)] out DeviceIdentifier? deviceIdentifier)
		{
			deviceIdentifier = null;

			// serialized identifier must have exactly two parts
			// This is equivalent to deviceId.Split('#') then checking that the resulting array is of Length == 2.
			// But it's more efficient to get the first and last indices of '#' and make sure they are the same.
			var firstIndexOfHash = deviceId.IndexOf('#');
			if (firstIndexOfHash <= 0 || firstIndexOfHash == deviceId.Length - 1)
			{
				return false;
			}

			var lastIndexOfHash = deviceId.LastIndexOf('#');
			if (firstIndexOfHash != lastIndexOfHash)
			{
				return false;
			}

			// Uri.UnescapeDataString doesn't have a ReadOnlySpan<char> overload. So we have to substring.
			var firstPart = deviceId.Substring(0, firstIndexOfHash);
			var secondPart = deviceId.AsSpan().Slice(firstIndexOfHash + 1);

			var id = Uri.UnescapeDataString(firstPart);
			var deviceClassSpan = secondPart.Trim(_bracesArray);

			// second part must be valid GUID
			if (!Guid.TryParse(deviceClassSpan, out var deviceClassGuid))
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
