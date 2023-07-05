#nullable disable

#if __MACOS__ || IS_UNIT_TESTS
namespace Windows.Devices.Sensors
{
	public partial class Magnetometer
	{
		private Magnetometer()
		{
		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static Magnetometer GetDefault() => null;
	}
}
#endif
