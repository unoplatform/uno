using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer;
using Uno.Foundation.Extensibility;

[assembly: ApiExtension(typeof(ILottieVisualSourceProvider), typeof(Uno.UI.Lottie.LottieVisualSourceProvider))]

namespace Uno.UI.Lottie
{
	public class LottieVisualSourceProvider : ILottieVisualSourceProvider
	{
		public LottieVisualSourceProvider(object owner)
		{
		}

		public IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile) => new LottieVisualSource {UriSource = sourceFile};
	}
}
