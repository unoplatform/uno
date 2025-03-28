#nullable enable

using System;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;

#if __IOS__
using _UIImage = UIKit.UIImage;
#elif __MACOS__
using _UIImage = AppKit.NSImage;
#elif __ANDROID__
using Android.Graphics;
#endif

namespace Uno.UI.Xaml.Media;

/// <summary>
/// Represents the raw data of an **opened** image source
/// </summary>
internal partial struct ImageData
{
	public static ImageData FromBytes(byte[] data) => new(data);

	private ImageData(byte[] data)
	{
		Kind = ImageDataKind.ByteArray;
		ByteArray = data ?? throw new ArgumentNullException(nameof(data));
	}

	public static ImageData FromError(Exception exception) => new(exception);

	private ImageData(Exception exception)
	{
		Kind = ImageDataKind.Error;
		Error = exception ?? throw new ArgumentNullException(nameof(exception));
	}

#if __IOS__ || __MACOS__
	public static ImageData FromNative(_UIImage uiImage) => new ImageData(uiImage);

	private ImageData(_UIImage uiImage)
	{
		Kind = ImageDataKind.NativeImage;
		NativeImage = uiImage ?? throw new ArgumentNullException(nameof(uiImage));
	}
#elif __SKIA__
	public static ImageData FromCompositionSurface(SkiaCompositionSurface compositionSurface) => new(compositionSurface);

	private ImageData(SkiaCompositionSurface compositionSurface)
	{
		Kind = ImageDataKind.CompositionSurface;
		CompositionSurface = compositionSurface;
	}
#elif __WASM__
	public static ImageData FromDataUri(string dataUri) => new ImageData(ImageDataKind.DataUri, dataUri);

	public static ImageData FromUrl(Uri url, ImageSource source) => new ImageData(url.ToString(), source);

	public static ImageData FromUrl(string url, ImageSource source) => new ImageData(url, source);

	private ImageData(ImageDataKind kind, string value)
	{
		Kind = kind;
		Value = value;
	}

	private ImageData(string url, ImageSource source)
	{
		Kind = ImageDataKind.Url;
		Value = url;
		Source = source;
	}
#elif __ANDROID__
	public static ImageData FromBitmap(Bitmap? bitmap)
	{
		if (bitmap is null)
		{
			return ImageData.Empty;
		}

		return new ImageData(bitmap);
	}

	private ImageData(Bitmap bitmap)
	{
		Kind = ImageDataKind.NativeImage;
		Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
	}
#endif

	public static ImageData Empty { get; }

	public bool HasData => Kind != ImageDataKind.Empty && Kind != ImageDataKind.Error;

	public ImageDataKind Kind { get; }

	public Exception? Error { get; } = null;

	public byte[]? ByteArray { get; } = null;

#if __IOS__ || __MACOS__
	public _UIImage? NativeImage { get; } = null;
#elif __SKIA__
	public SkiaCompositionSurface? CompositionSurface { get; } = null;
#elif __WASM__
	internal ImageSource? Source { get; } = null;

	public string? Value { get; } = null;
#elif __ANDROID__
	public Bitmap? Bitmap { get; } = null;
#endif

	public override string ToString() =>
		Kind switch
		{
			ImageDataKind.Empty => "Empty",
			ImageDataKind.Error => $"Error[{Error}]",
			ImageDataKind.ByteArray => $"Byte array: Length {ByteArray?.Length ?? -1}",
#if __IOS__ || __MACOS__
			ImageDataKind.NativeImage => $"Native UIImage: {NativeImage}",
#endif
#if __SKIA__
			ImageDataKind.CompositionSurface => $"CompositionSurface: {CompositionSurface}",
#endif
#if __WASM__
			ImageDataKind.DataUri => $"DataUri: {Value}",
			ImageDataKind.Url => $"Url: {Value}, Source: {Source}",
#endif
			_ => $"{Kind}"
		};
}
