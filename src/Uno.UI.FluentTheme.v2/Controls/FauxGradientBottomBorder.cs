#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.FluentTheme.Controls
{
	internal partial class FauxGradientBottomBorder : ContentControl
	{
#if __WASM__
		private readonly Border? _displayBorder = null;
#endif

		public FauxGradientBottomBorder()
		{
#if __WASM__
			HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
			VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
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
#if __WASM__
			if (_displayBorder == null)
			{
				return;
			}

			var requestedThickness = RequestedBorderThickness;
			var requestedBorderBrush = RequestedBorderBrush;
			var requestedCornerRadius = RequestedCornerRadius;

			if (requestedThickness.Bottom == 0 ||
				requestedBorderBrush is not GradientBrush gradientBrush)
			{
				_displayBorder.Visibility = Visibility.Collapsed;
				return;
			}
#endif
		}
	}
}
