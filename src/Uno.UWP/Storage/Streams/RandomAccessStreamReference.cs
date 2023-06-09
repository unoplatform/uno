#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno;
using Uno.Extensions;
using Uno.Helpers;

namespace Windows.Storage.Streams
{
	public partial class RandomAccessStreamReference : IRandomAccessStreamReference
	{
		public static RandomAccessStreamReference CreateFromFile(IStorageFile file)
			=> new RandomAccessStreamReference(file.OpenReadAsync);

		public static RandomAccessStreamReference CreateFromUri(Uri uri)
			=> new RandomAccessStreamReference(async ct
				=>
			{
				if (uri.IsAppData())
				{
					var storageFile = StorageFile.GetFileFromPath(AppDataUriEvaluator.ToPath(uri));
					return await storageFile.OpenReadAsync();
				}

				var downloader = await StreamedUriDataLoader.Create(ct, uri);
				return new StreamedRandomAccessStream(downloader);
			});

		public static RandomAccessStreamReference CreateFromStream(IRandomAccessStream stream)
			=> new RandomAccessStreamReference(ct =>
			{
				return Task.FromResult(stream.TrySetContentType());
			});

		private readonly Func<IAsyncOperation<IRandomAccessStreamWithContentType>> _open;

		internal RandomAccessStreamReference(Func<IAsyncOperation<IRandomAccessStreamWithContentType>> open)
		{
			_open = open;
		}

		internal RandomAccessStreamReference(Func<CancellationToken, Task<IRandomAccessStreamWithContentType>> open)
			: this(() => AsyncOperation.FromTask(open))
		{
		}

		public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
			=> _open();
	}
}
