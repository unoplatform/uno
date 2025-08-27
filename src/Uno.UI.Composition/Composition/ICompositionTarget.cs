#nullable enable
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Microsoft.UI.Composition.Interactions;

namespace Uno.UI.Composition;

internal interface ICompositionTarget
{
	void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker);

	double RasterizationScale { get; }

	event EventHandler? RasterizationScaleChanged;

#if __SKIA__
	void RequestNewFrame();
#endif
}
