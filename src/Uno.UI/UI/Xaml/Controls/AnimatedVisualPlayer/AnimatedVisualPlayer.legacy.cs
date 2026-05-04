// LEGACY: Pre-7.0 Uno AnimatedVisualPlayer implementation.
// This file delegates the lifecycle and playback hooks to the IAnimatedVisualSource
// (Update/Load/Play/Pause/Resume/Stop/SetProgress/Measure) so that animated-visual
// sources that don't implement TryCreateAnimatedVisual (e.g. CommunityToolkit.WinUI.Lottie's
// LottieVisualSource on Skia, which uses a custom SKCanvasElement) continue to work.
//
// All members in this file are scheduled for removal in the 7.0 breaking-change release once
// Uno-flavored sources are migrated to the WinUI flow (TryCreateAnimatedVisual + IAnimatedVisual).
// At that point the AnimatedVisualPlayer port can drop the m_useWinUIFlow gate and use the WinUI
// path unconditionally.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	// LEGACY: Re-runs the source's update/load when the Source property changes.
	private void OnSourceChangedLegacy()
	{
		if (IsLoaded && Source is not null)
		{
			Source.Update(this);
			InvalidateMeasure();
		}
	}

	// LEGACY: Update + Load the source when the player loads.
	private void OnLoadedLegacy()
	{
		Source?.Update(this);
		Source?.Load();
	}

	// LEGACY: Unload the source when the player unloads.
	private void OnUnloadedLegacy()
	{
		Source?.Unload();
	}

	// LEGACY: Defer to the source for size hints, falling back to first-child measurement.
	private Size MeasureOverrideLegacy(Size availableSize)
	{
		var measured = Source?.Measure(availableSize);
		if (measured is { } measuredSize)
		{
			return measuredSize;
		}

		return MeasureFirstChild(availableSize);
	}

	// LEGACY: Arrange the first child (e.g. fallback content) within finalSize.
	private Size ArrangeOverrideLegacy(Size finalSize) => ArrangeFirstChild(finalSize);
}
