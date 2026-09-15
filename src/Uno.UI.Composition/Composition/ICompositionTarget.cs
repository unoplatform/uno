#nullable enable
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Microsoft.UI.Composition.Interactions;
using SkiaSharp;

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(Microsoft.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker);

	double RasterizationScale { get; }

	event EventHandler? RasterizationScaleChanged;

	/// <summary>Raised once per presented frame, before layout and before the record.</summary>
	event EventHandler<long>? FrameStarting;

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	long FrameIntervalInTicks { get; }

	void RequestNewFrame();

	void AddDamage(SKRect bounds);

	void AddDamage(SKPath region);
}
