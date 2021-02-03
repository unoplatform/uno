#nullable enable

using Uno.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

#if !IS_UNO
using Uno.Web.Query;
using Uno.Web.Query.Cache;
#endif

namespace Windows.UI.Xaml.Media
{
	partial class ImageSource
	{
		partial void InitFromResource(Uri uri)
		{
			WebUri = new Uri(uri.PathAndQuery.TrimStart("/"), UriKind.Relative);
		}

		private protected async Task<ImageData> OpenFromStream(IRandomAccessStreamWithContentType stream, Action<ulong, ulong?>? progress, CancellationToken ct)
		{
			try
			{
				var bytes = await stream.ReadBytesAsync(ct, progressCallback: progress);
				var encodedBytes = Convert.ToBase64String(bytes);

				ReportImageLoaded();

				return new ImageData
				{
					Kind = ImageDataKind.DataUri,
					Value = "data:" + stream.ContentType + ";base64," + encodedBytes
				};
			}
			catch (Exception ex)
			{
				ReportImageFailed(ex.Message);

				return new ImageData {Kind = ImageDataKind.Error, Error = ex};
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
