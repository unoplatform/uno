#nullable enable

using System;
using Windows.Foundation;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
	{
		public SimpleHapticsController SimpleHapticsController { get; } = new SimpleHapticsController();

		public static IAsyncOperation<VibrationAccessStatus> RequestAccessAsync() =>
			RequestAccessTaskAsync().AsAsyncOperation();

		public static IAsyncOperation<VibrationDevice?> GetDefaultAsync() =>
			GetDefaultTaskAsync().AsAsyncOperation();
	}
}
