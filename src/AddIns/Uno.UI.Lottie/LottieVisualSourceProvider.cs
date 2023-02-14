using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Uno.Foundation.Extensibility;

#pragma warning disable 105
using Microsoft/*Intentional space for WinUI upgrade tool*/.UI.Xaml.Controls;
#pragma warning restore 105

[assembly: ApiExtension(typeof(ILottieVisualSourceProvider), typeof(Uno.UI.Lottie.LottieVisualSourceProvider))]

namespace Uno.UI.Lottie
{
	public class LottieVisualSourceProvider : ILottieVisualSourceProvider
	{
		public LottieVisualSourceProvider(object owner)
		{
		}

		public IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile) => new LottieVisualSource { UriSource = sourceFile };

		public IThemableAnimatedVisualSource CreateTheamableFromLottieAsset(Uri sourceFile) => new ThemableLottieVisualSource { UriSource = sourceFile };

		public bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource? themableAnimatedVisualSource)
		{
			themableAnimatedVisualSource = default;
			if (animatedVisualSource is ThemableLottieVisualSource themable)
			{
				themableAnimatedVisualSource = themable;
				return true;
			}

			if (animatedVisualSource is LottieVisualSource lottieVisualSource)
			{
				themableAnimatedVisualSource = CreateTheamableFromLottieAsset(lottieVisualSource.UriSource);
				return true;
			}

			return false;
		}
	}
}
