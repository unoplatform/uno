using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimatedVisuals
{
	// TODO Uno: This is currently a stub, as animated visuals are not properly supported
	public partial class AnimatedSettingsVisualSource : IAnimatedVisualSource2
	{
		public IReadOnlyDictionary<string, double> Markers => new Dictionary<string, double>();
		public void Load() { }
		public Size Measure(Size availableSize) => availableSize;
		public void Pause() { }
		public void Play(double fromProgress, double toProgress, bool looped) { }
		public void Resume() { }
		public void SetColorProperty(string propertyName, Color value) { }
		public void SetProgress(double progress) { }
		public void Stop() { }
		public void Unload() { }
		public void Update(AnimatedVisualPlayer player) { }
		public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics) { diagnostics = new object(); return null; }
	}
}
