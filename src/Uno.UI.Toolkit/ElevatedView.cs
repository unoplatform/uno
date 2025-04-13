﻿using System;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

#if __APPLE_UIKIT__
using CoreGraphics;
using _View = UIKit.UIView;
#elif __ANDROID__
using Android.Views;
#elif __WASM__
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
#endif


namespace Uno.UI.Toolkit
{
	[ContentProperty(Name = "ElevatedContent")]
	[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
	[TemplatePart(Name = "PART_ShadowHost", Type = typeof(Grid))]
	public sealed partial class ElevatedView : Control
#if HAS_UNO && !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		, ICustomClippingElement
#endif
	{
		/*
		 *  +-ElevatedView---------------------+
		 *  |                                  |
		 *  |  +-Canvas (PART_ShadowHost)---+  |
		 *  |  |                            |  |
		 *  |  +----------------------------+  |
		 *  |  +-Border (PART_Border)-------+  |
		 *  |  |                            |  |
		 *  |  |  +-Content--------------+  |  |
		 *  |  |  | (...)                |  |  |
		 *  |  |  +----------------------+  |  |
		 *  |  |                            |  |
		 *  |  +----------------------------+  |
		 *  |                                  |
		 *  +----------------------------------+
		 *
		 * UWP - Grid is responsible for the shadow
		 * Other Platforms - Elevated is responsible for the shadow
		 * Border responsible for rounded corners (if any)
		 *
		 */

		private static readonly Color DefaultShadowColor = Color.FromArgb(64, 0, 0, 0);

		private Border _border;
		private Panel _shadowHost;

		public ElevatedView()
		{
			DefaultStyleKey = typeof(ElevatedView);

#if __ANDROID__
			Unloaded += (snd, evt) => DisposeShadow();
#endif

#if HAS_UNO
			// Patch to deactivate the clipping by ContentControl
			RenderTransform = new CompositeTransform();
#endif
			SizeChanged += (snd, evt) => UpdateElevation();
		}

		protected override void OnApplyTemplate()
		{
			_border = GetTemplateChild("PART_Border") as Border;
			_shadowHost = GetTemplateChild("PART_ShadowHost") as Panel;

#if __APPLE_UIKIT__
			if (_border != null)
			{
				_border.BorderRenderer.BoundsPathUpdated += (s, e) => UpdateElevation();
			}
#endif

			UpdateElevation();
		}

		public static DependencyProperty ElevationProperty { get; } = DependencyProperty.Register(
			"Elevation", typeof(double), typeof(ElevatedView), new PropertyMetadata(default(double), OnChanged));

#if __ANDROID__
		public new double Elevation
#else
		public double Elevation
#endif
		{
			get => (double)GetValue(ElevationProperty);
			set => SetValue(ElevationProperty, value);
		}

		public static DependencyProperty ShadowColorProperty { get; } = DependencyProperty.Register(
			"ShadowColor", typeof(Color), typeof(ElevatedView), new PropertyMetadata(DefaultShadowColor, OnChanged));

		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}

		public static DependencyProperty ElevatedContentProperty { get; } = DependencyProperty.Register(
			"ElevatedContent", typeof(object), typeof(ElevatedView), new PropertyMetadata(default(object)));

		public object ElevatedContent
		{
			get => GetValue(ElevatedContentProperty);
			set => SetValue(ElevatedContentProperty, value);
		}

#if HAS_UNO
		public new static DependencyProperty BackgroundProperty { get; } = DependencyProperty.Register(
			"Background",
			typeof(Brush),
			typeof(ElevatedView),
#if __APPLE_UIKIT__
			new FrameworkPropertyMetadata(default(Brush))
#else
			new FrameworkPropertyMetadata(default(Brush), OnChanged)
#endif
		);

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

#if !__APPLE_UIKIT__
		private protected override void OnCornerRadiusChanged(DependencyPropertyChangedEventArgs args) => OnChanged(this, args);
#endif
#endif

		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			// We limit the clip to reduce the size of the cached bitmap created by Uno's
			// rendering logic as an optimization for the expensive drawing of shadows.
			Clip = new RectangleGeometry
			{
				Rect = new Rect(-Elevation, -Elevation, RenderSize.Width + Elevation * 2, RenderSize.Height + Elevation * 2)
			};

			if (_border == null)
			{
				return; // not initialized yet
			}

			if (Background == null)
			{
				this.SetElevationInternal(0, default);
			}
			else
			{
#if __WASM__
				this.SetElevationInternal(Elevation, ShadowColor);
				// We don't pass BorderThickness here to avoid "double" border being created. The BorderThickness will flow through ElevatedView template to PART_Border
				// Note that this line was necessary as of writing it even though we pass zero for BorderThickness. Setting the CornerRadius alone has a noticeable effect.
				// and not setting CornerRadius properly results in wrong rendering.
				// Note that the brush will not be used if we pass zero thickness, so we pass null instead of wasting time reading the dependency property.
				BorderLayerRenderer.SetCornerRadius(this, CornerRadius, default);
#elif __APPLE_UIKIT__
				this.SetElevationInternal(Elevation, ShadowColor, _border.BorderRenderer.BoundsPath);
#elif __ANDROID__
				_invalidateShadow = true;
				((ViewGroup)this).Invalidate();
#elif __SKIA__
				this.SetElevationInternal(Elevation, ShadowColor);
#elif (WINAPPSDK || WINDOWS_UWP || NETCOREAPP) && !HAS_UNO
				_border.SetElevationInternal(Elevation, ShadowColor, _shadowHost as DependencyObject, CornerRadius);
#endif
			}
		}

#if HAS_UNO && !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false; // Never clip, since it will remove the shadow

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

		protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
		{
#if __ANDROID__
			_invalidateShadow = true;
			((ViewGroup)this).Invalidate();
#endif

			return base.ArrangeOverride(this.ApplySizeConstraints(finalSize));
		}
#endif
	}
}
