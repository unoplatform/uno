using System;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Shapes;

[Sample("Shapes")]
public sealed partial class Rectangle_Radius_StrokeThickness : UserControl
{
	public Rectangle_Radius_StrokeThickness()
	{
		this.InitializeComponent();
	}
	
	public bool LockRadius
	{
		get => (bool)GetValue(LockRadiusProperty);
		set => SetValue(LockRadiusProperty, value);
	}

	public static DependencyProperty LockRadiusProperty { get; } =
		DependencyProperty.Register(nameof(LockRadius), typeof(bool), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(false, OnLockRadiusChanged));
	
	public bool LockSize
	{
		get => (bool)GetValue(LockSizeProperty);
		set => SetValue(LockSizeProperty, value);
	}

	public static DependencyProperty LockSizeProperty { get; } =
		DependencyProperty.Register(nameof(LockSize), typeof(bool), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(false, OnLockSizeChanged));

	public int RadiusX
	{
		get => (int)GetValue(RadiusXProperty);
		set => SetValue(RadiusXProperty, value);
	}

	public static DependencyProperty RadiusXProperty { get; } =
		DependencyProperty.Register(nameof(RadiusX), typeof(int), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(0, OnRadiusChanged));

	public int RadiusY
	{
		get => (int)GetValue(RadiusYProperty);
		set => SetValue(RadiusYProperty, value);
	}

	public static DependencyProperty RadiusYProperty { get; } =
		DependencyProperty.Register(nameof(RadiusY), typeof(int), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(0, OnRadiusChanged));


	public int StrokeThickness
	{
		get => (int)GetValue(StrokeThicknessProperty);
		set => SetValue(StrokeThicknessProperty, value);
	}

	public static DependencyProperty StrokeThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(StrokeThickness),
			typeof(int),
			typeof(Rectangle_Radius_StrokeThickness),
			new PropertyMetadata(0));

	public int ElementWidth
	{
		get => (int)GetValue(ElementWidthProperty);
		set => SetValue(ElementWidthProperty, value);
	}

	public static DependencyProperty ElementWidthProperty { get; } =
		DependencyProperty.Register(nameof(ElementWidth), typeof(int), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(200, OnElementSizeChanged));

	public int ElementHeight
	{
		get => (int)GetValue(ElementHeightProperty);
		set => SetValue(ElementHeightProperty, value);
	}

	public static DependencyProperty ElementHeightProperty { get; } =
		DependencyProperty.Register(nameof(ElementHeight), typeof(int), typeof(Rectangle_Radius_StrokeThickness), new PropertyMetadata(200, OnElementSizeChanged));

	private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Rectangle_Radius_StrokeThickness ctrl)
		{
			if (ctrl.LockRadius)
			{
				var val = (int)e.NewValue;
				ctrl.RadiusX = ctrl.RadiusY = val;
			}
		}
	}

	private static void OnElementSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Rectangle_Radius_StrokeThickness ctrl)
		{
			if (ctrl.LockSize)
			{
				var val = (int)e.NewValue;
				ctrl.ElementHeight = ctrl.ElementWidth = val;
			}
		}
	}

	private static void OnLockRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Rectangle_Radius_StrokeThickness ctrl)
		{
			if ((bool)e.NewValue)
			{
				var max = Math.Max(ctrl.RadiusX, ctrl.RadiusY);
				ctrl.RadiusX = ctrl.RadiusY = max;
			}
		}
	}
	
	private static void OnLockSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is Rectangle_Radius_StrokeThickness ctrl)
		{
			if ((bool)e.NewValue)
			{
				var max = Math.Max(ctrl.ElementWidth, ctrl.ElementHeight);
				ctrl.ElementWidth = ctrl.ElementHeight = max;
			}
		}
	}
}
