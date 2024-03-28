using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.UI.Input;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ScrollBar
	{
		public double ViewportSize
		{
			get => (double)GetValue(ViewportSizeProperty);
			set => SetValue(ViewportSizeProperty, value);
		}

		public Controls.Orientation Orientation
		{
			get => (Controls.Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public ScrollingIndicatorMode IndicatorMode
		{
			get => (ScrollingIndicatorMode)GetValue(IndicatorModeProperty);
			set => SetValue(IndicatorModeProperty, value);
		}

		public static DependencyProperty IndicatorModeProperty { get; } =
		DependencyProperty.Register(
			nameof(IndicatorMode),
			typeof(ScrollingIndicatorMode),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(ScrollingIndicatorMode.None, propertyChangedCallback: (s, e) => (s as ScrollBar).RefreshTrackLayout()));

		public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Controls.Orientation),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(Orientation.Vertical, propertyChangedCallback: (s, e) => (s as ScrollBar).OnOrientationChanged()));

		public static DependencyProperty ViewportSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(ViewportSize),
			typeof(double),
			typeof(ScrollBar),
			new FrameworkPropertyMetadata(0.0));

		public event ScrollEventHandler Scroll;
	}
}
