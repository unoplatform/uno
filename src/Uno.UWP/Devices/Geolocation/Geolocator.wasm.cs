using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using static Uno.Foundation.WebAssemblyRuntime;

using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.Devices.Geolocation.Geolocator.NativeMethods;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private const uint Wgs84SpatialReferenceId = 4326;

		private static List<TaskCompletionSource<GeolocationAccessStatus>> _pendingAccessRequests = new List<TaskCompletionSource<GeolocationAccessStatus>>();
		private static ConcurrentDictionary<string, TaskCompletionSource<Geoposition>> _pendingGeopositionRequests = new ConcurrentDictionary<string, TaskCompletionSource<Geoposition>>();
		private static ConcurrentDictionary<string, Geolocator> _positionChangedSubscriptions = new ConcurrentDictionary<string, Geolocator>();

		private string? _positionChangedRequestId;

		partial void OnActualDesiredAccuracyInMetersChanged()
		{
			if (_positionChangedWrapper.Event != null)
			{
				//reset position changed watch to apply accuracy
				StopPositionChanged();
				StartPositionChanged();
			}
		}

		partial void StartPositionChanged()
		{
			BroadcastStatusChanged(PositionStatus.Initializing); //GPS is initializing
			_positionChangedRequestId = Guid.NewGuid().ToString();
			_positionChangedSubscriptions.TryAdd(_positionChangedRequestId, this);
			NativeMethods.StartPositionWatch(ActualDesiredAccuracyInMeters, _positionChangedRequestId);
		}

		partial void StopPositionChanged()
		{
			_positionChangedSubscriptions.TryRemove(_positionChangedRequestId!, out var _);
			NativeMethods.StopPositionWatch(ActualDesiredAccuracyInMeters, _positionChangedRequestId!);
		}

		public static IAsyncOperation<GeolocationAccessStatus> RequestAccessAsync()
		{
			return AsyncOperation.FromTask(async ct =>
			{
				var accessRequest = new TaskCompletionSource<GeolocationAccessStatus>();
				lock (_pendingAccessRequests)
				{
					//enqueue request
					_pendingAccessRequests.Add(accessRequest);

					if (_pendingAccessRequests.Count == 1)
					{
						NativeMethods.RequestAccess();
					}
				}

				//await access status asynchronously, will come back through DispatchAccessRequest call
				var result = await accessRequest.Task;

				//if geolocation is not well accessible, default geoposition should be recommended
				if (result != GeolocationAccessStatus.Allowed)
				{
					IsDefaultGeopositionRecommended = true;
				}

				return result;
			});
		}

		/// <summary>
		/// UWP defaults to 60 seconds, but in practice the result is returned much sooner,
		/// even if it does not satisfy the user's requested accuracy. Hence we are using 10 seconds here.
		/// The maximum age is not specified in documentation, we use 30 seconds for low accuracy requests,
		/// 5 seconds for high accuracy requests
		/// </summary>
		/// <returns>Geoposition</returns>
		public IAsyncOperation<Geoposition> GetGeopositionAsync() =>
			GetGeopositionAsync(
				ActualDesiredAccuracyInMeters < 50 ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(30),
				TimeSpan.FromSeconds(10));

		/// <summary>
		/// Retrieves geoposition with specific maximum age and timeout
		/// </summary>
		/// <param name="maximumAge">Maximum age of the geoposition</param>
		/// <param name="timeout">Timeout for geoposition retrieval</param>
		/// <returns></returns>
		public IAsyncOperation<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			return AsyncOperation.FromTask(async ct =>
			{

				BroadcastStatusChanged(PositionStatus.Initializing); //GPS is initializing
				var completionRequest = new TaskCompletionSource<Geoposition>();
				var requestId = Guid.NewGuid().ToString();
				_pendingGeopositionRequests.TryAdd(requestId, completionRequest);
				NativeMethods.GetGeoposition(ActualDesiredAccuracyInMeters, maximumAge.TotalMilliseconds, timeout.TotalMilliseconds, requestId);
				return await completionRequest.Task;
			});
		}

		internal static int DispatchGeoposition(string serializedGeoposition, string requestId)
		{
			BroadcastStatusChanged(PositionStatus.Ready); //whenever a location is successfully retrieved, GPS has state of Ready
			var geocoordinate = ParseGeocoordinate(serializedGeoposition);
			if (_pendingGeopositionRequests.TryRemove(requestId, out var geopositionCompletionSource))
			{
				geopositionCompletionSource.SetResult(new Geoposition(geocoordinate));
			}
			else if (_positionChangedSubscriptions.TryGetValue(requestId, out var geolocator))
			{
				geolocator.OnPositionChanged(new Geoposition(geocoordinate));
			}

			return 0;
		}

		/// <summary>
		/// Invokes <see cref="PositionChanged" /> event
		/// </summary>
		/// <param name="geoposition">Geoposition</param>
		private void OnPositionChanged(Geoposition geoposition)
		{
			_positionChangedWrapper.Event?.Invoke(this, new PositionChangedEventArgs(geoposition));
		}

		internal static int DispatchError(string currentPositionRequestResult, string requestId)
		{
			if (_pendingGeopositionRequests.TryRemove(requestId, out var geopositionCompletionSource))
			{
				if (!Enum.TryParse<PositionStatus>(currentPositionRequestResult, true, out var positionStatus))
				{
					throw new ArgumentOutOfRangeException(
						nameof(currentPositionRequestResult),
						"DispatchError argument must be a serialzied PositionStatus");
				}
				BroadcastStatusChanged(positionStatus);
				switch (positionStatus)
				{
					case PositionStatus.NoData:
						geopositionCompletionSource.SetException(
							new Exception("This operation returned because the timeout period expired."));
						break;
					case PositionStatus.Disabled:
						geopositionCompletionSource.SetException(
							new UnauthorizedAccessException(
								"Access is denied. Your App does not have permission to access location data. " +
								"The user blocked access, or the device location services are currently turned off."));
						break;
					case PositionStatus.NotAvailable:
						geopositionCompletionSource.SetException(
							new InvalidOperationException("Location services API is not available."));
						break;
					default:
						throw new ArgumentOutOfRangeException(
							nameof(currentPositionRequestResult), "DispatchError position status must be an error status");
				}
			}
			return 0;
		}

		/// <summary>
		/// Handles result of Geolocation access request (initiated by <see cref="RequestAccessAsync"/>.
		/// </summary>
		/// <param name="serializedAccessStatus">Serialized string value from the <see cref="GeolocationAccessStatus"/> enum</param>
		/// <returns>0 - needed to bind method from WASM</returns>
		internal static int DispatchAccessRequest(string serializedAccessStatus)
		{
			if (serializedAccessStatus is null)
			{
				throw new ArgumentNullException(nameof(serializedAccessStatus));
			}
			if (!Enum.TryParse<GeolocationAccessStatus>(serializedAccessStatus, true, out var geolocationAccessStatus))
			{
				throw new ArgumentOutOfRangeException(
					nameof(serializedAccessStatus),
					"Parameter must be parseable as GeolocationAccessStatus enum value");
			}

			lock (_pendingAccessRequests)
			{
				//resolve all pending requests
				foreach (var request in _pendingAccessRequests)
				{
					request.SetResult(geolocationAccessStatus);
				}
				_pendingAccessRequests.Clear();
			}
			return 0;
		}

		/// <summary>
		/// Parses geocoordinate retrieved from JS in form of colon-separated string
		/// </summary>
		/// <param name="serializedGeoposition">Serialized, colon-separated position</param>
		/// <returns>Geocoordinate</returns>
		private static Geocoordinate ParseGeocoordinate(string serializedPosition)
		{
			var dataSplit = serializedPosition.Split(':');

			var latitude = double.Parse(dataSplit[0], CultureInfo.InvariantCulture);
			var longitude = double.Parse(dataSplit[1], CultureInfo.InvariantCulture);

			double? altitude = null;
			if (double.TryParse(dataSplit[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedAltitude))
			{
				altitude = parsedAltitude;
			}

			double? altitudeAccuracy = null;
			if (double.TryParse(dataSplit[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedAltitudeAccuracy))
			{
				altitudeAccuracy = parsedAltitudeAccuracy;
			}

			var accuracy = double.Parse(dataSplit[4], CultureInfo.InvariantCulture);

			double? heading = null;
			if (double.TryParse(dataSplit[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeading))
			{
				heading = parsedHeading;
			}

			double? speed = null;
			if (double.TryParse(dataSplit[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpeed))
			{
				speed = parsedSpeed;
			}

			var timestamp = DateTimeOffset.UtcNow;
			if (long.TryParse(dataSplit[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTimestamp))
			{
				timestamp = DateTimeOffset.FromUnixTimeMilliseconds(parsedTimestamp);
			}

			var geocoordinate = new Geocoordinate(
				latitude,
				longitude,
				accuracy,
				timestamp,
				point: new Geopoint(
					new BasicGeoposition { Longitude = longitude, Latitude = latitude, Altitude = altitude ?? 0 },
					AltitudeReferenceSystem.Ellipsoid, //based on https://www.w3.org/TR/geolocation-API/
					Wgs84SpatialReferenceId), //based on https://en.wikipedia.org/wiki/Spatial_reference_system
				altitude: altitude,
				altitudeAccuracy: altitudeAccuracy,
				heading: heading,
				speed: speed);
			return geocoordinate;
		}
	}
}

namespace Uno.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		[JSExport]
		internal static int DispatchAccessRequest(string serializedAccessStatus)
			=> global::Windows.Devices.Geolocation.Geolocator.DispatchAccessRequest(serializedAccessStatus);

		[JSExport]
		internal static int DispatchGeoposition(string serializedGeoposition, string requestId)
			=> global::Windows.Devices.Geolocation.Geolocator.DispatchGeoposition(serializedGeoposition, requestId);

		[JSExport]
		internal static int DispatchError(string currentPositionRequestResult, string requestId)
			=> global::Windows.Devices.Geolocation.Geolocator.DispatchError(currentPositionRequestResult, requestId);

	}
}
