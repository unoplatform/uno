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
	/// <summary>
	/// The backend this target presents through. Recording and texture creation go through it rather than the
	/// process-wide <see cref="global::Uno.UI.Composition.Drawing.DrawingFactory.Current"/>, which holds whichever
	/// window registered LAST -- with two windows open that is the wrong device for one of them.
	/// </summary>
	global::Uno.UI.Composition.Drawing.IDrawingFactory? Renderer { get; }

	void RequestNewFrame();

	/// <summary>Raised once per frame, before layout and before the record, with the frame's timestamp.</summary>
	event EventHandler<long>? FrameStarting
	{
		add { }
		remove { }
	}

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	long FrameIntervalInTicks => TimeSpan.TicksPerSecond / 60;

	/// <summary>Marks a rectangular area (root/frame coordinates) dirty so the next frame repaints it, even if no
	/// visual paints there this frame (e.g. a removed or hidden visual vacating the area).</summary>
	void AddDamage(Rect bounds);

	/// <summary>Marks an arbitrary region (root/frame coordinates) dirty for the next frame.</summary>
	void AddDamage(global::Uno.UI.Composition.Drawing.IGeometry region);
#endif
}
