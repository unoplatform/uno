#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IImageFrames"/> owning the decoded <see cref="SKImage"/> frames.</summary>
internal sealed class SkiaImageFrames : IImageFrames
{
	private readonly SkiaImage[] _frames;
	private bool _disposed;

	public SkiaImageFrames(SKImage[] images, int[] durationsMs)
	{
		_frames = new SkiaImage[images.Length];
		for (var i = 0; i < images.Length; i++)
		{
			_frames[i] = new SkiaImage(images[i]);
		}

		DurationsMs = durationsMs;
	}

	public static SkiaImageFrames FromImage(SKImage image) => new(new[] { image }, new[] { 0 });

	public IReadOnlyList<IImage> Frames => _frames;

	public IReadOnlyList<int> DurationsMs { get; }

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		foreach (var frame in _frames)
		{
			frame.Image.Dispose();
		}
	}
}
