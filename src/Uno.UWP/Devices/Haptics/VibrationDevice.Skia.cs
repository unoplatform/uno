using System.Threading.Tasks;
using Uno.Foundation.Extensibility;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
	{
		private static IVibrationDeviceExtension _vibrationDeviceExtensions;

		private static Task<VibrationAccessStatus> RequestAccessTaskAsync()
		{
			EnsureExtensionInitialized();

			return Task.FromResult(_vibrationDeviceExtensions?.AccessStatus ?? VibrationAccessStatus.Allowed);
		}

		private static Task<VibrationDevice> GetDefaultTaskAsync()
		{
			EnsureExtensionInitialized();

			if (_vibrationDeviceExtensions?.IsAvailable == true)
			{
				return Task.FromResult(new VibrationDevice());
			}

			return Task.FromResult<VibrationDevice>(null);
		}

		private static void EnsureExtensionInitialized()
		{
			if (_vibrationDeviceExtensions == null)
			{
				ApiExtensibility.CreateInstance(typeof(VibrationDevice), out _vibrationDeviceExtensions);
			}
		}
	}

	internal interface IVibrationDeviceExtension
	{
		VibrationAccessStatus AccessStatus { get; }

		bool IsAvailable { get; }
	}
}
