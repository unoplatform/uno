#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Media;

partial class ImageSource
{
	/// <summary>
	/// Initializes the Uno image downloader.
	/// </summary>
	private void InitializeDownloader()
	{
		Downloader = DefaultDownloader;
	}

	internal Stream? Stream { get; set; }

	/// <summary>
	/// Downloads an image from the provided Uri.
	/// </summary>
	/// <returns>n Uri containing a local path for the downloaded image.</returns>
	internal async Task<Uri> Download(CancellationToken ct, Uri uri)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("Initiated download from {0}", uri);
		}

		if (Downloader != null)
		{
			return await Downloader.Download(ct, uri);
		}
		else
		{
			throw new InvalidOperationException("No Downloader has been specified for this ImageSource. An IImageSourceDownloader may be provided to enable image downloads.");
		}
	}
}
