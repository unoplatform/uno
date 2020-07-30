using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
	/// <summary>
	/// Represents a collection of DeviceInformation objects.
	/// </summary>
	public partial class DeviceInformationCollection : IReadOnlyList<DeviceInformation>, IEnumerable<DeviceInformation>
	{
		private readonly List<DeviceInformation> _devices;

		internal DeviceInformationCollection(IEnumerable<DeviceInformation> devices)
		{
			if (devices is null)
			{
				throw new ArgumentNullException(nameof(devices));
			}

			_devices = new List<DeviceInformation>(devices);
		}

		/// <summary>
		/// The number of DeviceInformation objects in the collection.
		/// </summary>
		public uint Size => (uint)_devices.Count;

		/// <summary>
		/// Gets the DeviceInformation object at the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <returns>Device information.</returns>
		public DeviceInformation this[int index]
		{
			get => _devices[index];
			set => throw new NotSupportedException();
		}

		/// <inheritdoc />
		public IEnumerator<DeviceInformation> GetEnumerator() => _devices.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => _devices.GetEnumerator();

		/// <summary>
		/// The number of DeviceInformation objects in the collection.
		/// </summary>
		public int Count
		{
			get => _devices.Count;
			set
			{
				throw new NotSupportedException();
			}
		}
	}
}
