#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using SkiaSharp;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Composition;

internal sealed class SingleFrameProvider : IFrameProvider
{
	private readonly SKImage _image;
	private readonly int _bytes;
	private bool _disposed;

	public SingleFrameProvider(SKImage image)
	{
		_image = image;
		_bytes = _image.Info.BytesSize;
		// https://github.com/unoplatform/uno/issues/20285
		GC.AddMemoryPressure(_bytes);
	}

	public SKImage? CurrentImage => _image;

	public void Dispose()
	{
		lock (this)
		{
			if (!_disposed)
			{
				_disposed = true;
				_image.Dispose();
				GC.RemoveMemoryPressure(_bytes);
			}
			else
			{
				this.LogError()?.Error("Detected a double dispose.");
			}
		}
	}

	~SingleFrameProvider()
	{
		Dispose();
	}
}
