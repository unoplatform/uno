#nullable enable

using System;
using Uno.Foundation;

namespace Windows.System.Power;

public partial class PowerManager
{
	private const string JsType = "Windows.System.Power.PowerManager";

	private static BatteryStatus GetBatteryStatus()
	{
		WebAssemblyRuntime.InvokeAsync($"{JsType}.initializeAsync()");
		var batteryStatusString = WebAssemblyRuntime.InvokeJS($"{JsType}.getBatteryStatus()");
		if (Enum.TryParse<BatteryStatus>(batteryStatusString, out var batteryStatus))
		{
			return batteryStatus;
		}
		return BatteryStatus.NotPresent;
	}
}
