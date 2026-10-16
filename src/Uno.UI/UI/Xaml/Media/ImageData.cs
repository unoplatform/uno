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

	/// <summary>
	/// Whether the data is shared by every source that loads the same cached key, in which case no single source may
	/// release it. Travels with the result so a cancelled or superseded open releases exactly what it owned.
	/// </summary>
	public bool IsShared => Share is not null;

	/// <summary>
	/// The decode this data is shared through, which releases it once nothing displays it.
	/// </summary>
	public SharedImageDecode? Share { get; private init; }

	/// <summary>
	/// Returns this data marked as shared through <paramref name="share"/>.
	/// </summary>
	internal ImageData AsShared(SharedImageDecode share) => this with { Share = share };

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
