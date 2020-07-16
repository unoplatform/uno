using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TwoPaneView : Windows.UI.Xaml.Controls.Control
	{
		private const double c_defaultMinWideModeWidth = 641.0;
		private const double c_defaultMinTallModeHeight = 641.0;

		private readonly static GridLength c_pane1LengthDefault = new GridLength(1, GridUnitType.Auto);
		private readonly static GridLength c_pane2LengthDefault = new GridLength(1, GridUnitType.Star);

		private enum ViewMode
		{
			Pane1Only,
			Pane2Only,
			LeftRight,
			RightLeft,
			TopBottom,
			BottomTop,
			None
		}

		public event Windows.Foundation.TypedEventHandler<TwoPaneView, Object> ModeChanged;

		public UIElement Pane1
		{
			get => (UIElement)GetValue(Pane1Property);
			set => SetValue(Pane1Property, value);
		}

		// Using a DependencyProperty as the backing store for Pane1.  This enables animation, styling, binding, etc...
		public static DependencyProperty Pane1Property { get ; } =
			DependencyProperty.Register("Pane1", typeof(UIElement), typeof(TwoPaneView), new PropertyMetadata(null));

		public UIElement Pane2
		{
			get => (UIElement)GetValue(Pane2Property);
			set => SetValue(Pane2Property, value);
		}

		public static DependencyProperty Pane2Property { get ; } =
			DependencyProperty.Register("Pane2", typeof(UIElement), typeof(TwoPaneView), new PropertyMetadata(null));

		public GridLength Pane1Length
		{
			get => (GridLength)GetValue(Pane1LengthProperty);
			set => SetValue(Pane1LengthProperty, value);
		}

		public static DependencyProperty Pane1LengthProperty { get ; } =
			DependencyProperty.Register(
				nameof(Pane1Length),
				typeof(GridLength),
				typeof(TwoPaneView),
				new PropertyMetadata(c_pane1LengthDefault));



		public GridLength Pane2Length
		{
			get => (GridLength)GetValue(Pane2LengthProperty);
			set => SetValue(Pane2LengthProperty, value);
		}

		public static DependencyProperty Pane2LengthProperty { get ; } =
			DependencyProperty.Register(
				"Pane2Length",
				typeof(GridLength),
				typeof(TwoPaneView),
				new PropertyMetadata(c_pane2LengthDefault));

		public TwoPaneViewPriority PanePriority
		{
			get => (TwoPaneViewPriority)GetValue(PanePriorityProperty);
			set => SetValue(PanePriorityProperty, value);
		}

		public static DependencyProperty PanePriorityProperty { get ; } =
			DependencyProperty.Register("PanePriority", typeof(TwoPaneViewPriority), typeof(TwoPaneView), new PropertyMetadata(TwoPaneViewPriority.Pane1));



		public TwoPaneViewMode Mode
		{
			get => (TwoPaneViewMode)GetValue(ModeProperty);
			set => SetValue(ModeProperty, value);
		}

		public static DependencyProperty ModeProperty { get ; } =
			DependencyProperty.Register(
				"Mode",
				typeof(TwoPaneViewMode),
				typeof(TwoPaneView),
				new PropertyMetadata(TwoPaneViewMode.SinglePane));



		public TwoPaneViewWideModeConfiguration WideModeConfiguration
		{
			get => (TwoPaneViewWideModeConfiguration)GetValue(WideModeConfigurationProperty);
			set => SetValue(WideModeConfigurationProperty, value);
		}

		public static DependencyProperty WideModeConfigurationProperty { get ; } =
			DependencyProperty.Register("WideModeConfiguration", typeof(TwoPaneViewWideModeConfiguration), typeof(TwoPaneView), new PropertyMetadata(TwoPaneViewWideModeConfiguration.LeftRight));




		public TwoPaneViewTallModeConfiguration TallModeConfiguration
		{
			get => (TwoPaneViewTallModeConfiguration)GetValue(TallModeConfigurationProperty);
			set => SetValue(TallModeConfigurationProperty, value);
		}

		public static DependencyProperty TallModeConfigurationProperty { get ; } =
			DependencyProperty.Register("TallModeConfiguration", typeof(TwoPaneViewTallModeConfiguration), typeof(TwoPaneView), new PropertyMetadata(TwoPaneViewTallModeConfiguration.TopBottom));



		public double MinWideModeWidth
		{
			get => (double)GetValue(MinWideModeWidthProperty);
			set => SetValue(MinWideModeWidthProperty, value);
		}

		public static DependencyProperty MinWideModeWidthProperty { get ; } =
			DependencyProperty.Register("MinWideModeWidth", typeof(double), typeof(TwoPaneView), new PropertyMetadata(c_defaultMinWideModeWidth));



		public double MinTallModeHeight
		{
			get => (double)GetValue(MinTallModeHeightProperty);
			set => SetValue(MinTallModeHeightProperty, value);
		}

		public static DependencyProperty MinTallModeHeightProperty { get ; } =
			DependencyProperty.Register("MinTallModeHeight", typeof(double), typeof(TwoPaneView), new PropertyMetadata(c_defaultMinTallModeHeight));
	}
}
