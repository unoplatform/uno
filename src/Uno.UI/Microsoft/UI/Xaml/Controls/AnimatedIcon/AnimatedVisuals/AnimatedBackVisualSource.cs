using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.AnimatedVisuals
{
	// TODO Uno: This is currently a stub, as animated visuals are not properly supported
	public class AnimatedBackVisualSource : IAnimatedVisualSource, IAnimatedVisualSource2
	{
		public IReadOnlyDictionary<string, double> Markers => throw new NotImplementedException();
		public void Load() => throw new NotImplementedException();
		public Size Measure(Size availableSize) => throw new NotImplementedException();
		public void Pause() => throw new NotImplementedException();
		public void Play(double fromProgress, double toProgress, bool looped) => throw new NotImplementedException();
		public void Resume() => throw new NotImplementedException();
		public void SetColorProperty(string propertyName, Color value) => throw new NotImplementedException();
		public void SetProgress(double progress) => throw new NotImplementedException();
		public void Stop() => throw new NotImplementedException();
		public void Unload() => throw new NotImplementedException();
		public void Update(AnimatedVisualPlayer player) => throw new NotImplementedException();
	}
}
