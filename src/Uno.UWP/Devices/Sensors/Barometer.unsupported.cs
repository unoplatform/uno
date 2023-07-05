#nullable disable

#if __MACOS__ || IS_UNIT_TESTS || __WASM__
namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private Barometer()
		{
		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static Barometer GetDefault() => null;
	}
}
#endif
