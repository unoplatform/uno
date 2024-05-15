#if !(__ANDROID__ || __IOS__)
#nullable enable

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
		public static Barometer? GetDefault() => null;
	}
}
#endif
