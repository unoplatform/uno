#nullable enable

using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
#nullable enable

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
	{
		private static IVibrationDeviceExtension? _vibrationDeviceExtension;

		private static Task<VibrationAccessStatus> RequestAccessTaskAsync()
		{
			EnsureExtensionInitialized();

			return Task.FromResult(_vibrationDeviceExtension?.AccessStatus ?? VibrationAccessStatus.Allowed);
		}

		private static Task<VibrationDevice?> GetDefaultTaskAsync()
		{
			EnsureExtensionInitialized();

			if (_vibrationDeviceExtension?.IsAvailable == true)
			{
				return Task.FromResult<VibrationDevice?>(new VibrationDevice());
			}

			return Task.FromResult<VibrationDevice?>(null);
		}

		private static void EnsureExtensionInitialized()
		{
			if (_vibrationDeviceExtension == null)
			{
				ApiExtensibility.CreateInstance(typeof(VibrationDevice), out _vibrationDeviceExtension);
			}
		}
	}

	internal interface IVibrationDeviceExtension
	{
		VibrationAccessStatus AccessStatus { get; }

		bool IsAvailable { get; }
	}
}
