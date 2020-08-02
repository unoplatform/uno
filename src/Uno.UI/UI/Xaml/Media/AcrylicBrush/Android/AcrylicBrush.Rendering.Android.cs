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
		private GradientDrawable _mainDrawable;
		private GradientDrawable _acrylicLayer;

		protected void UpdateProperties()
		{
			UpdateCornerRadius();
			UpdateAcrylicGlowColor();
			UpdateAndroidBlurOverlayColor();
			UpdateAndroidBlurRadius();
			//UpdateAndroidBlurRootElement();
			UpdateMaterialBlurStyle();
		}

		protected void Cleanup(BindableView view)
		{
			//TODO:
			//MaterialFrame?.Unsubscribe();
			_mainDrawable = null;

			DestroyBlur(view);

			_acrylicLayer?.Dispose();
			_acrylicLayer = null;

			_mainDrawable = null;

			_acrylicLayer?.Dispose();
			_acrylicLayer = null;
		}

		private void UpdateCornerRadius()
		{
			//TODO
			//_acrylicLayer?.SetCornerRadius(ContextHelper.Current.ToPixels(MaterialFrame.CornerRadius));
			//_realtimeBlurView?.SetCornerRadius(ContextHelper.Current.ToPixels(MaterialFrame.CornerRadius));
		}

		private void UpdateElevation(BindableView view)
		{
			ViewCompat.SetElevation(view, 0);
		}

		private void UpdateAcrylicGlowColor()
		{
			Android.Graphics.Color androidColor = TintColor;
			_acrylicLayer?.SetColor(androidColor);
		}

		private void SetAcrylicBlur(BindableView view)
		{
			Border b = (Border)(view);
			_mainDrawable = new GradientDrawable();
			_mainDrawable.SetShape(ShapeType.Rectangle);

			Android.Graphics.Color androidColor = Colors.Transparent;
			_mainDrawable.SetColor(androidColor);

			SetBackground(view, _mainDrawable);

			//view.LayoutChange += (s,e)=> LayoutBlurView(view);
			UpdateElevation(view);
			//LayoutBlurView(view);

			EnableBlur(view);
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
