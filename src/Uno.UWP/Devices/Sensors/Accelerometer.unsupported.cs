#if __MACOS__ || NET461 || __SKIA__ || __NETSTD_REFERENCE__
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
