#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.Storage.Streams
{
	internal class StreamedUriDataLoader : IStreamedDataLoader, IDisposable
	{
#if __WASM__
		private const string _errorMessage = "Failed to load content. Make sure that CORS has been properly configured on the server hosting the resource.";
#else
		private const string _errorMessage = "Failed to load content.";
#endif

		/// <summary>
		/// Asynchronously creates a StreamedDataLoader.
		/// </summary>
		/// <remarks>This will make sure to have successfully contacted the server and loaded the ContentType before completing this async method.</remarks>
		public static async Task<StreamedUriDataLoader> Create(
			CancellationToken ct,
			Uri uri,
			HttpMethod? method = null,
			HttpClient? client = null,
			TemporaryFile? tempFile = null)
		{
			method ??= HttpMethod.Get;
			client ??= new HttpClient();
			tempFile ??= new TemporaryFile();

			var cts = new CancellationTokenSource();
			using (ct.Register(cts.Cancel))
			{
				var request = new HttpRequestMessage(method, uri);
				var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

				response.EnsureSuccessStatusCode();

				if (response.Content is null)
				{
					throw new InvalidOperationException("No content");
				}

				return new StreamedUriDataLoader(response, tempFile, cts);
			}
		}

		public event TypedEventHandler<IStreamedDataLoader, object?>? DataUpdated;

		private readonly CancellationTokenSource _ct;
		private bool _isCompleted;
		private Exception? _failure;
		private ulong _totalLoaded;

		private StreamedUriDataLoader(HttpResponseMessage response, TemporaryFile tempFile, CancellationTokenSource ct)
		{
			_ct = ct;
			File = tempFile;
			ContentType = response.Content.Headers.ContentType?.ToString();

			Task.Run(() => Download(response, ct.Token), ct.Token);
		}

		public string? ContentType { get; }

		public TemporaryFile File { get; }

		public void CheckState()
		{
			if (_failure is {})
			{
				throw new InvalidOperationException(_errorMessage, _failure);
			}
		}

		public bool CanRead(ulong position)
			=> _isCompleted || _failure is {} || position <= _totalLoaded;

		private async Task Download(HttpResponseMessage response, CancellationToken ct)
		{
			try
			{
				using var responseStream = await response.Content.ReadAsStreamAsync();
				using var file = File.OpenWeak(FileAccess.Write);

				var buffer = new byte[Buffer.DefaultCapacity];
				int read;
				while ((read = await responseStream.ReadAsync(buffer, 0, Buffer.DefaultCapacity, ct)) > 0)
				{
					await file.WriteAsync(buffer, 0, read, ct);
					await file.FlushAsync(ct); // We make sure to write the data to the disk before allow read to access it

					_totalLoaded += (ulong)read;
					DataUpdated?.Invoke(this, default);
				}

				_isCompleted = true;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(_errorMessage, e);
				}

				_failure = e;
			}
			finally
			{
				DataUpdated?.Invoke(this, default);
			}
		}

		/// <inheritdoc />
		public void Dispose()
			=> _ct.Cancel();

		~StreamedUriDataLoader()
			=> Dispose();
	}
}
