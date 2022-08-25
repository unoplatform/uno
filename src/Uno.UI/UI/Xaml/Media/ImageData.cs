#nullable enable

using System;
using Windows.UI.Composition;

#if __IOS__
using _UIImage = UIKit.UIImage;
#elif __MACOS__
using _UIImage = AppKit.NSImage;
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
#endif

#if __SKIA__
	public static ImageData FromCompositionSurface(SkiaCompositionSurface compositionSurface) => new(compositionSurface);

	private ImageData(SkiaCompositionSurface compositionSurface)
	{
		Kind = ImageDataKind.CompositionSurface;
		CompositionSurface = compositionSurface;
	}
#endif

	public static ImageData Empty { get; } = new ImageData();

	public bool HasData => Kind != ImageDataKind.Empty && Kind != ImageDataKind.Error;

	public ImageDataKind Kind { get; }

	public Exception? Error { get; } = null;

	public byte[]? ByteArray { get; } = null;

#if __WASM__
	internal ImageSource? Source { get; } = null;

	public string? Value { get; } = null;
#elif __SKIA__
	public SkiaCompositionSurface? CompositionSurface { get; } = null;
#elif __IOS__ || __MACOS__
	public _UIImage? NativeImage { get; } = null;
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
			ImageDataKind.Value => $"Value: {Value}",
#endif
			_ => $"{Kind}"
		};
}
