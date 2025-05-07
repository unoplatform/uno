using System;

// Keep this using in place until UWP support is dropped.
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public interface ILottieVisualSourceProvider
	{
		IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);
		IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile);
		public bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource themableAnimatedVisualSource);
	}
}
