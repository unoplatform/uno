#if !(__IOS__ || __ANDROID__ || __WASM__)
#nullable enable

namespace Windows.Devices.Sensors
{
	public partial class Gyrometer
	{
		private Gyrometer()
		{
		}

		/// <summary>
		/// API not supported, always returns null.
		/// </summary>
		/// <returns>Null.</returns>
		public static Gyrometer? GetDefault() => null;
	}
}
#endif
