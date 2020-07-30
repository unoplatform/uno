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

namespace Uno.UI.Xaml.Media.Acrylic
{
	/// <summary>
	/// Renderer to update all frames with better shadows matching material design standards.
	/// </summary>
	[Preserve]
	public partial class AndroidMaterialFrameRenderer
	{
		private GradientDrawable _mainDrawable;

		private GradientDrawable _acrylicLayer;

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(MaterialFrame.CornerRadius):
					UpdateCornerRadius();
					base.OnElementPropertyChanged(sender, e);
					break;

				case nameof(MaterialFrame.Elevation):
					UpdateElevation();
					break;

				case nameof(MaterialFrame.LightThemeBackgroundColor):
					UpdateLightThemeBackgroundColor();
					break;

				case nameof(MaterialFrame.AcrylicGlowColor):
					UpdateAcrylicGlowColor();
					break;

				case nameof(MaterialFrame.AndroidBlurOverlayColor):
					UpdateAndroidBlurOverlayColor();
					break;

				case nameof(MaterialFrame.AndroidBlurRadius):
					UpdateAndroidBlurRadius();
					break;

				case nameof(MaterialFrame.AndroidBlurRootElement):
					UpdateAndroidBlurRootElement();
					break;

				case nameof(MaterialFrame.MaterialTheme):
					UpdateMaterialTheme();
					break;

				case nameof(MaterialFrame.MaterialBlurStyle):
					UpdateMaterialBlurStyle();
					break;

				default:
					base.OnElementPropertyChanged(sender, e);
					break;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				MaterialFrame?.Unsubscribe();
				Destroy();

				_mainDrawable = null;

				_acrylicLayer?.Dispose();
				_acrylicLayer = null;
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
		{
			base.OnElementChanged(e);

			((MaterialFrame)e.OldElement)?.Unsubscribe();
			Destroy();

			if (e.NewElement == null)
			{
				return;
			}

			_mainDrawable = (GradientDrawable)Background;

			UpdateMaterialTheme();
		}

		private void Destroy()
		{
			_mainDrawable = null;

			DestroyBlur();

			_acrylicLayer?.Dispose();
			_acrylicLayer = null;
		}

		private void UpdateCornerRadius()
		{
			_acrylicLayer?.SetCornerRadius(Context.ToPixels(MaterialFrame.CornerRadius));
			_realtimeBlurView?.SetCornerRadius(Context.ToPixels(MaterialFrame.CornerRadius));
		}

		private void UpdateElevation()
		{
			if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
			{
				ViewCompat.SetElevation(this, 0);
				return;
			}

			bool isAcrylicTheme = MaterialFrame.MaterialTheme == MaterialFrame.Theme.Acrylic;

			// we need to reset the StateListAnimator to override the setting of Elevation on touch down and release.
			StateListAnimator = new Android.Animation.StateListAnimator();

			// set the elevation manually
			ViewCompat.SetElevation(this, isAcrylicTheme ? MaterialFrame.AcrylicElevation : MaterialFrame.Elevation);
		}

		private void UpdateLightThemeBackgroundColor()
		{
			if (MaterialFrame.MaterialTheme == MaterialFrame.Theme.Dark || MaterialFrame.MaterialTheme == MaterialFrame.Theme.AcrylicBlur)
			{
				return;
			}

			_mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());
		}

		private void UpdateAcrylicGlowColor()
		{
			_acrylicLayer?.SetColor(MaterialFrame.AcrylicGlowColor.ToAndroid());
		}

		private void UpdateMaterialTheme()
		{
			switch (MaterialFrame.MaterialTheme)
			{
				case MaterialFrame.Theme.Acrylic:
					SetAcrylicTheme();
					break;

				case MaterialFrame.Theme.Dark:
					SetDarkTheme();
					break;

				case MaterialFrame.Theme.Light:
					SetLightTheme();
					break;

				case MaterialFrame.Theme.AcrylicBlur:
					SetAcrylicBlurTheme();
					break;
			}
		}

		private void SetAcrylicBlurTheme()
		{
			_mainDrawable.SetColor(Color.Transparent.ToAndroid());

			this.SetBackground(_mainDrawable);

			UpdateElevation();

			EnableBlur();
		}

		private void SetDarkTheme()
		{
			DisableBlur();

			_mainDrawable.SetColor(MaterialFrame.ElevationToColor().ToAndroid());

			this.SetBackground(_mainDrawable);

			UpdateElevation();
		}

		private void SetLightTheme()
		{
			DisableBlur();

			_mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());

			this.SetBackground(_mainDrawable);

			UpdateElevation();
		}

		private void SetAcrylicTheme()
		{
			if (_acrylicLayer == null)
			{
				_acrylicLayer = new GradientDrawable();
				_acrylicLayer.SetShape(ShapeType.Rectangle);
			}

			UpdateAcrylicGlowColor();
			UpdateCornerRadius();

			_mainDrawable.SetColor(MaterialFrame.LightThemeBackgroundColor.ToAndroid());

			LayerDrawable layer = new LayerDrawable(new Drawable[] { _acrylicLayer, _mainDrawable });
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				layer.SetLayerInsetTop(1, (int)Context.ToPixels(2));
			}
			else
			{
				System.Console.WriteLine(
					$"{DateTime.Now:MM-dd H:mm:ss.fff} | Sharpnado.MaterialFrame | WARNING | The Acrylic glow is only supported on android API 23 or greater (starting from Marshmallow)");
			}

			this.SetBackground(layer);

			UpdateElevation();
		}
	}
}
