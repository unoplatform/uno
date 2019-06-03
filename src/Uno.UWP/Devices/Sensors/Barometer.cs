#if __ANDROID__ || __IOS__

using Windows.Foundation;

namespace Windows.Devices.Sensors
{
	public partial class Barometer
	{
		private static bool _initializationAttempted = false;
		private static Barometer _instance = null;

		/// <summary>
		/// Hides the public parameterless constructor
		/// </summary>
		private Barometer()
		{

		}

		public static Barometer GetDefault()
		{
			if (_instance == null && !_initializationAttempted)
			{
				_instance = TryCreateInstance();
				_initializationAttempted = true;
			}
			return _instance;
		}
	}
}
#endif
