using System;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;
using Windows.ApplicationModel.VoiceCommands;

namespace Microsoft.UI.Composition;

internal partial class CompositionBrushWrapper : CompositionBrush
{
	private CompositionBrush _wrappedBrush;

	internal CompositionBrush WrappedBrush
	{
		get => _wrappedBrush;
		set => SetProperty(ref _wrappedBrush, value);
	}

	internal CompositionBrushWrapper(CompositionBrush wrappedBrush, Compositor compositor) : base(compositor)
	{
		WrappedBrush = wrappedBrush;
	}

	internal override void Paint(SKCanvas canvas, SKRect bounds) => _wrappedBrush?.Paint(canvas, bounds);
	internal override bool CanPaint() => WrappedBrush?.CanPaint() ?? false;
}
