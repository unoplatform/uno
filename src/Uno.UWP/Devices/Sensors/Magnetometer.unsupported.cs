#if __MACOS__ || NET461
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
