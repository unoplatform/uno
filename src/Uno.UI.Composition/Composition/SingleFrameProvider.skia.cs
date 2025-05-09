#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal sealed class SingleFrameProvider : IFrameProvider
{
	private SKImage? _image;
	private int _bytes;
	private int _disposed;

	public SingleFrameProvider(SKImage image)
	{
		_image = image;
		_bytes = _image.Info.BytesSize;
		GC.AddMemoryPressure(_bytes);
	}

	public SKImage? CurrentImage => _image;

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_image?.Dispose();
			GC.RemoveMemoryPressure(_bytes);
		}
	}

	~SingleFrameProvider()
	{
		Dispose();
	}
}
