using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

		public Windows.UI.Xaml.Controls.IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile) => new LottieVisualSource { UriSource = sourceFile };

		public Windows.UI.Xaml.Controls.IThemableAnimatedVisualSource CreateTheamableFromLottieAsset(Uri sourceFile) => new ThemableLottieVisualSource { UriSource = sourceFile };

		public bool TryCreateThemableFromAnimatedVisualSource(Windows.UI.Xaml.Controls.IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource? themableAnimatedVisualSource)
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
