using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
	public static partial class TimelineExtensions
	{
		internal static string GetTimelineTargetFullName(this Timeline t)
		{
			return Storyboard.GetTargetName(t) + "." + Storyboard.GetTargetProperty(t);
		}

		internal static bool GetIsDurationZero(this Timeline timeline)
		{
			return timeline.GetCalculatedDuration() == TimeSpan.Zero;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is a dependent animation.
		/// 
		/// https://msdn.microsoft.com/en-uS/office/office365/jj819807.aspx#dependent
		/// </summary>
		/// <value><c>true</c> if this instance is dependent animation; otherwise, <c>false</c>.</value>
		internal static bool GetIsDependantAnimation(this Timeline timeline)
		{
			if (timeline.GetIsDurationZero() || !timeline.IsTargetPropertyDependant())
			{
				//is not dependent if the target is a transform or duration is zero
				return false;
			}

			if (timeline.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				timeline.Log().Debug("This Dependent animation will not run, EnableDependentAnimation is set to false");
			}

			return true;//is dependent
		}

		/// <summary>
		/// Animation should be hardware-bound if it is neither dependent, nor zero-duration.
		/// </summary>
		internal static bool GetIsHardwareAnimated(this Timeline timeline) => !timeline.GetIsDependantAnimation() && !timeline.GetIsDurationZero();
	}
}
