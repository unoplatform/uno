/*

Implementation based on https://github.com/roubachof/Sharpnado.MaterialFrame.
with some modifications and removal of unused features.

*/

using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Color = Windows.UI.Color;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		private static bool? _isStockBlurSupported;

		private const float AndroidBlurRadius = 20;
		private const float StyledBlurRadius = 64;

		internal const int BlurAutoUpdateDelayMilliseconds = 100;
		internal const int BlurProcessingDelayMilliseconds = 10;

		/// <summary>
		/// If set to <see langword="true"/>, the rendering result could be better (clearer blur not mixing front elements).
		/// However due to a bug in the Xamarin framework https://github.com/xamarin/xamarin-android/issues/4548, debugging is impossible with this mode (causes SIGSEGV).
		/// A suggestion would be to set it to false for debug, and to true for releases.
		/// </summary>
		public static bool ThrowStopExceptionOnDraw { get; set; }

		private bool IsAndroidBlurPropertySet => AndroidBlurRadius > 0;

		private double CurrentBlurRadius =>
			IsAndroidBlurPropertySet ? AndroidBlurRadius : StyledBlurRadius;

		private void DestroyBlur(AcrylicState state)
		{
			if (!state.BlurView.IsNullOrDisposed())
			{
				state.Owner.RemoveView(state.BlurView);
			}

			state.BlurView?.Destroy();
			state.BlurView = null;
		}

		private void UpdateTint(AcrylicState state, bool invalidate = true)
		{
			var tintColorWithOpacity =
				Color.FromArgb(
					(byte)(TintOpacity * TintColor.A),
					TintColor.R,
					TintColor.G,
					TintColor.B);
			Android.Graphics.Color tintColor = tintColorWithOpacity;
			state.BlurView?.SetOverlayColor(tintColor, invalidate);
		}

		private void EnableBlur(AcrylicState state)
		{
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("Renderer::EnableBlur()");
			}

			if (state.BlurView == null)
			{
				state.BlurView = new RealtimeBlurView(state.Owner.Context);
			}

			state.BlurView.SetBlurRadius(state.Owner.Context.ToPixels(AndroidBlurRadius));
			UpdateTint(state);

			state.BlurView.SetDownsampleFactor(CurrentBlurRadius <= 10 ? 1 : 2);

			UpdateCornerRadius();

			if (state.Owner.ChildCount > 0 && ReferenceEquals(state.Owner.GetChildAt(0), state.BlurViewWrapper))
			{
				// Already added
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("Renderer::EnableBlur() => adding pre draw listener");
			}

			var blurViewWrapper = new ContentPresenter()
			{
				Content = state.BlurView,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			state.Owner.AddView(blurViewWrapper, 0);
			state.BlurViewWrapper = blurViewWrapper;
		}

		private void DisableBlur(AcrylicState state)
		{
			if (state.Owner.ChildCount == 0 || !ReferenceEquals(state.Owner.GetChildAt(0), state.BlurViewWrapper))
			{
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info("Renderer::DisableBlur() => removing pre draw listener");
			}

			state.Owner.RemoveView(state.BlurViewWrapper);
		}

		/// <summary>
		/// Checks if blurring is available on the current
		/// device.
		/// </summary>
		/// <returns>Value indicating whether blur is available.</returns>
		private static bool SupportsBlur()
		{
			if (_isStockBlurSupported == null)
			{
				// try to use stock impl first
				if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
				{
					if (ContextHelper.Current != null)
					{
						try
						{
							var stockBlurImplementation = new AndroidStockBlur();
							var bmp = Bitmap.CreateBitmap(4, 4, Bitmap.Config.Argb8888);
							stockBlurImplementation.Prepare(ContextHelper.Current, bmp, 4);
							stockBlurImplementation.Release();
							bmp.Recycle();
							_isStockBlurSupported = true;
						}
						catch (Exception)
						{
							if (typeof(AcrylicBrush).Log().IsEnabled(LogLevel.Warning))
							{
								typeof(AcrylicBrush).Log().LogWarning("Android Stock Blur implementation is not available");
							}
							_isStockBlurSupported = false;
						}
					}
					else
					{
						// Context has not been set yet, so we can't determine if blur is supported or not.
						// We return false but don't set the flag so next evaluation can check again.
						if (typeof(AcrylicBrush).Log().IsEnabled(LogLevel.Warning))
						{
							typeof(AcrylicBrush).Log().LogWarning("AcrylicBrush applied too early. Android Context must be initialized first.");
						}
						return false;
					}
				}
				else
				{
					_isStockBlurSupported = false;
				}
			}
			return _isStockBlurSupported.Value;
		}
	}
}
