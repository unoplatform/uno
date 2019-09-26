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

		private static readonly object _syncLock = new object();

		private TypedEventHandler<Geolocator, PositionChangedEventArgs> _positionChanged;

		private PositionAccuracy _desiredAccuracy = PositionAccuracy.Default;
		private uint? _desiredAccuracyInMeters = DefaultAccuracyInMeters;

		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged
		{
			add
			{
				lock (_syncLock)
				{
					bool isFirstSubscriber = _positionChanged == null;
					_positionChanged += value;
					if (isFirstSubscriber)
					{
						StartPositionChanged();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_positionChanged -= value;
					if (_positionChanged == null)
					{
						StopPositionChanged();
					}
				}
			}
		}

		partial void StartPositionChanged();

		partial void StopPositionChanged();

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
		public uint? DesiredAccuracyInMeters
		{
			get => _desiredAccuracyInMeters;
			set
			{
				_desiredAccuracyInMeters = value;
				if (value != null)
				{
					ActualDesiredAccuracyInMeters = value.Value;
				}
				else
				{
					//force set DesiredAccuracy so that its ActualDesiredAccuracyInMeters rule is applied
					DesiredAccuracy = DesiredAccuracy; 
				}
			}
		}

		internal uint ActualDesiredAccuracyInMeters { get; private set; } = DefaultAccuracyInMeters;
	}
}
#endif
