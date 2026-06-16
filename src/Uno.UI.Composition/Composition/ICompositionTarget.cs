#nullable enable
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Microsoft.UI.Composition.Interactions;
#if __SKIA__
using SkiaSharp;
#endif

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker);

	double RasterizationScale { get; }

	event EventHandler? RasterizationScaleChanged;

#if __SKIA__
	void RequestNewFrame();

	/// <summary>
	/// Adds a changed region (in root/logical coordinates) to the current frame's dirty region,
	/// for dirty-rectangles rendering.
	/// </summary>
	void AddDamage(SKRect bounds);

	/// <summary>
	/// Marks the entire surface as dirty for the current frame (e.g. on resize), forcing a full repaint.
	/// </summary>
	void AddFullFrameDamage();
#endif
}
