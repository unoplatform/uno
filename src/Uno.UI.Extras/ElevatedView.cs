using System;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Extras
{
	[ContentProperty(Name = "ElevatedContent")]
	[TemplatePart(Name = "PART_Border", Type = typeof(Border))]
	[TemplatePart(Name = "PART_ShadowHost", Type = typeof(Grid))]
	public sealed partial class ElevatedView : Control
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

			UpdateElevation();
		}

		public static DependencyProperty ElevationProperty { get; } = DependencyProperty.Register(
			"Elevation", typeof(double), typeof(ElevatedView), new PropertyMetadata(default(double), OnChanged));

		public double Elevation
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
			new FrameworkPropertyMetadata(default(Brush), OnChanged)
		);

		public new Brush Background
		{
			get => (Brush)GetValue(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		private protected override void OnCornerRadiusChanged(DependencyPropertyChangedEventArgs args) => OnChanged(this, args);
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
#if __SKIA__
				this.SetElevationInternal(Elevation, ShadowColor);
#elif (WINAPPSDK || WINDOWS_UWP || NETCOREAPP) && !HAS_UNO
				_border.SetElevationInternal(Elevation, ShadowColor, _shadowHost as DependencyObject, CornerRadius);
#endif
			}
		}
	}
}
