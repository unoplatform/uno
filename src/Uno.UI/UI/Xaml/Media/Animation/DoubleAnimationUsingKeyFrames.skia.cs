using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}
	}
}
