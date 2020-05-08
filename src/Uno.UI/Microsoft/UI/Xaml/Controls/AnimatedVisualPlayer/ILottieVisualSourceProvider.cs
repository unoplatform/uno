using System;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer
{
	public interface ILottieVisualSourceProvider
	{
		IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);
	}
}
