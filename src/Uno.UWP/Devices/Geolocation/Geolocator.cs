#if __IOS__ || __ANDROID__ || __WASM__ || __SKIA__
#pragma warning disable 67
using System.Collections.Concurrent;
using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Devices.Geolocation
{
	/// <summary>
	/// Provides access to the current geographic location.
	/// </summary>
	public sealed partial class Geolocator
	{
		private const uint DefaultAccuracyInMeters = 500;
		private const uint HighAccuracyInMeters = 10;

		private static readonly object _syncLock = new();

		// Using ConcurrentDictionary as concurrent HashSet (https://stackoverflow.com/questions/18922985/concurrent-hashsett-in-net-framework), byte is throwaway
		private static readonly ConcurrentDictionary<Geolocator, byte> _statusChangedSubscriptions = new ConcurrentDictionary<Geolocator, byte>();

		private readonly StartStopTypedEventWrapper<Geolocator, StatusChangedEventArgs> _statusChangedWrapper;
		private readonly StartStopTypedEventWrapper<Geolocator, PositionChangedEventArgs> _positionChangedWrapper;

		private PositionAccuracy _desiredAccuracy = PositionAccuracy.Default;

		private uint? _desiredAccuracyInMeters = DefaultAccuracyInMeters;

		private uint _actualDesiredAccuracyInMeters = DefaultAccuracyInMeters;

		/// <summary>
		/// Initializes a new Geolocator object.
		/// </summary>
		public Geolocator()
		{
			_statusChangedWrapper = new StartStopTypedEventWrapper<Geolocator, StatusChangedEventArgs>(
				() => StartStatusChanged(),
				() => StopStatusChanged(),
				_syncLock);
			_positionChangedWrapper = new StartStopTypedEventWrapper<Geolocator, PositionChangedEventArgs>(
				() => StartPositionChanged(),
				() => StopPositionChanged(),
				_syncLock);

			PlatformInitialize();
		}

		partial void PlatformInitialize();

		~Geolocator()
		{
			StopStatusChanged();
			PlatformDestruct();
		}

		partial void PlatformDestruct();

		/// <summary>
		/// Raised when the ability of the Geolocator to provide updated location changes.
		/// </summary>
		public event TypedEventHandler<Geolocator, StatusChangedEventArgs> StatusChanged
		{
			add => _statusChangedWrapper.AddHandler(value);
			remove => _statusChangedWrapper.RemoveHandler(value);
		}

		/// <summary>
		/// Raised when the location is updated.
		/// </summary>
		public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged
		{
			add => _positionChangedWrapper.AddHandler(value);
			remove => _positionChangedWrapper.RemoveHandler(value);
		}

		/// <summary>
		/// Gets or sets the location manually entered into the system by the user, to be utilized if no better options exist.
		/// </summary>
		public static BasicGeoposition? DefaultGeoposition { get; set; }

		/// <summary>
		/// The accuracy level at which the Geolocator provides location updates.
		/// </summary>
		/// <remarks>
		/// Does not overwrite <see cref="DesiredAccuracyInMeters"/> directly.
		/// Default is equivalent to 500 meters, High is equivalent to 10 meters
		/// Matches UWP behavior <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.geolocation.geolocator.desiredaccuracy#remarks">Docs</see>
		/// </remarks>
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
		/// Gets or sets the desired accuracy in meters for data returned from the location service.
		/// </summary>
		/// <remarks>
		/// Does not overwrite <see cref="DesiredAccuracy"/>. When set to null, <see cref="DesiredAccuracy" /> is reapplied.
		/// Matches UWP behavior <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.geolocation.geolocator.desiredaccuracy#remarks">Docs</see>
		/// </remarks>
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
					// Force set DesiredAccuracy so that its ActualDesiredAccuracyInMeters rule is applied
					DesiredAccuracy = DesiredAccuracy;
					OnDesiredAccuracyInMetersChanged();
				}
			}
		}

		/// <summary>
		/// Indicates whether the user should be prompted to set a default location manually.
		/// </summary>
		/// <remarks>
		///	Should be set to true on devices without GPS.
		/// </remarks>
		public static bool IsDefaultGeopositionRecommended { get; private set; }

		/// <summary>
		/// The status that indicates the ability of the Geolocator to provide location updates.
		/// </summary>
		public PositionStatus LocationStatus { get; private set; } = PositionStatus.NotInitialized;

		partial void OnDesiredAccuracyInMetersChanged();

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

		/// <summary>
		/// Broadcasts status change to all subscribed Geolocator instances
		/// </summary>
		/// <param name="positionStatus"></param>
		private static void BroadcastStatusChanged(PositionStatus positionStatus)
		{
			foreach (var subscriber in _statusChangedSubscriptions.Keys)
			{
				subscriber.OnStatusChanged(positionStatus);
			}
		}

		private void StartStatusChanged() => _statusChangedSubscriptions.TryAdd(this, 0);

		private void StopStatusChanged() => _statusChangedSubscriptions.TryRemove(this, out var _);

		partial void StartPositionChanged();

		partial void StopPositionChanged();

		partial void OnActualDesiredAccuracyInMetersChanged();

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
