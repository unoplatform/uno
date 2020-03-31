using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class RenderingLoopFloatAnimator : RenderingLoopAnimator<float>
	{
		public RenderingLoopFloatAnimator(float from, float to) : base(from, to)
		{
		}

		protected override float GetUpdatedValue(long frame, float from, float to) => (float)_easing.Ease(frame, from, to, Duration);
	}
}
