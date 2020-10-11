#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__

using System.Threading.Tasks;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
    {
		private static Task<VibrationAccessStatus> RequestAccessTaskAsync() =>
			Task.FromResult(VibrationAccessStatus.Allowed);

		private static Task<VibrationDevice> GetDefaultTaskAsync() =>
			Task.FromResult<VibrationDevice>(null);
	}
}
#endif
