using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.UI.Dispatching;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal abstract class DispatcherAnimator<T> : CPUBoundAnimator<T> where T : struct
	{
		public const int DefaultFrameRate = 60;

		public DispatcherAnimator(T from, T to, int frameRate = 0)
			: base(from, to)
		{
		}

		protected override void EnableFrameReporting() => CompositionTarget.Rendering += base.OnFrame;
		protected override void DisableFrameReporting() => CompositionTarget.Rendering -= base.OnFrame;

		protected abstract override T GetUpdatedValue(long frame, T from, T to);
	}
}
