using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial interface IAnimatedVisualSource
	{
		void Update(AnimatedVisualPlayer player);
		void Load();
		void Unload();
		void Play(double fromProgress, double toProgress, bool looped);
		void Stop();
		void Pause();
		void Resume();

		void SetProgress(double progress);

		Size Measure(Size availableSize);
	}

	internal partial interface IAnimatedVisualSourceWithUri
	{
		Uri UriSource { get; set; }
	}
}
