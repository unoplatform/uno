#if __IOS__ || __ANDROID__
#pragma warning disable 67
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged;


		public PositionAccuracy DesiredAccuracy
		{
			get;
			set;
		}
	}
}
#endif
