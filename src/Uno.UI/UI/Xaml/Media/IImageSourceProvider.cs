using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Media
{
	/// <summary>
	/// Defines an interface for <see cref="ImageSource"/> to enable image downloads.
	/// </summary>
	public interface IImageSourceDownloader
	{
		/// <summary>
		/// Downloads a image and returns a valid local file system Uri.
		/// </summary>
		/// <param name="ct">An cancellation token</param>
		/// <param name="uri">A valid uri for the image</param>
		/// <returns>A valid local file system Uri.</returns>
		Task<Uri> Download(CancellationToken ct, Uri uri);
	}
}
