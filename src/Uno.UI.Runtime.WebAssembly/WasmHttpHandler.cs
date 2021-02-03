using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Threading;

namespace Uno.UI.Wasm
{
	public class WasmHttpHandler : HttpMessageHandler
	{
		private static long RequestsCounter;

		private static ImmutableDictionary<long, FastTaskCompletionSource<(int status, string headers, string payload)>> PendingExecutions =
			ImmutableDictionary<long, FastTaskCompletionSource<(int, string, string)>>.Empty;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var requestId = Interlocked.Increment(ref RequestsCounter);

			string requestPayload;
			string requestPayloadType;
			if (request.Content != null)
			{
				var bytes = await request.Content.ReadAsByteArrayAsync();
				var base64 = Convert.ToBase64String(bytes);
				requestPayload = string.Concat("\"", base64, "\"");
				requestPayloadType = string.Concat("\"", request.Content.Headers.ContentType.MediaType, "\"");
			}
			else
			{
				requestPayload = "null";
				requestPayloadType = "null";
			}

			string Escape(string s) => WebAssemblyRuntime.EscapeJs(s);

			var requestHeaders = request.Headers
				.SelectMany(h => h.Value
					.Select(v => string.Concat("[\"", Escape(h.Key), "\", \"", Escape(v), "\"]")));

			var command = string.Concat(
				"Uno.Http.HttpClient.send({id: \"",
				requestId,
				"\", method:\"",
				request.Method,
				"\", url:\"",
				request.RequestUri,
				"\", headers:[",
				string.Join(",", requestHeaders),
				"], payload:",
				requestPayload,
				", payloadType:",
				requestPayloadType,
				"});");
			WebAssemblyRuntime.InvokeJS(command);

			var tcs = new FastTaskCompletionSource<(int status, string headers, string payload)>();
			ImmutableInterlocked.TryAdd(ref PendingExecutions, requestId, tcs);

			try
			{
				var response = await tcs.Task;

				var responseMessage = new HttpResponseMessage((HttpStatusCode)response.status);
				var responseContent = new ByteArrayContent(Convert.FromBase64String(response.payload));
				responseMessage.Content = responseContent;
				responseMessage.RequestMessage = request;

				var headers = response.headers
					.Split('\n')
					.Select(h => h.Split(new [] {':'}, 2))
					.Where(h => h.Length == 2)
					.GroupBy(h => h[0], h => h[1]);

				foreach (var header in headers)
				{
					if (!responseMessage.Content.Headers.TryAddWithoutValidation(header.Key, header))
					{
						responseMessage.Headers.TryAddWithoutValidation(header.Key, header);
					}
				}

				return responseMessage;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				throw;
			}
		}

		[Preserve]
		public static void DispatchResponse(string requestId, string status, string headers, string payload)
		{
			var id = long.Parse(requestId, CultureInfo.InvariantCulture);
			var s = int.Parse(status, CultureInfo.InvariantCulture);

			if (ImmutableInterlocked.TryRemove(ref PendingExecutions, id, out var v))
			{
				v.SetResult((s, headers, payload));
			}
		}

		[Preserve]
		public static void DispatchError(string requestId, string errorMessage)
		{
			var id = long.Parse(requestId, CultureInfo.InvariantCulture);
			if (ImmutableInterlocked.TryRemove(ref PendingExecutions, id, out var v))
			{
				v.SetException(new Exception(errorMessage));
			}
		}
	}
}
