#if __WASM__
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private const string JsType = "Windows.Devices.Geolocation.Geolocator";

		private static List<TaskCompletionSource<GeolocationAccessStatus>> _pendingAccessRequests = new List<TaskCompletionSource<GeolocationAccessStatus>>();
		private static ConcurrentDictionary<string, TaskCompletionSource<Geoposition>> _pendingGeopositionRequests = new ConcurrentDictionary<string, TaskCompletionSource<Geoposition>>();

		public Geolocator()
		{

		}

		public event TypedEventHandler<Geolocator, StatusChangedEventArgs> StatusChanged;

		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{
			var accessRequest = new TaskCompletionSource<GeolocationAccessStatus>();
			lock (_pendingAccessRequests)
			{
				//enqueue request
				_pendingAccessRequests.Add(accessRequest);
			}
			var command = $"{JsType}.requestAccess()";
			InvokeJS(command);
			//await access status asynchronously, will come back through DispatchAccessRequest call
			var result = await accessRequest.Task;

			//if geolocation is not well accessible, default geoposition should be recommended
			if (result == GeolocationAccessStatus.Unspecified)
			{
				IsDefaultGeopositionRecommended = true;
			}
			return result;
		}

		/// <summary>
		/// Uses 60 second timeout to match the UWP default.
		/// The maximum age is not specified in documentation, so we use 10 s for now.
		/// </summary>
		/// <returns>Geoposition</returns>
		public Task<Geoposition> GetGeopositionAsync() =>
			GetGeopositionAsync(TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(60));

		public async Task<Geoposition> GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout)
		{
			var completionRequest = new TaskCompletionSource<Geoposition>();
			string requestId;
			do
			{
				requestId = Guid.NewGuid().ToString();
			} while (_pendingGeopositionRequests.TryAdd(requestId, completionRequest));
			var command = FormattableString.Invariant($"{JsType}.getGeoposition({ActualDesiredAccuracyInMeters},{maximumAge.TotalMilliseconds},{timeout.TotalMilliseconds},\"{requestId}\")");
			InvokeJS(command);
			return await completionRequest.Task;
		}

		[Preserve]
		public static int DispatchGeoposition(string serializedGeoposition, string requestId)
		{
			if (!_pendingGeopositionRequests.TryGetValue(requestId, out var requestTask))
			{
				throw new InvalidOperationException($"Geoposition request {requestId} does not exist");
			}

			var geocoordinate = ParseGeocoordinate(serializedGeoposition);

			requestTask.SetResult(new Geoposition(geocoordinate));

			return 0;
		}

		[Preserve]
		public static int DispatchError(string currentPositionRequestResult)
		{
			if (currentPositionRequestResult.StartsWith("erros", StringComparison.InvariantCultureIgnoreCase))
			{

			}
			else
			{
				//set appropriate exception on all waiting requests

			}
			return 0;
		}

		/// <summary>
		/// Handles result of Geolocation access request (initiated by <see cref="RequestAccessAsync"/>.
		/// </summary>
		/// <param name="serializedAccessStatus">Serialized string value from the <see cref="GeolocationAccessStatus"/> enum</param>
		/// <returns>0 - needed to bind method from WASM</returns>
		[Preserve]
		public static int DispatchAccessRequest(string serializedAccessStatus)
		{
			if (serializedAccessStatus is null)
			{
				throw new ArgumentNullException(nameof(serializedAccessStatus));
			}
			if (!Enum.TryParse<GeolocationAccessStatus>(serializedAccessStatus, out var geolocationAccessStatus))
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

			var latitude = double.Parse(dataSplit[0]);
			var longitude = double.Parse(dataSplit[1]);

			double? altitude = null;
			if (double.TryParse(dataSplit[2], out double parsedAltitude))
			{
				altitude = parsedAltitude;
			}

			double? altitudeAccuracy = null;
			if (double.TryParse(dataSplit[3], out double parsedAltitudeAccuracy))
			{
				altitudeAccuracy = parsedAltitudeAccuracy;
			}

			var accuracy = double.Parse(dataSplit[4]);

			double? heading = null;
			if (double.TryParse(dataSplit[5], out var parsedHeading))
			{
				heading = parsedHeading;
			}

			double? speed = null;
			if (double.TryParse(dataSplit[6], out var parsedSpeed))
			{
				speed = parsedSpeed;
			}

			var timestamp = DateTimeOffset.UtcNow;
			if (long.TryParse(dataSplit[7], out var parsedTimestamp))
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
					4326u), //based on https://en.wikipedia.org/wiki/Spatial_reference_system
				altitude: altitude,
				altitudeAccuracy: altitudeAccuracy,
				heading: heading,
				speed: speed);
			return geocoordinate;
		}
	}
}
#endif
