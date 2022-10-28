using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Shapes;

[Sample("Shapes")]
public sealed partial class Ellipse_StrokeThickness_Tester : UserControl
{
	public Ellipse_StrokeThickness_Tester()
	{
		this.InitializeComponent();
	}

	public bool LockSize
	{
		get => (bool)GetValue(LockSizeProperty);
		set => SetValue(LockSizeProperty, value);
	}

	public static DependencyProperty LockSizeProperty { get; } =
		DependencyProperty.Register(nameof(LockSize), typeof(bool), typeof(Ellipse_StrokeThickness_Tester), new PropertyMetadata(false, OnLockSizeChanged));

	public int StrokeThickness
	{
		get => (int)GetValue(StrokeThicknessProperty);
		set => SetValue(StrokeThicknessProperty, value);
	}

	public static DependencyProperty StrokeThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(StrokeThickness),
			typeof(int),
			typeof(Ellipse_StrokeThickness_Tester),
			new PropertyMetadata(0));

	public int ElementWidth
	{
		get => (int)GetValue(ElementWidthProperty);
		set => SetValue(ElementWidthProperty, value);
	}

	public static DependencyProperty ElementWidthProperty { get; } =
		DependencyProperty.Register(nameof(ElementWidth), typeof(int), typeof(Ellipse_StrokeThickness_Tester), new PropertyMetadata(200, OnElementSizeChanged));

	public int ElementHeight
	{
		get => (int)GetValue(ElementHeightProperty);
		set => SetValue(ElementHeightProperty, value);
	}

	public static DependencyProperty ElementHeightProperty { get; } =
		DependencyProperty.Register(nameof(ElementHeight), typeof(int), typeof(Ellipse_StrokeThickness_Tester), new PropertyMetadata(200, OnElementSizeChanged));
	
	private static void OnElementSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Ellipse_StrokeThickness_Tester ctrl)
		{
			if (ctrl.LockSize)
			{
				var val = (int)e.NewValue;
				ctrl.ElementHeight = ctrl.ElementWidth = val;
			}
		}
	}

	private static void OnLockSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Ellipse_StrokeThickness_Tester ctrl)
		{
			if ((bool)e.NewValue)
			{
				var max = Math.Max(ctrl.ElementWidth, ctrl.ElementHeight);
				ctrl.ElementWidth = ctrl.ElementHeight = max;
			}
		}
	}
}
