using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.Devices.Enumeration
{
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

		public uint Size => (uint)_devices.Count;

		public DeviceInformation this[int index]
		{
			get => _devices[index];
			set => throw new global::System.NotSupportedException();
		}

		public IEnumerator<DeviceInformation> GetEnumerator() => _devices.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _devices.GetEnumerator();

		public int Count
		{
			get => _devices.Count;
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
	}
}
