#nullable enable

using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using AndroidX.Core.Content;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.Devices.Haptics
{
	public partial class VibrationDevice
	{
		private const string VibratePermission = "android.permission.VIBRATE";

		private static Task<VibrationAccessStatus> RequestAccessTaskAsync()
		{
			if (ContextCompat.CheckSelfPermission(Application.Context, VibratePermission) == Permission.Denied)
			{
				if (typeof(VibrationDevice).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(VibrationDevice).Log().LogWarning($"Permission '{VibratePermission}' must be declared " +
						$"for the application to use {nameof(VibrationDevice)}.");
				}
				return Task.FromResult(VibrationAccessStatus.DeniedBySystem);
			}
			return Task.FromResult(VibrationAccessStatus.Allowed);
		}

		private static Task<VibrationDevice?> GetDefaultTaskAsync() =>
			Task.FromResult<VibrationDevice?>(new VibrationDevice());
	}
}
