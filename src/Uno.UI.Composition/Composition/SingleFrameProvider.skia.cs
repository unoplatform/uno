#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

internal sealed class SingleFrameProvider : IFrameProvider
{
	private readonly IImageFrames _frames;
	private readonly long _bytes;
	private bool _disposed;
	private readonly object _lock = new();

	public SingleFrameProvider(IImageFrames frames)
	{
		_frames = frames;
		var image = frames.Frames[0];
		_bytes = (long)image.PixelWidth * image.PixelHeight * 4;
		// https://github.com/unoplatform/uno/issues/20285
		GC.AddMemoryPressure(_bytes);
	}

	public IImage? CurrentImage => _frames.Frames[0];

	public void Dispose()
	{
		lock (_lock)
		{
			if (!_disposed)
			{
				_disposed = true;
				_frames.Dispose();
				GC.RemoveMemoryPressure(_bytes);
			}
		}
	}
}
