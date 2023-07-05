#nullable disable

#if __MACOS__ || IS_UNIT_TESTS
namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private Accelerometer()
		{
		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static Accelerometer GetDefault() => null;
	}
}
#endif
