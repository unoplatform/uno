using System;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Logging;

namespace Windows.System.Power;

partial class PowerManager
{
	/// <summary>
	/// Initializes the PowerManager.
	/// </summary>
	/// <returns>A value indicating whether the initialization succeeded.</returns>
	public static async Task<bool> InitializeAsync()
	{
#if __WASM__
		try
		{
			await WebAssemblyRuntime.InvokeAsync($"{JsType}.initializeAsync()");
			_isInitialized = true;
			return true;
		}
		catch (Exception ex)
		{
			if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
			{
				typeof(PowerManager).Log().LogError("Could not initialize PowerManager", ex);
			}
			return false;
		}
#else
		if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
		{
			typeof(PowerManager).Log().LogError("PowerManager is not implemented on this platform", ex);
		}

		return false;
#endif
	}
}
