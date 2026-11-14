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
#if __CROSSRUNTIME__
		// Like WinUI, a new stream cancels the operation still pending for the previous one.
		_setSourceOperation?.Supersede();
#endif
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
			var operation = new SetSourceOperation(this, load, loadToken);
			InvalidateSource();

			return await operation.Task;
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

#if __CROSSRUNTIME__
	// The pending SetSourceAsync of the current load, which completes when that load has been published.
	private SetSourceOperation? _setSourceOperation;

	private protected override void OnPublished() => _setSourceOperation?.OnPublished();

	/// <summary>
	/// One SetSourceAsync: pending until its load fails, is replaced (another stream or URI, or the source is disposed),
	/// or has been published to every element showing the source.
	/// </summary>
	private sealed class SetSourceOperation
	{
		private readonly TaskCompletionSource<SvgImageSourceLoadStatus> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly SvgImageSource _owner;
		private readonly Task<ImageData> _load;
		private readonly CancellationToken _loadToken;
		private bool _superseded;

		public SetSourceOperation(SvgImageSource owner, Task<ImageData> load, CancellationToken loadToken)
		{
			(_owner, _load, _loadToken) = (owner, load, loadToken);
			owner._setSourceOperation = this;

			// A replacement settles once the call that made it has returned: the source has been reset by then, and a
			// load that set another stream from its OpenFailed handler has completed, failed.
			load.ContinueWith(_ => Settle(published: false), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			loadToken.Register(() => owner.DispatcherQueue.TryEnqueue(() => Settle(published: false)));
		}

		public Task<SvgImageSourceLoadStatus> Task => _completion.Task;

		public void Supersede() => _superseded = true;

		// Like WinUI, a successful operation completes after the brushes showing the source raised ImageOpened, whose
		// handler may replace it, and before images do, on their next measure.
		public void OnPublished()
		{
			if (_load.IsCompleted)
			{
				Settle(published: true);
			}
		}

		// The only way out of pending, taken once: a successful load waits for its publication.
		private void Settle(bool published)
		{
			if (_load.IsCompletedSuccessfully && _load.Result.Error is ArgumentException invalidFormat)
			{
				// Like WinUI (E_INVALIDARG), even when an OpenFailed handler has set another stream since.
				_completion.TrySetException(new ArgumentException(invalidFormat.Message));
			}
			else if (_load.IsFaulted)
			{
				_completion.TrySetException(_load.Exception!.InnerException!);
			}
			else if (!_load.IsCompleted || _loadToken.IsCancellationRequested)
			{
				// Replaced. Like WinUI, another SetSourceAsync cancels the operation, while a new UriSource (or the
				// source's disposal) completes it.
				if (_superseded)
				{
					_completion.TrySetCanceled();
				}
				else
				{
					_completion.TrySetResult(SvgImageSourceLoadStatus.Success);
				}
			}
			else if (published)
			{
				_completion.TrySetResult(_load.Result.HasData ? SvgImageSourceLoadStatus.Success : SvgImageSourceLoadStatus.Other);
			}
			else
			{
				return;
			}

			if (_owner._setSourceOperation == this)
			{
				_owner._setSourceOperation = null;
			}
		}
	}
#endif

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
