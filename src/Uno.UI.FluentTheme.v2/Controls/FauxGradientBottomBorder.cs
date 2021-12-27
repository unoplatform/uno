#nullable enable

using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.FluentTheme.Controls
{
	internal partial class FauxGradientBottomBorder : ContentControl
	{
#if __WASM__ || __IOS__ || __MACOS__
		private static readonly Dictionary<LinearGradientBrush, SolidColorBrush> _overlayBrushCache = new();

		private readonly Border? _displayBorder = null;
#endif

		public FauxGradientBottomBorder()
		{
#if __WASM__ || __IOS__ || __MACOS__
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			VerticalContentAlignment = VerticalAlignment.Stretch;
			Content = _displayBorder = new Border();
#endif
		}

		public Brush RequestedBorderBrush
		{
			get => (Brush)GetValue(RequestedBorderBrushProperty);
			set => SetValue(RequestedBorderBrushProperty, value);
		}

		public static DependencyProperty RequestedBorderBrushProperty { get; } =
			DependencyProperty.Register(
				nameof(RequestedBorderBrush),
				typeof(Brush),
				typeof(FauxGradientBottomBorder),
				new PropertyMetadata(null, propertyChangedCallback: (s, args) => (s as FauxGradientBottomBorder)?.OnBorderChanged()));

		public Thickness RequestedBorderThickness
		{
			get => (Thickness)GetValue(RequestedBorderThicknessProperty);
			set => SetValue(RequestedBorderThicknessProperty, value);
		}

		public static DependencyProperty RequestedBorderThicknessProperty { get; } =
			DependencyProperty.Register(
				nameof(RequestedBorderThickness),
				typeof(Thickness),
				typeof(FauxGradientBottomBorder),
				new PropertyMetadata(default(Thickness), propertyChangedCallback: (s, args) => (s as FauxGradientBottomBorder)?.OnBorderChanged()));

		public CornerRadius RequestedCornerRadius
		{
			get => (CornerRadius)GetValue(RequestedCornerRadiusProperty);
			set => SetValue(RequestedCornerRadiusProperty, value);
		}

		public static DependencyProperty RequestedCornerRadiusProperty { get; } =
			DependencyProperty.Register(
				nameof(RequestedCornerRadius),
				typeof(CornerRadius),
				typeof(FauxGradientBottomBorder),
				new PropertyMetadata(CornerRadius.None, propertyChangedCallback: (s, args) => (s as FauxGradientBottomBorder)?.OnBorderChanged()));

		private void OnBorderChanged()
		{
#if __WASM__ || __IOS__ || __MACOS__
			if (_displayBorder == null)
			{
				return;
			}

			var requestedThickness = RequestedBorderThickness;
			var requestedBorderBrush = RequestedBorderBrush;
			var requestedCornerRadius = RequestedCornerRadius;

			if (requestedBorderBrush is not LinearGradientBrush gradientBrush)
			{
				_displayBorder.Visibility = Visibility.Collapsed;
				return;
			}

#if __WASM__
			if (requestedCornerRadius == CornerRadius.None)
			{
				// WASM can render linear gradient border unless corner radius is set.
				_displayBorder.Visibility = Visibility.Collapsed;
				return;
			}
#endif

#if __IOS__ || __MACOS__
			if (gradientBrush.RelativeTransform == null)
			{
				// iOS can render linear gradient border unless relative transform is used.
				_displayBorder.Visibility = Visibility.Collapsed;
				return;
			}
#endif

			requestedThickness.Left = 0;
			requestedThickness.Right = 0;
			var minorStopAlignment = BorderGradientBrushHelper.GetMinorStopAlignment(gradientBrush);
			if (minorStopAlignment == VerticalAlignment.Top)
			{
				requestedThickness.Bottom = 0;
			}
			else
			{
				requestedThickness.Top = 0;
			}

			if (requestedThickness == Thickness.Empty)
			{
				_displayBorder.Visibility = Visibility.Collapsed;
				return;
			}

			_displayBorder.Visibility = Visibility.Visible;

			if (!_overlayBrushCache.TryGetValue(gradientBrush, out var overlayBrush))
			{
				var majorStop = BorderGradientBrushHelper.GetMajorStop(gradientBrush);
				var minorStop = gradientBrush.GradientStops.First(s => s != majorStop);

				overlayBrush = new SolidColorBrush(minorStop.Color);
				_overlayBrushCache[gradientBrush] = overlayBrush;
			}

			_displayBorder.CornerRadius = requestedCornerRadius;
			_displayBorder.BorderThickness = requestedThickness;
			_displayBorder.BorderBrush = overlayBrush;
#endif
		}
	}
}
