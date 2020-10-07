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
		private bool _isFailed;
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
			if (_isFailed)
			{
				throw new InvalidOperationException("Failed to load content");
			}
		}

		public bool CanRead(ulong position)
			=> _isCompleted || _isFailed || position <= _totalLoaded;

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

					_totalLoaded += (ulong)read;
					DataUpdated?.Invoke(this, default);
				}

				_isCompleted = true;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("Failed to load content", e);
				}

				_isFailed = true;
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
