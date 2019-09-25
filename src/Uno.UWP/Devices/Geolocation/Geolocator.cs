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
		private const uint DefaultAccuracyInMeters = 500;
		private const uint HighAccuracyInMeters = 10;

		private PositionAccuracy _desiredAccuracy = PositionAccuracy.Default;
		private uint _desiredAccuracyInMeters = DefaultAccuracyInMeters;

		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged;


		/// <summary>
		/// By default null, can be set by the user when no better option exists
		/// </summary>
		public static BasicGeoposition? DefaultGeoposition { get; set; }

		/// <summary>
		/// Ideally should be set to true on devices without GPS capabilities, keep false as default
		/// </summary>
		public static bool IsDefaultGeopositionRecommended { get; private set; } = false;

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
				ActualDesiredAccuracyInMeters =
					value == PositionAccuracy.Default ?
						DefaultAccuracyInMeters : HighAccuracyInMeters;
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
				ActualDesiredAccuracyInMeters = value;
			}
		}

		internal uint ActualDesiredAccuracyInMeters { get; private set; } = DefaultAccuracyInMeters;
	}
}
#endif
