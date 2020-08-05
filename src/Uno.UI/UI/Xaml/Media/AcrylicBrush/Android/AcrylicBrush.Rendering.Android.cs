/*

Implementation based on https://github.com/roubachof/Sharpnado.MaterialFrame.
with some modifications and removal of unused features.

*/

#if __ANDROID_29__
using AndroidX.Core.View;
#else
using Android.Support.V4.View;
#endif
using System;
using System.ComponentModel;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Java.IO;
using Android.Graphics;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using Uno.UI.Controls;
using Uno.UI;
using Android.Views;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Renderer to update all frames with better shadows matching material design standards.
	/// </summary>	
	public partial class AcrylicBrush
	{
		private void UpdateProperties(AcrylicState state)
		{
			UpdateCornerRadius();
			UpdateTint(state);
		}

		private void UpdateCornerRadius()
		{
			//TODO
			//_acrylicLayer?.SetCornerRadius(ContextHelper.Current.ToPixels(MaterialFrame.CornerRadius));
			//_realtimeBlurView?.SetCornerRadius(ContextHelper.Current.ToPixels(MaterialFrame.CornerRadius));
		}

		private void ApplyAcrylicBlur(AcrylicState state)
		{
			ApplyBackground(state);

			EnableBlur(state);

			UpdateProperties(state);
		}

		private void ApplyBackground(AcrylicState state)
		{
			if (state.BackgroundDrawable == null)
			{
				state.BackgroundDrawable = new GradientDrawable();
				state.BackgroundDrawable.SetShape(ShapeType.Rectangle);
				Android.Graphics.Color androidColor = Colors.Transparent;

				state.BackgroundDrawable.SetColor(androidColor);

				SetBackground(state.Owner, state.BackgroundDrawable);				 
			}
		}

		private void RemoveAcrylicBlur(AcrylicState state)
		{
			SetBackground(state.Owner, null);
			state.BackgroundDrawable?.Dispose();
			state.BackgroundDrawable = null;

			DisableBlur(state);
			DestroyBlur(state);
		}

		private void SetBackground(BindableView view, Drawable drawable)
		{
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{
#pragma warning disable 618 // Using older method for compatibility with API 15
				view.SetBackgroundDrawable(drawable);
#pragma warning restore 618
			}
			else
			{
				view.Background = drawable;
			}
		}
	}
}
