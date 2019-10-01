using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
    public partial class DoubleAnimationUsingKeyFrames
    {
		private bool _errorReported;

		// This class is not implemented for Android
		partial void OnFrame(IValueAnimator currentAnimator)
		{
			if(!_errorReported && this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				_errorReported = true;

				this.Log().Error($"{GetType()} is not supported on this platform.");
			}
		}

		// For performance consideration, do not report each frame if we are GPU bound
		// Frame will be repported on Pause or End (cf. InitializeAnimator)
		private bool ReportEachFrame() => this.GetIsDependantAnimation() || this.GetIsDurationZero();
	}
}
