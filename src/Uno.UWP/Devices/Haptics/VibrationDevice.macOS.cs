#nullable enable

using System.Threading.Tasks;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
    {
		private static Task<VibrationAccessStatus> RequestAccessTaskAsync() =>
			Task.FromResult(VibrationAccessStatus.Allowed);

		private static Task<VibrationDevice?> GetDefaultTaskAsync() =>
			Task.FromResult<VibrationDevice?>(new VibrationDevice());
	}
}
