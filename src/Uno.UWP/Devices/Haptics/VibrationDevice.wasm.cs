#nullable enable

using System.Threading.Tasks;
using PhoneVibrationDevice = Windows.Phone.Devices.Notification.VibrationDevice;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
	{
		private static Task<VibrationAccessStatus> RequestAccessTaskAsync() =>
			Task.FromResult(VibrationAccessStatus.Allowed);

		private static Task<VibrationDevice?> GetDefaultTaskAsync()
		{
			if (PhoneVibrationDevice.GetDefault() != null)
			{
				return Task.FromResult<VibrationDevice?>(new VibrationDevice());
			}
			return Task.FromResult<VibrationDevice?>(null);
		}
	}
}
