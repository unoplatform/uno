using System;
using System.Drawing;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.Xaml.Media;
using View = Android.Views.View;

namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush
	{
		private const float AndroidBlurRadius = 20;

		private const float StyledBlurRadius = 64;

		private static readonly Color DarkBlurOverlayColor = Color.FromArgb(128, 0, 0, 0);

		//private static readonly Color LightBlurOverlayColor = Color.FromHex("#40FFFFFF");

		//private static readonly Color ExtraLightBlurOverlayColor = Color.FromHex("#B0FFFFFF");

		private static int blurAutoUpdateDelayMilliseconds = 100;
		private static int blurProcessingDelayMilliseconds = 10;

		private RealtimeBlurView _realtimeBlurView;

		//private View _blurRootView;

		/// <summary>
		/// When a page visibility changes we activate or deactivate blur updates.
		/// Setting a bigger delay could improve performance and rendering.
		/// </summary>
		public static int BlurAutoUpdateDelayMilliseconds
		{
			get => blurAutoUpdateDelayMilliseconds;
			set
			{
				if (value < 0)
				{
					throw new ArgumentException(
						"The blur processing delay cannot be negative",
						nameof(BlurAutoUpdateDelayMilliseconds));
				}

				blurAutoUpdateDelayMilliseconds = value;
			}
		}

		/// <summary>
		/// Sometimes the computation of the background can take some times (svg images for example).
		/// Setting a bigger delay to be sure that the background is rendered first can fix some glitches.
		/// </summary>
		public static int BlurProcessingDelayMilliseconds
		{
			get => blurProcessingDelayMilliseconds;
			set
			{
				if (value < 0)
				{
					throw new ArgumentException(
						"The blur processing delay cannot be negative",
						nameof(BlurProcessingDelayMilliseconds));
				}

				blurProcessingDelayMilliseconds = value;
			}
		}

		/// <summary>
		/// If set to <see langword="true"/>, the rendering result could be better (clearer blur not mixing front elements).
		/// However due to a bug in the Xamarin framework https://github.com/xamarin/xamarin-android/issues/4548, debugging is impossible with this mode (causes SIGSEGV).
		/// A suggestion would be to set it to false for debug, and to true for releases.
		/// </summary>
		public static bool ThrowStopExceptionOnDraw { get; set; } = false;

		private bool IsAndroidBlurPropertySet => AndroidBlurRadius > 0;

		private double CurrentBlurRadius =>
			IsAndroidBlurPropertySet ? AndroidBlurRadius : StyledBlurRadius;

		//TODO
		//protected void OnAttachedToWindow()
		//{
		//	if (MaterialFrame.AndroidBlurRootElement != null && _blurRootView == null)
		//	{
		//		UpdateAndroidBlurRootElement();
		//	}
		//}

		//TODO
		//protected void OnSizeChanged(int w, int h, int oldw, int oldh)
		//{
		//	base.OnSizeChanged(w, h, oldw, oldh);

		//	LayoutBlurView();
		//}

		private void LayoutBlurView(ViewGroup view)
		{
			if (view.MeasuredWidth == 0 || view.MeasuredHeight == 0 || _realtimeBlurView == null)
			{
				return;
			}

			int width = view.MeasuredWidth;
			int height = view.MeasuredHeight;

			_realtimeBlurView.Measure(width, height);
			_realtimeBlurView.Layout(0, 0, width, height);
		}

		private void DestroyBlur(BindableView view)
		{
			if (!_realtimeBlurView.IsNullOrDisposed())
			{
				view.RemoveView(_realtimeBlurView);
			}

			_realtimeBlurView?.Destroy();
			_realtimeBlurView = null;
		}

		//private void UpdateAndroidBlurRootElement()
		//{
		//	if (MaterialFrame.AndroidBlurRootElement == null)
		//	{
		//		return;
		//	}

		//	var formsView = MaterialFrame.AndroidBlurRootElement;
		//	var renderer = Platform.GetRenderer(formsView);
		//	if (renderer == null)
		//	{
		//		return;
		//	}

		//	bool IsAncestor(Element child, Layout parent)
		//	{
		//		if (child.Parent == null)
		//		{
		//			return false;
		//		}

		//		if (child.Parent == parent)
		//		{
		//			return true;
		//		}

		//		return IsAncestor(child.Parent, parent);
		//	}

		//	if (!IsAncestor(MaterialFrame, MaterialFrame.AndroidBlurRootElement))
		//	{
		//		throw new InvalidOperationException(
		//			"The AndroidBlurRootElement of the MaterialFrame should be an ancestor of the MaterialFrame.");
		//	}

		//	Platform.SetRenderer(formsView, renderer);
		//	_blurRootView = renderer.View;

		//	_realtimeBlurView?.SetRootView(_blurRootView);
		//}

		private void UpdateAndroidBlurOverlayColor(bool invalidate = true)
		{
			if (IsAndroidBlurPropertySet)
			{
				var tintColorWithOpacity =
					Color.FromArgb(
						(byte)(TintOpacity * TintColor.A),
						TintColor.R,
						TintColor.G,
						TintColor.B);
				Android.Graphics.Color tintColor = tintColorWithOpacity;
				_realtimeBlurView?.SetOverlayColor(tintColor, invalidate);
			}
		}

		private void UpdateAndroidBlurRadius(bool invalidate = true)
		{
			if (IsAndroidBlurPropertySet)
			{
				//TODO
				//_realtimeBlurView?.SetBlurRadius(
				//	ContextHelper.Current.ToPixels(AndroidBlurRadius),
				//	invalidate);
			}
		}

		private void UpdateMaterialBlurStyle(bool invalidate = true)
		{
			if (_realtimeBlurView == null || IsAndroidBlurPropertySet)
			{
				return;
			}

			//TODO
			//_realtimeBlurView.SetBlurRadius(
			//	ContextHelper.Current.ToPixels(StyledBlurRadius),
			//	invalidate);

			Android.Graphics.Color color = DarkBlurOverlayColor;
			_realtimeBlurView.SetOverlayColor(color, invalidate);
		}

		private void EnableBlur(ViewGroup view)
		{
			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInformation("Renderer::EnableBlur()");
			}

			if (_realtimeBlurView == null)
			{
				_realtimeBlurView = new RealtimeBlurView(view.Context);
			}

			UpdateAndroidBlurRadius();
			UpdateAndroidBlurOverlayColor();
			UpdateMaterialBlurStyle();
			//UpdateAndroidBlurRootElement();

			_realtimeBlurView.SetDownsampleFactor(CurrentBlurRadius <= 10 ? 1 : 2);

			UpdateCornerRadius();

			if (view.ChildCount > 0 && ReferenceEquals(view.GetChildAt(0), _realtimeBlurView))
			{
				// Already added
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInformation("Renderer::EnableBlur() => adding pre draw listener");
			}

			view.AddView(
				_realtimeBlurView,
				0,
				new FrameLayout.LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent,
					GravityFlags.NoGravity));

			LayoutBlurView(view);
		}

		private void DisableBlur(ViewGroup view)
		{
			if (view.ChildCount == 0 || !ReferenceEquals(view.GetChildAt(0), _realtimeBlurView))
			{
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().LogInformation("Renderer::DisableBlur() => removing pre draw listener");
			}

			view.RemoveView(_realtimeBlurView);
		}
	}
}
