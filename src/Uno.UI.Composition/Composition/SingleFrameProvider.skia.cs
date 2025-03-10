#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SkiaSharp;

namespace Windows.UI.Composition;

internal sealed class SingleFrameProvider : IFrameProvider
{
	private SKImage? _image;

	public SingleFrameProvider(SKImage image)
	{
		_image = image;
	}

	public SKImage? CurrentImage => _image;

	public void Dispose()
	{
		_image?.Dispose();
		_image = null;
	}
}
