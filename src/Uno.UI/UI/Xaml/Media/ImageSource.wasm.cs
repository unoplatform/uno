#nullable enable

using Uno.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Uno.UI.Xaml.Media;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Microsoft.UI.Xaml.Media
{
	partial class ImageSource
	{
		partial void InitFromResource(Uri uri)
		{
			var path = uri.PathAndQuery.TrimStart("/");

			AbsoluteUri = new Uri(path, UriKind.Relative);
		}

		partial void CleanupResource()
		{
			AbsoluteUri = null;
		}

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

	}
}
