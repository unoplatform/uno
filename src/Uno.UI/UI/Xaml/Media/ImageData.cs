#nullable enable

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

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

#if __SKIA__
	// Neutral surface (ICompositionSurface): a texture-backed CompositionImageSurface OR a live self-painting one
	// (e.g. CompositionSvgSurface). Consumers that need texture specifics down-cast.
	public static ImageData FromCompositionSurface(ICompositionSurface compositionSurface) => new(compositionSurface);

	private ImageData(ICompositionSurface compositionSurface)
	{
		Kind = ImageDataKind.CompositionSurface;
		CompositionSurface = compositionSurface;
	}
#endif

	public static ImageData Empty { get; }

	public bool HasData => Kind != ImageDataKind.Empty && Kind != ImageDataKind.Error;

	public ImageDataKind Kind { get; }

	public Exception? Error { get; } = null;

	public byte[]? ByteArray { get; } = null;

#if __SKIA__
	public ICompositionSurface? CompositionSurface { get; } = null;
#endif

	public override string ToString() =>
		Kind switch
		{
			ImageDataKind.Empty => "Empty",
			ImageDataKind.Error => $"Error[{Error}]",
			ImageDataKind.ByteArray => $"Byte array: Length {ByteArray?.Length ?? -1}",
#if __SKIA__
			ImageDataKind.CompositionSurface => $"CompositionSurface: {CompositionSurface}",
#endif
			_ => $"{Kind}"
		};
}
