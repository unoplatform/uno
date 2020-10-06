#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;

namespace Windows.Storage
{
	public delegate void StreamedFileDataRequestedHandler(StreamedFileDataRequest stream);

	//internal static class StreamedFileDataRequestedHandlerHelper
	//{
	//	public static StreamedFileDataRequestedHandler CreateFromUri(
	//		Uri uri,
	//		HttpMethod? method = null,
	//		HttpClient? client = null,
	//		ActionAsync<HttpResponseMessage?, Exception?>? onReady = null)
	//	{
	//		method ??= HttpMethod.Get;
	//		client ??= new HttpClient();
			
	//		return req => Task.Run(() => FetchAsync(req, uri, method, client, onReady, req.CancellationToken));
	//	}

	//	private static async Task FetchAsync(
	//		StreamedFileDataRequest req,
	//		Uri uri,
	//		HttpMethod method,
	//		HttpClient client,
	//		ActionAsync<HttpResponseMessage?, Exception?>? onReady,
	//		CancellationToken ct)
	//	{
	//		try
	//		{
	//			HttpResponseMessage response;
	//			try
	//			{
	//				var request = new HttpRequestMessage(method, uri);
	//				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

	//				response.EnsureSuccessStatusCode();

	//				if (onReady is {})
	//				{
	//					await onReady(ct, response, null);
	//				}
	//			}
	//			catch (Exception e) when (onReady is {})
	//			{
	//				await onReady(ct, null, e);
	//				throw;
	//			}

	//			if (response.Content is { } content)
	//			{
	//				var responseStream = await content.ReadAsStreamAsync();
	//				await responseStream.CopyToAsync(req.AsStreamForWrite(), 8192, ct);
	//			}
	//		}
	//		catch (Exception e)
	//		{
	//			if (req.Log().IsEnabled(LogLevel.Warning))
	//			{
	//				req.Log().LogWarning("Failed to load content", e);
	//			}

	//			req.FailAndClose(StreamedFileFailureMode.Failed);
	//		}
	//		finally
	//		{
	//			req.Dispose();
	//		}
	//	}
	//}
}
