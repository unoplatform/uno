#nullable enable

using System;

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
	public static ImageData FromBytes(byte[] data) => new ImageData(data);

	private ImageData(byte[] data)
	{
		Kind = ImageDataKind.ByteArray;
		ByteArray = data ?? throw new ArgumentNullException(nameof(data));
	}

	public static ImageData FromError(Exception exception) => new ImageData(exception);

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

	public static ImageData Empty { get; } = new ImageData();

	public bool HasData => Kind != ImageDataKind.Empty && Kind != ImageDataKind.Error;

	public ImageDataKind Kind { get; }

	public Exception? Error { get; } = null;

	public byte[]? ByteArray { get; } = null;

#if __WASM__
	internal ImageSource? Source { get; } = null;

	public string? Value { get; } = null;
#elif __SKIA__
	public SkiaCompositionSurface? Value { get; } = null;
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
#if __WASM__ || __SKIA__
			_ => $"{Kind}: {Value}"
#else
			_ => $"{Kind}"
#endif
		};
}
