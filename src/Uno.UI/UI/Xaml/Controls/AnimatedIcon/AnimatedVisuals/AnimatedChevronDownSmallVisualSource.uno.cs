using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.AnimatedVisuals;

// Uno-specific: the public IAnimatedVisualSource on Uno historically exposed legacy hooks
// (Update/Load/Pause/Play/etc.) that older animated-visual sources implemented before the
// AnimatedVisualPlayer was wired through TryCreateAnimatedVisual. This source is driven by
// AnimatedVisualPlayer's WinUI-aligned Skia path via TryCreateAnimatedVisual, so the legacy
// hooks below are kept as no-ops to satisfy the interface and remain backward-compatible.
partial class AnimatedChevronDownSmallVisualSource
{
	void IAnimatedVisualSource.Update(AnimatedVisualPlayer player) { }
	void IAnimatedVisualSource.Load() { }
	void IAnimatedVisualSource.Unload() { }
	void IAnimatedVisualSource.Play(double fromProgress, double toProgress, bool looped) { }
	void IAnimatedVisualSource.Stop() { }
	void IAnimatedVisualSource.Pause() { }
	void IAnimatedVisualSource.Resume() { }
	void IAnimatedVisualSource.SetProgress(double progress) { }
	Size IAnimatedVisualSource.Measure(Size availableSize) => availableSize;
}
