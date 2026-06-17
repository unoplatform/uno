using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial interface IAnimatedVisualSource
	{
		// LEGACY: pre-7.0 Uno playback hooks driven only by AnimatedVisualPlayer.legacy.cs for sources
		// that don't return an IAnimatedVisual from TryCreateAnimatedVisual (e.g. CommunityToolkit's
		// LottieVisualSource). WinUI-aligned sources — including standard LottieGen-generated
		// IAnimatedVisualSource2 output — play through the player's WinUI flow and never invoke these,
		// so they default to no-ops here instead of forcing every source to implement Uno-only members.
		// Removed together with AnimatedVisualPlayer.legacy.cs in the 7.0 breaking-change release.
		void Update(AnimatedVisualPlayer player) { }
		void Load() { }
		void Unload() { }
		void Play(double fromProgress, double toProgress, bool looped) { }
		void Stop() { }
		void Pause() { }
		void Resume() { }

		void SetProgress(double progress) { }

		// Unreachable for WinUI-flow sources (they use MeasureOverrideWinUI); legacy sources override this.
		Size Measure(Size availableSize) => default;
	}

	internal partial interface IAnimatedVisualSourceWithUri
	{
		Uri UriSource { get; set; }
	}
}
