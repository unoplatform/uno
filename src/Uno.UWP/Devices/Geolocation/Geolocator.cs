#if __IOS__ || __ANDROID__ || __WASM__
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
		private PositionAccuracy _desiredAccuracy = PositionAccuracy.Default;
		private uint _desiredAccuracyInMeters;

		private uint _actualDesiredAccuracyInMeters = 500u;

		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged;

		/// <summary>
		/// Setting overwrites <see cref="_actualDesiredAccuracyInMeters"/> but does not overwrite <see cref="DesiredAccuracyInMeters"/> directly.
		/// Default is equivalent to 500 meters, High is equivalent to 10 meters
		/// Matches UWP behavior <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.geolocation.geolocator.desiredaccuracy#remarks">Docs</see> 
		/// </summary>
		public PositionAccuracy DesiredAccuracy
		{
			get => _desiredAccuracy;
			set
			{
				_desiredAccuracy = value;
				_actualDesiredAccuracyInMeters = value == PositionAccuracy.Default ? 500u : 10u;
			}
		}

		/// <summary>
		/// Setting overwrites <see cref="_actualDesiredAccuracyInMeters"/> but does not overwrite <see cref="DesiredAccuracy"/> directly.
		/// Matches UWP behavior <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.geolocation.geolocator.desiredaccuracy#remarks">Docs</see> 
		/// </summary>
		public uint DesiredAccuracyInMeters
		{
			get => _desiredAccuracyInMeters;
			set
			{
				_desiredAccuracyInMeters = value;
				_actualDesiredAccuracyInMeters = value;
			}
		}

		/// <summary>
		/// By default null, can be set by the user when no better option exists
		/// </summary>
		public static BasicGeoposition? DefaultGeoposition { get; set; }

		/// <summary>
		/// Ideally should be true on devices without GPS capabilities
		/// </summary>
		public static bool IsDefaultGeopositionRecommended { get; private set; }
	}
}
#endif
