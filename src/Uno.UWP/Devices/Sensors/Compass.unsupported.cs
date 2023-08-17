#if __MACOS__ || IS_UNIT_TESTS || __WASM__ || __NETSTD_REFERENCE__
namespace Windows.Devices.Sensors;

public class Compass
{
	private Compass()
	{
	}

	/// <summary>
	/// API not supported, always returns null.
	/// </summary>
	/// <returns>Null.</returns>
	public static Compass GetDefault() => null;
}
#endif
