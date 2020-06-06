#if __IOS__ || __ANDROID__ || __WASM__ || __MACOS__
#pragma warning disable 67
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.WebUI;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private const uint DefaultAccuracyInMeters = 500;
		private const uint HighAccuracyInMeters = 10;		

		private static readonly object _syncLock = new object();

		//using ConcurrentDictionary as concurrent HashSet (https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework), byte is throwaway
		private static ConcurrentDictionary<Geolocator, byte> _statusChangedSubscriptions = new ConcurrentDictionary<Geolocator, byte>();

		private StartStopEventWrapper<TypedEventHandler<Geolocator, StatusChangedEventArgs>> _statusChangedWrapper;
		private StartStopEventWrapper<TypedEventHandler<Geolocator, PositionChangedEventArgs>> _positionChangedWrapper;			

		private PositionAccuracy _desiredAccuracy = PositionAccuracy.Default;
		private uint? _desiredAccuracyInMeters = DefaultAccuracyInMeters;
		private uint _actualDesiredAccuracyInMeters = DefaultAccuracyInMeters;

		public Geolocator()
		{
			_statusChangedWrapper = new StartStopEventWrapper<TypedEventHandler<Geolocator, StatusChangedEventArgs>>(
				() => StartStatusChanged(),
				() => StopStatusChanged(),
				_syncLock);
			_positionChangedWrapper = new StartStopEventWrapper<TypedEventHandler<Geolocator, PositionChangedEventArgs>>(
				() => StartPositionChanged(),
				() => StopPositionChanged(),
				_syncLock);

			InitializePlatform();
		}

		partial void InitializePlatform();

		/// <summary>
		/// By default null, can be set by the user when no better option exists
		/// </summary>
		public static BasicGeoposition? DefaultGeoposition { get; set; }

		/// <summary>
		/// Ideally should be set to true on devices without GPS capabilities, keep false as default
		/// </summary>
		public static bool IsDefaultGeopositionRecommended { get; private set; } = false;

		/// <summary>
		/// Default is NotInitialized in line with UWP
		/// </summary>
		public PositionStatus LocationStatus { get; private set; } = PositionStatus.NotInitialized;

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

		internal uint ActualDesiredAccuracyInMeters
		{
			get => _actualDesiredAccuracyInMeters;
			private set
			{
				lock (_syncLock)
				{
					_actualDesiredAccuracyInMeters = value;
					OnActualDesiredAccuracyInMetersChanged();
				}
			}
		}

		public event TypedEventHandler<Geolocator, StatusChangedEventArgs> StatusChanged
		{
			add => _statusChangedWrapper.AddHandler(value);
			remove => _statusChangedWrapper.RemoveHandler(value);
		}

		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged
		{
			add => _positionChangedWrapper.AddHandler(value);
			remove => _positionChangedWrapper.RemoveHandler(value);
		}
			   
		/// <summary>
		/// Broadcasts status change to all subscribed Geolocator instances
		/// </summary>
		/// <param name="positionStatus"></param>
		private static void BroadcastStatus(PositionStatus positionStatus)
		{
			foreach (var key in _statusChangedSubscriptions.Keys)
			{
				key.OnStatusChanged(positionStatus);
			}
		}

		private void StartStatusChanged() => _statusChangedSubscriptions.TryAdd(this, 0);

		private void StopStatusChanged() => _statusChangedSubscriptions.TryRemove(this, out var _);

		partial void StartPositionChanged();

		partial void StopPositionChanged();		

		partial void OnActualDesiredAccuracyInMetersChanged();

		/// <summary>
		/// Invokes <see cref="PositionChanged" /> event
		/// </summary>
		/// <param name="geoposition">Geoposition</param>
		private void OnPositionChanged(Geoposition geoposition)
		{
			_positionChangedWrapper.Event?.Invoke(this, new PositionChangedEventArgs(geoposition));
		}

		/// <summary>
		/// Invokes <see cref="StatusChanged" /> event
		/// </summary>
		/// <param name="status"></param>
		private void OnStatusChanged(PositionStatus status)
		{
			//report only when not changed
			//report initializing only when not initialized
			if (status == LocationStatus ||
				(status == PositionStatus.Initializing &&
				LocationStatus != PositionStatus.NotInitialized))
			{
				return;
			}

			LocationStatus = status;
			_statusChangedWrapper.Event?.Invoke(this, new StatusChangedEventArgs(status));
		}
	}
}
#endif
