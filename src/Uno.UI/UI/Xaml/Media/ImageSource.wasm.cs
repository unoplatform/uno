#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Streams;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media
{
	partial class ImageSource
	{
		private protected async Task<ImageData> OpenFromStream(IRandomAccessStreamWithContentType stream, Action<ulong, ulong?>? progress, CancellationToken ct)
		{
			try
			{
				var bytes = await stream.ReadBytesAsync(ct, progressCallback: progress);
				var encodedBytes = Convert.ToBase64String(bytes);

				ReportImageLoaded();

				return ImageData.FromDataUri("data:" + stream.ContentType + ";base64," + encodedBytes);
			}
			catch (Exception ex)
			{
				ReportImageFailed(ex.Message);

				return ImageData.FromError(ex);
			}
		}

		internal virtual void ReportImageLoaded()
		{

		}

		internal virtual void ReportImageFailed(string errorMessage)
		{

		}

		internal virtual string? ContentType { get; }

		internal async Task<ImageData> OpenMsAppData(Uri uri, CancellationToken ct)
		{
			var file = await StorageFile.GetFileFromPathAsync(AppDataUriEvaluator.ToPath(uri));
			using var stream = await file.OpenAsync(FileAccessMode.Read);

			var streamWithContentType = ContentType is { } contentType
				? stream.TrySetContentType(ContentType)
				: stream.TrySetContentType();

			return await OpenFromStream(streamWithContentType, null, ct);
		}

	}
}
