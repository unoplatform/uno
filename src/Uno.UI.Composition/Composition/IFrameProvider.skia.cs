#nullable enable

using System;
using SkiaSharp;

namespace Windows.UI.Composition;

internal interface IFrameProvider : IDisposable
{
	SKImage? CurrentImage { get; }
}
