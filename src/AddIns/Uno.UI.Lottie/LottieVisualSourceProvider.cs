using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Foundation.Extensibility;

#if HAS_UNO_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

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

		public IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile) => new ThemableLottieVisualSource { UriSource = sourceFile };

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
				themableAnimatedVisualSource = CreateThemableFromLottieAsset(lottieVisualSource.UriSource);
				return true;
			}

			return false;
		}
	}
}
