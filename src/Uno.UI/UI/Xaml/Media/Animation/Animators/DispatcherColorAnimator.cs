using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal sealed class DispatcherColorAnimator(ColorOffset from, ColorOffset to, int frameRate = DispatcherAnimator<ConsoleColor>.DefaultFrameRate)
		: DispatcherAnimator<ColorOffset>(from, to, frameRate)
	{
		protected override ColorOffset GetUpdatedValue(long frame, ColorOffset from, ColorOffset to) =>
			ColorOffset.FromArgb(
				(int)Math.Round(_easing.Ease(frame, from.A, to.A, Duration)),
				(int)Math.Round(_easing.Ease(frame, from.R, to.R, Duration)),
				(int)Math.Round(_easing.Ease(frame, from.G, to.G, Duration)),
				(int)Math.Round(_easing.Ease(frame, from.B, to.B, Duration)));
	}
}
