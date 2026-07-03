using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	// The only member of IAnimatedVisualSource is the WinUI TryCreateAnimatedVisual (declared in the
	// generated partial). The pre-7.0 Uno-legacy playback hooks (Update/Load/Play/…) were removed so a
	// standard WinUI/LottieGen source that only implements TryCreateAnimatedVisual satisfies the contract.
	internal partial interface IAnimatedVisualSourceWithUri
	{
		Uri UriSource { get; set; }
	}
}
