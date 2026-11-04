#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// Minimal loopback HTTP server that serves ms-appx:///Assets/square100.png, sending the response headers immediately but
/// holding the image bytes until <see cref="ReleaseResponses"/>, so a test can act while a download is deterministically
/// in flight. <see cref="HoldResponses"/> holds the later ones again. With <see cref="FailNextRequest"/> set, the next
/// request gets an HTTP 500 instead of the image.
/// </summary>
internal sealed class GatedImageServer : IDisposable
{
	private readonly TcpListener _listener;
	private readonly byte[] _body;
	private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private bool _isDisposed;
	private int _requestCount;
	private int _bodiesSent;
	private volatile bool _failNextRequest;

	private GatedImageServer(TcpListener listener, byte[] body)
	{
		_listener = listener;
		_body = body;
		Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/square100.png");

		// Off the UI thread: a test may hold the UI thread while a response is pending, and awaits started from it would
		// otherwise resume through its synchronization context.
		_ = Task.Run(AcceptLoopAsync);
	}

	public static async Task<GatedImageServer> StartAsync()
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/square100.png"));
		using var stream = await file.OpenStreamForReadAsync();
		using var memory = new MemoryStream();
		await stream.CopyToAsync(memory);

		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return new GatedImageServer(listener, memory.ToArray());
	}

	public Uri Uri { get; }

	public int RequestCount => Volatile.Read(ref _requestCount);

	public int BodiesSent => Volatile.Read(ref _bodiesSent);

	public bool FailNextRequest
	{
		get => _failNextRequest;
		set => _failNextRequest = value;
	}

	public void ReleaseResponses() => Volatile.Read(ref _release).TrySetResult();

	/// <summary>
	/// Holds the image bytes of the responses sent from now on, until <see cref="ReleaseResponses"/>.
	/// </summary>
	public void HoldResponses()
	{
		// A gate still held stays, so the requests waiting on it are released with the later ones.
		if (!_isDisposed && Volatile.Read(ref _release).Task.IsCompleted)
		{
			Volatile.Write(ref _release, new(TaskCreationOptions.RunContinuationsAsynchronously));
		}
	}

	public void Dispose()
	{
		_isDisposed = true;
		Volatile.Read(ref _release).TrySetResult();
		_listener.Stop();
	}

	private async Task AcceptLoopAsync()
	{
		try
		{
			while (true)
			{
				var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
				_ = HandleAsync(client);
			}
		}
		catch (ObjectDisposedException)
		{
		}
		catch (SocketException)
		{
		}
	}

	private async Task HandleAsync(TcpClient client)
	{
		try
		{
			using var _ = client;
			using var stream = client.GetStream();

			var request = new byte[8192];
			var read = 0;
			while (!Encoding.ASCII.GetString(request, 0, read).Contains("\r\n\r\n"))
			{
				var count = await stream.ReadAsync(request, read, request.Length - read).ConfigureAwait(false);
				if (count == 0)
				{
					return;
				}

				read += count;
			}

			Interlocked.Increment(ref _requestCount);

			if (_failNextRequest)
			{
				_failNextRequest = false;
				await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 500 Internal Server Error\r\nContent-Length: 0\r\nConnection: close\r\n\r\n")).ConfigureAwait(false);
				await stream.FlushAsync().ConfigureAwait(false);
				return;
			}

			var headers = $"HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nContent-Length: {_body.Length}\r\nConnection: close\r\n\r\n";
			await stream.WriteAsync(Encoding.ASCII.GetBytes(headers)).ConfigureAwait(false);
			await stream.FlushAsync().ConfigureAwait(false);

			await Volatile.Read(ref _release).Task.ConfigureAwait(false);

			await stream.WriteAsync(_body).ConfigureAwait(false);
			await stream.FlushAsync().ConfigureAwait(false);
			Interlocked.Increment(ref _bodiesSent);
		}
		catch (IOException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
		catch (SocketException)
		{
		}
	}
}
