using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Enumeration
{
	/// <summary>
	/// Contains updated properties for a DeviceInformation object.
	/// </summary>
	public partial class DeviceInformationUpdate
	{
		internal DeviceInformationUpdate(DeviceIdentifier deviceIdentifier) =>
			Id = deviceIdentifier.ToString();

		/// <summary>
		/// The DeviceInformation ID of the updated device.
		/// </summary>
		public string Id { get; }
	}
}
