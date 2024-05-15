using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Windows.System.Power;

partial class PowerManager
{
	/// <summary>
	/// Initializes the PowerManager.
	/// </summary>
	/// <returns>A value indicating whether the initialization succeeded.</returns>
	public static Task<bool> InitializeAsync()
	{
#if __WASM__
		return InitializePlatformAsync();
#elif __ANDROID__ || __IOS__
		return Task.FromResult(true);
#else
		if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
		{
			typeof(PowerManager).Log().LogError("PowerManager is not implemented on this platform");
		}

		return Task.FromResult(false);
#endif
	}
}
