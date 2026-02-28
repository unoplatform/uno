#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private static IGeolocatorExtension? _geolocatorExtension;

		partial void PlatformInitialize()
		{
			EnsureExtensionInitialized();
		}

		partial void PlatformDestruct()
		{
		}

		public static IAsyncOperation<GeolocationAccessStatus> RequestAccessAsync()
		{
			return RequestAccessTaskAsync().AsAsyncOperation();
		}

		private static async Task<GeolocationAccessStatus> RequestAccessTaskAsync()
		{
			EnsureExtensionInitialized();

			if (_geolocatorExtension != null)
			{
				return await _geolocatorExtension.RequestAccessAsync();
			}

			return GeolocationAccessStatus.Unspecified;
		}

		public IAsyncOperation<Geoposition> GetGeopositionAsync()
		{
			return GetGeopositionTaskAsync().AsAsyncOperation();
		}

		private async Task<Geoposition> GetGeopositionTaskAsync()
		{
			EnsureExtensionInitialized();

			if (_geolocatorExtension != null)
			{
				return await _geolocatorExtension.GetGeopositionAsync(ActualDesiredAccuracyInMeters);
			}

			throw new NotImplementedException("The member IAsyncOperation<Geoposition> Geolocator.GetGeopositionAsync() is not implemented on this platform.");
		}

		public IAsyncOperation<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			return GetGeopositionTaskAsync(maximumAge, timeout).AsAsyncOperation();
		}

		private async Task<Geoposition> GetGeopositionTaskAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			EnsureExtensionInitialized();

			if (_geolocatorExtension != null)
			{
				return await _geolocatorExtension.GetGeopositionAsync(ActualDesiredAccuracyInMeters, maximumAge, timeout);
			}

			throw new NotImplementedException("The member IAsyncOperation<Geoposition> Geolocator.GetGeopositionAsync(TimeSpan, TimeSpan) is not implemented on this platform.");
		}

		partial void StartPositionChanged()
		{
			EnsureExtensionInitialized();

			if (_geolocatorExtension != null)
			{
				_geolocatorExtension.StartPositionChanged(ActualDesiredAccuracyInMeters, OnPositionChanged);
			}
		}

		partial void StopPositionChanged()
		{
			EnsureExtensionInitialized();

			if (_geolocatorExtension != null)
			{
				_geolocatorExtension.StopPositionChanged();
			}
		}

		partial void OnActualDesiredAccuracyInMetersChanged()
		{
			// If position tracking is active, restart it with new accuracy
			if (_positionChangedWrapper.Event != null)
			{
				StopPositionChanged();
				StartPositionChanged();
			}
		}

		private void OnPositionChanged(Geoposition position)
		{
			BroadcastStatusChanged(PositionStatus.Ready);
			_positionChangedWrapper.Event?.Invoke(this, new PositionChangedEventArgs(position));
		}

		private static void EnsureExtensionInitialized()
		{
			if (_geolocatorExtension == null)
			{
				ApiExtensibility.CreateInstance(typeof(Geolocator), out _geolocatorExtension);
			}
		}
	}

	internal interface IGeolocatorExtension
	{
		Task<GeolocationAccessStatus> RequestAccessAsync();

		Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters);

		Task<Geoposition> GetGeopositionAsync(uint desiredAccuracyInMeters, TimeSpan maximumAge, TimeSpan timeout);

		void StartPositionChanged(uint desiredAccuracyInMeters, Action<Geoposition> onPositionChanged);

		void StopPositionChanged();
	}
}
