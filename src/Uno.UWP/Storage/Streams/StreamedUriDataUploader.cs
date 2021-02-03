#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.Storage.Streams
{
	internal class StreamedUriDataUploader : IStreamedDataUploader
	{
		private readonly Uri _uri;
		private readonly HttpMethod _method;
		private readonly HttpClient _client;

		public StreamedUriDataUploader(Uri uri, HttpMethod? method = null, HttpClient? client = null, TemporaryFile? tempFile = null)
		{
			_uri = uri;
			_method = method ?? HttpMethod.Put;
			_client = client ?? new HttpClient();
			File = tempFile ?? new TemporaryFile();
		}

		/// <inheritdoc />
		public TemporaryFile File { get; }

		/// <inheritdoc />
		public void CheckState()
		{
		}

		/// <inheritdoc />
		public async Task<bool> Push(ulong index, ulong length, CancellationToken ct)
		{
			try
			{
				var request = new HttpRequestMessage(_method, _uri)
				{
					// For now we don't use the range header!
					Content = new StreamContent(File.Open(FileAccess.Read))
				};

				var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

				response.EnsureSuccessStatusCode();

				return true;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("Failed to push content", e);
				}

				return false;
			}
		}
	}
}
