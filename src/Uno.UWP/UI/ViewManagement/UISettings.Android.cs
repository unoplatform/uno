using System;
using Android;
using Android.OS;
using Android.Util;
using AndroidX.AppCompat.View;
using Uno.UI;
using Settings = Android.Provider.Settings;

namespace Windows.UI.ViewManagement
{
	public partial class UISettings
	{
		public bool AnimationsEnabled
		{
			get
			{
				float duration, transition;
				if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
				{

					duration = Settings.Global.GetFloat(
								  ContextHelper.Current.ContentResolver,
								  Settings.Global.AnimatorDurationScale, 1);
					transition = Settings.Global.GetFloat(
								  ContextHelper.Current.ContentResolver,
								  Settings.Global.TransitionAnimationScale, 1);
				}
				else
				{
#pragma warning disable CS0618 // Type or member is obsolete
					duration = Settings.System.GetFloat(
								  ContextHelper.Current.ContentResolver,
								  Settings.System.AnimatorDurationScale, 1);
					transition = Settings.System.GetFloat(
								  ContextHelper.Current.ContentResolver,
								  Settings.System.TransitionAnimationScale, 1);
#pragma warning restore CS0618 // Type or member is obsolete
				}
				return duration != 0 && transition != 0;
			}
		}
	}
}
