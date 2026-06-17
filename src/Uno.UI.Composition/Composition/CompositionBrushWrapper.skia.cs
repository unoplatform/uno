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

	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds) => _wrappedBrush?.Paint(canvas, opacity, bounds);
	internal override bool CanPaint() => WrappedBrush?.CanPaint() ?? false;

	// Forward to the wrapped brush so that effect brushes (e.g. a backdrop-blur acrylic) keep their
	// per-frame-repaint characteristic through the wrapper; otherwise the owning visual sees the base
	// default (false) and dirty-rectangles rendering would freeze the effect's cached picture.
	internal override bool RequiresRepaintOnEveryFrame => WrappedBrush?.RequiresRepaintOnEveryFrame ?? false;
}
