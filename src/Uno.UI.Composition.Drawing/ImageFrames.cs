#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of decoding an image (or wrapping raw pixels) through <see cref="IImageEncoderDecoder"/>: one or more
/// decoded <see cref="IImage"/> frames in display order, plus their per-frame durations (a still image has one
/// frame; an animated GIF/APNG/WebP has several). Since <see cref="IImage"/> is not itself disposable, this type
/// owns the frames — disposing it releases any frame that is <see cref="IDisposable"/>.
/// </summary>
public sealed class ImageFrames : IDisposable
{
	private bool _disposed;

	public ImageFrames(IReadOnlyList<IImage> frames, IReadOnlyList<int> durationsMs)
	{
		if (frames is null || frames.Count == 0)
		{
			throw new ArgumentException("Image frames must contain at least one frame.", nameof(frames));
		}

		if (durationsMs is null || durationsMs.Count != frames.Count)
		{
			throw new ArgumentException("Durations must be non-null and the same length as frames.", nameof(durationsMs));
		}

		Frames = frames;
		DurationsMs = durationsMs;
	}

	/// <summary>The decoded frames, in display order. Never empty.</summary>
	public IReadOnlyList<IImage> Frames { get; }

	/// <summary>Per-frame display duration in milliseconds; same length as <see cref="Frames"/>.</summary>
	public IReadOnlyList<int> DurationsMs { get; }

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		foreach (var frame in Frames)
		{
			(frame as IDisposable)?.Dispose();
		}
	}
}
