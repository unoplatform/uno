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
using Windows.Application­Model;
using Microsoft.UI.Composition;
using SkiaSharp;
using System.Reflection;

namespace Microsoft.UI.Xaml.Media.Imaging;

/// <summary>
/// Provides a source object for properties that use a Scalable Vector Graphics (SVG) source. You can define a SvgImageSource
/// by using a Uniform Resource Identifier (URI) that references a SVG file, or by calling SetSourceAsync(IRandomAccessStream)
/// and supplying a stream.
/// </summary>
public partial class SvgImageSource : ImageSource
{
	private SvgImageSourceLoadStatus? _lastStatus;

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
#if __SKIA__
		InitSvgProvider();
#endif
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

			_lastStatus = null;

#if __CROSSRUNTIME__
			_stream = streamSource.CloneStream();

			var tcs = new TaskCompletionSource<SvgImageSourceLoadStatus>();

			using var x = Subscribe(OnChanged);


			InvalidateSource();

			return await tcs.Task;

			void OnChanged(ImageData data)
			{
				tcs.TrySetResult(_lastStatus ?? SvgImageSourceLoadStatus.Other);
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
	{
		_lastStatus = loadStatus;
		OpenFailed?.Invoke(this, new SvgImageSourceFailedEventArgs(loadStatus));
	}

	internal void RaiseImageOpened()
	{
		_lastStatus = SvgImageSourceLoadStatus.Success;
		Opened?.Invoke(this, new SvgImageSourceOpenedEventArgs());
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

#nullable disable
	private static MethodInfo _fromPictureMethod;

	private protected unsafe override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		if (TryOpenSvgImageData(ct, out var imageTask))
		{
			asyncImage = imageTask.ContinueWith(task =>
			{
				var imageData = task.Result;
				if (imageData is { Kind: ImageDataKind.ByteArray, ByteArray: not null } &&
					_svgProvider?.TryGetLoadedDataAsPictureAsync() is SKPicture picture)
				{
					var sourceSize = _svgProvider.SourceSize;

					_fromPictureMethod ??= typeof(SKImage).GetMethod(
						"FromPicture",
						BindingFlags.NonPublic | BindingFlags.Static,
						new[] {
							typeof(SKPicture),
							typeof(SKSizeI),
							typeof(SKMatrix).MakePointerType(),
							typeof(SKPaint),
							typeof(bool),
							typeof(SKColorSpace),
							typeof(SKSurfaceProperties) });

					if (_fromPictureMethod is null)
					{
						throw new InvalidOperationException("Unable to find the 'FromPicture' method on SKImage");
					}

					var matrix = SKMatrix.Identity;

					var skImage = (SKImage)_fromPictureMethod.Invoke(
						null,
						[
							picture,
							new SKSizeI((int)sourceSize.Width, (int)sourceSize.Height),
							Pointer.Box(&matrix, typeof(SKMatrix*)),
							new SKPaint(),
							false,
							SKColorSpace.CreateSrgb(),
							new SKSurfaceProperties(SKPixelGeometry.Unknown)
					]);

					return ImageData.FromCompositionSurface(new(skImage));
				}
				else
				{
					return ImageData.Empty;
				}
			}, ct);
			return true;
		}
		else
		{
			asyncImage = Task.FromResult(ImageData.Empty);
			return false;
		}
	}

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
#nullable enable
#endif
}
