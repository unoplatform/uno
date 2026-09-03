#if !(__IOS__ || __ANDROID__)

#nullable enable

namespace Windows.Devices.Sensors;

public partial class SimpleOrientationSensor
{
	private static partial SimpleOrientationSensor? TryCreateInstance() => null;
}
#endif
