#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Uno.Devices.Midi.Internal
{
	/// <summary>
	/// Needs to be public to get
	/// </summary>
	public static class WasmMidiAccess
	{
		private const string JsType = "Uno.Devices.Midi.Internal.WasmMidiAccess";
		private readonly static List<TaskCompletionSource<bool>> _pendingAccessRequests = new List<TaskCompletionSource<bool>>();

		private static bool _webMidiAccessible = false;

		/// <summary>
		/// Handles MIDI device access request (initiated by <see cref="RequestAsync"/>).
		/// </summary>
		/// <param name="hasAccess">Indicates if user has granted access</param>
		/// <returns>0 - needed to bind method from WASM</returns>
		[Preserve]
		public static int DispatchRequest(bool hasAccess)
		{
			lock (_pendingAccessRequests)
			{
				_webMidiAccessible = hasAccess;
				//resolve all pending requests
				foreach (var request in _pendingAccessRequests)
				{
					request.SetResult(hasAccess);
				}
				_pendingAccessRequests.Clear();
			}
			return 0;
		}

		internal static async Task<bool> RequestAsync()
		{
			if (_webMidiAccessible) return true;

			var accessRequest = new TaskCompletionSource<bool>();
			lock (_pendingAccessRequests)
			{
				//enqueue request
				_pendingAccessRequests.Add(accessRequest);

				if (_pendingAccessRequests.Count == 1)
				{
					//TODO: Support for SYS EX MESSAGES in wasm request
					//there are no access requests currently waiting for resolution, we need to invoke the check in JS
					var command = $"{JsType}.request()";
					InvokeJS(command);
				}
			}

			//await access status asynchronously, will come back through DispatchAccessRequest call
			return await accessRequest.Task;
		}
	}
}
#endif
