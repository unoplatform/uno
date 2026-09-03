#nullable enable
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Microsoft.UI.Composition.Interactions;

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(global::Microsoft.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker);

	double RasterizationScale { get; }

	event EventHandler? RasterizationScaleChanged;

#if __SKIA__
	void RequestNewFrame();

	/// <summary>Marks a rectangular area (root/frame coordinates) dirty so the next frame repaints it, even if no
	/// visual paints there this frame (e.g. a removed or hidden visual vacating the area).</summary>
	void AddDamage(Rect bounds);

	/// <summary>Marks an arbitrary region (root/frame coordinates) dirty for the next frame.</summary>
	void AddDamage(global::Uno.UI.Composition.Drawing.IGeometry region);
#endif
}
