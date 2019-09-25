#if __WASM__
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
			return await accessRequest.Task;
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
			var command = FormattableString.Invariant($"{JsType}.getGeoposition({maximumAge.TotalMilliseconds},{timeout.TotalMilliseconds},{DesiredAccuracy},{DesiredAccuracyInMeters},\"{requestId}\")");			
			InvokeJS(command);
			return await completionRequest.Task;
		}

		[Preserve]
		public static int DispatchGeopositionRequest(string currentPositionRequestResult)
		{
			if (currentPositionRequestResult.StartsWith("success", StringComparison.InvariantCultureIgnoreCase))
			{
				
			}
			else
			{
				//set appropriate exception on all waiting requests

			}
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
	}
}
#endif
