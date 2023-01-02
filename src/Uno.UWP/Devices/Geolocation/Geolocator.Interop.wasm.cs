#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Geolocation
{
	internal static partial class Geolocator
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Devices.Geolocation.Geolocator.getGeoposition")]
			internal static partial void GetGeoposition(double actualDesiredAccuracyInMeters, double maximumAge, double timeout, string requestId);

			[JSImport("globalThis.Windows.Devices.Geolocation.Geolocator.requestAccess")]
			internal static partial void RequestAccess();

			[JSImport("globalThis.Windows.Devices.Geolocation.Geolocator.startPositionWatch")]
			internal static partial void StartPositionWatch(double actualDesiredAccuracyInMeters, string requestId);

			[JSImport("globalThis.Windows.Devices.Geolocation.Geolocator.stopPositionWatch")]
			internal static partial void StopPositionWatch(double actualDesiredAccuracyInMeters, string requestId);
		}
	}
}
#endif
