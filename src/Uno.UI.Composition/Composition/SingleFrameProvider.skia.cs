#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SkiaSharp;

namespace Microsoft.UI.Composition;

internal sealed class SingleFrameProvider : IFrameProvider
{
	private SKImage? _image;

	public SingleFrameProvider(SKImage image)
	{
		GC.AddMemoryPressure(image.Info.BytesSize);
		_image = image;
	}

	public SKImage? CurrentImage => _image;

	public void Dispose()
	{
		if (_image != null)
		{
			GC.RemoveMemoryPressure(_image.Info.BytesSize);
			_image.Dispose();
		}
	}
}
