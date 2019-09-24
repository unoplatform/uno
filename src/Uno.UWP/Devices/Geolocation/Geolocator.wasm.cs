#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno;

namespace Windows.Devices.Geolocation
{
	public sealed partial class Geolocator
	{
		private const string JsType = "Windows.Devices.Geolocation.Geolocator";

		private static List<TaskCompletionSource<GeolocationAccessStatus>> _pendingAccessRequests = new List<TaskCompletionSource<GeolocationAccessStatus>>();

		public Geolocator()
		{

		}

		public static async Task<GeolocationAccessStatus> RequestAccessAsync()
		{			
			var accessRequest = new TaskCompletionSource<GeolocationAccessStatus>();
			lock (_pendingAccessRequests)
			{
				//enqueue request
				_pendingAccessRequests.Add(accessRequest);
			}
			var command = $"{JsType}.requestAccess()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			//await access status asynchronously, will come back through DispatchAccessRequest call
			return await accessRequest.Task;
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
