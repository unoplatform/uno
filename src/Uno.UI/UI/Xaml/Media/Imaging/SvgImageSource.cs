#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Uno;
using Uno.UI.Xaml.Media;
using System.Net.Http;
using Uno.Helpers;
using Uno.Foundation.Logging;
using Windows.Application­Model;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// Provides a source object for properties that use a Scalable Vector Graphics (SVG) source. You can define a SvgImageSource
/// by using a Uniform Resource Identifier (URI) that references a SVG file, or by calling SetSourceAsync(IRandomAccessStream)
/// and supplying a stream.
/// </summary>
public partial class SvgImageSource : ImageSource
{
	private bool _openedRaised;

#if __CROSSRUNTIME__
	private IRandomAccessStream? _stream;
#endif

	/// <summary>
	/// Initializes a new instance of the SvgImageSource class.
	/// </summary>
	public SvgImageSource()
	{
		Initialize();
	}

	/// <summary>
	/// Initializes a new instance of the SvgImageSource class, using the supplied Uniform Resource Identifier (URI).
	/// </summary>
	/// <param name="uriSource"></param>
	public SvgImageSource(Uri uriSource)
		: this()
	{
		UriSource = uriSource;
	}

	private void Initialize()
	{
		InitPartial();
	}

	private void OnUriSourceChanged(DependencyPropertyChangedEventArgs e)
	{
		if (!object.Equals(e.OldValue, e.NewValue))
		{
			UnloadImageData();
		}

		InitFromUri(e.NewValue as Uri);

#if __CROSSRUNTIME__
		InvalidateSource();
#endif
		InvalidateImageSource();
	}

	/// <summary>
	/// Sets the source SVG for a SvgImageSource by accessing a stream and processing the result asynchronously.
	/// </summary>
	/// <param name="streamSource">The stream source that sets the SVG source value.</param>
	/// <returns>
	/// A SvgImageSourceLoadStatus value that indicates whether the operation was successful.
	/// If it failed, indicates the reason for the failure.
	/// </returns>
	public IAsyncOperation<SvgImageSourceLoadStatus> SetSourceAsync(IRandomAccessStream streamSource)
	{
		UnloadImageData();

#if __CROSSRUNTIME__
		async
#endif
		Task<SvgImageSourceLoadStatus> SetSourceAsync(CancellationToken ct)
		{
			if (streamSource == null)
			{
				//Same behavior as windows, although the documentation does not mention it!!!
				throw new ArgumentException(nameof(streamSource));
			}

#if __CROSSRUNTIME__
			_stream = streamSource.CloneStream();

			// The operation completes from its own load, not from what the source publishes: by then a handler may
			// already have given the source another stream.
			var load = EnsureLoad(out var loadToken);
			var completion = new TaskCompletionSource<SvgImageSourceLoadStatus>();

			// As on WinUI, a load that failed or was replaced completes the operation at once, and a successful one
			// completes it as it is published, after the brushes showing the source raised ImageOpened (whose handler
			// may replace it) and before the images do, on their next measure.
			_ = load.ContinueWith(
				_ =>
				{
					if (loadToken.IsCancellationRequested || !load.IsCompletedSuccessfully || load.Result.Kind == ImageDataKind.Error)
					{
						Complete();
					}
				},
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);
			using var publication = Subscribe(_ =>
			{
				if (load.IsCompleted)
				{
					Complete();
				}
			});
			InvalidateSource();

			return await completion.Task;

			void Complete()
			{
				if (!load.IsCompletedSuccessfully)
				{
					completion.TrySetException(load.Exception?.InnerException ?? new TaskCanceledException());
				}
				else if (load.Result.Error is ArgumentException invalidFormat)
				{
					// Like WinUI, which completes the operation with E_INVALIDARG: OpenFailed reports InvalidFormat, and
					// awaiting the operation throws, even when an OpenFailed handler has set another stream since.
					completion.TrySetException(new ArgumentException(invalidFormat.Message, nameof(streamSource)));
				}
				else if (loadToken.IsCancellationRequested)
				{
					// Another stream replaced this one before the operation completed, which cancels it, as on WinUI.
					completion.TrySetCanceled();
				}
				else
				{
					completion.TrySetResult(load.Result.HasData ? SvgImageSourceLoadStatus.Success : SvgImageSourceLoadStatus.Other);
				}
			}
#else
			Stream = streamSource.CloneStream().AsStream();
			StreamLoaded?.Invoke(this, EventArgs.Empty);

			return Task.FromResult(SvgImageSourceLoadStatus.Success);
#endif
		}

		return AsyncOperation.FromTask(SetSourceAsync);
	}

#if !__CROSSRUNTIME__
	internal event EventHandler? StreamLoaded;
#endif

	partial void InitPartial();

	internal void RaiseImageFailed(SvgImageSourceLoadStatus loadStatus)
		=> OpenFailed?.Invoke(this, new SvgImageSourceFailedEventArgs(loadStatus));

	internal void RaiseImageOpened()
	{
		// Like WinUI, only the first successful load of a source raises Opened: CSvgImageSource fires it while
		// m_frameIndex is 0, and nothing resets that index for an SVG.
		if (!_openedRaised)
		{
			_openedRaised = true;
			Opened?.Invoke(this, new SvgImageSourceOpenedEventArgs());
		}
	}

	internal bool UseRasterized => !double.IsNaN(RasterizePixelWidth) && !double.IsNaN(RasterizePixelHeight);

#if __CROSSRUNTIME__
	public override string ToString()
	{
		if (AbsoluteUri is { } uri)
		{
			return $"{GetType().Name}/{uri}";
		}

		if (_stream is { } stream)
		{
			return $"{GetType().Name}/{stream.GetType()}";
		}

		return $"{GetType().Name}/-empty-";
	}
#endif

	// The retained parsed SVG (from the registered ISvgRenderer); set when the markup parses.
	private ISvgDocument? _svgDocument;

	// One surface per parsed document, shared by every consumer of this source: the surface owns the document and
	// disposes it, so a re-open or an unload releases exactly one owner (see Unload).
	private CompositionSvgSurface? _svgSurface;

	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
		=> TryOpenSvgImageData(out asyncImage);

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			ImageData imageData = ImageData.Empty;

			if (AbsoluteUri is { } uri)
			{
				imageData = await ImageSourceHelpers.GetImageDataFromUriAsBytes(uri, ct);
			}

			if (!imageData.HasData && _stream is not null)
			{
				imageData = await ImageSourceHelpers.ReadFromStreamAsBytesAsync(_stream.AsStream(), ct);
			}

			return imageData;
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}
	}
}
