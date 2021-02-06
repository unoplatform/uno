using System;

// Keep this using in place until UWP support is dropped.
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public interface ILottieVisualSourceProvider
	{
		IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);
		IThemableAnimatedVisualSource CreateTheamableFromLottieAsset(Uri sourceFile);
		public bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource themableAnimatedVisualSource);
	}
}
