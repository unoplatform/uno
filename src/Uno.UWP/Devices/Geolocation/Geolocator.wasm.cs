#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		public Geolocator()
		{
			
		}

		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{
			
		}
	}
}
#endif
