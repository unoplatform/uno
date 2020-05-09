using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.UI.Extensions
{
    public static class ToolbarExtensions
    {
		public static Android.Graphics.Color GetTitleTextColor(this AndroidX.AppCompat.Widget.Toolbar toolbar)
		{
			try
			{
				if (toolbar.TitleFormatted == null)
				{
					// This forces the initialization of the inner mTitleTextView variable
					toolbar.TitleFormatted = new Java.Lang.String(" "); // Must be non-empty
					toolbar.TitleFormatted = null;
				}

				var field = toolbar.Class.GetDeclaredField("mTitleTextView");
				field.Accessible = true;
				var mTitleTextView = (Android.Widget.TextView)field.Get(toolbar);
				if (mTitleTextView != null)
				{
					return new Android.Graphics.Color(mTitleTextView.CurrentTextColor);
				}
			}
			catch (Exception exception)
			{
				if (toolbar.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
				{
					toolbar.Log().Warn("Failed to get Toolbar's TitleTextColor.", exception);
				}
			}

			return new Android.Graphics.Color(0);
		}
	}
}
