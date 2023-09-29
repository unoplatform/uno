#if !(__IOS__ || __ANDROID__)
#nullable enable

namespace Windows.Devices.Sensors;

public partial class Compass
{
	private Compass()
	{
	}

	/// <summary>
	/// API not supported, always returns null.
	/// </summary>
	/// <returns>Null.</returns>
	public static Compass? GetDefault() => null;
}
#endif
