#nullable enable

using System.Threading.Tasks;
using UIKit;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
    {
		private static Task<VibrationAccessStatus> RequestAccessTaskAsync() =>
			Task.FromResult(VibrationAccessStatus.Allowed);

		private static Task<VibrationDevice?> GetDefaultTaskAsync()
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
			{
				return Task.FromResult<VibrationDevice?>(new VibrationDevice());
			}
			return Task.FromResult<VibrationDevice?>(null);
		}
	}
}
