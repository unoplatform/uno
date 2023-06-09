using System;

// Keep this using in place until UWP support is dropped.
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public interface ILottieVisualSourceProvider
	{
		Windows.UI.Xaml.Controls.IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);
		Windows.UI.Xaml.Controls.IThemableAnimatedVisualSource CreateTheamableFromLottieAsset(Uri sourceFile);
		public bool TryCreateThemableFromAnimatedVisualSource(Windows.UI.Xaml.Controls.IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource themableAnimatedVisualSource);
	}
}
