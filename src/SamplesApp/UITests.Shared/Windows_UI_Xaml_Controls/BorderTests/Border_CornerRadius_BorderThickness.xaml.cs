using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.BorderTests
{
	[Sample("Border", Name = "Border_CornerRadius_BorderThickness")]
	public sealed partial class Border_CornerRadius_BorderThickness : UserControl
	{
		public Border_CornerRadius_BorderThickness()
		{
			this.InitializeComponent();
		}

		public Thickness MyBorderThickness
		{
			get => (Thickness)GetValue(MyBorderThicknessProperty);
			set => SetValue(MyBorderThicknessProperty, value);
		}

		public static DependencyProperty MyBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(MyBorderThickness), typeof(Thickness), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(new Thickness(0, 0, 0, 0)));

		public CornerRadius MyCornerRadius
		{
			get => (CornerRadius)GetValue(MyCornerRadiusProperty);
			set => SetValue(MyCornerRadiusProperty, value);
		}

		public static DependencyProperty MyCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(MyCornerRadius), typeof(CornerRadius), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(new CornerRadius(0, 0, 0, 0)));

		public bool LockCornerRadius
		{
			get => (bool)GetValue(LockCornerRadiusProperty);
			set => SetValue(LockCornerRadiusProperty, value);
		}

		public static DependencyProperty LockCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(LockCornerRadius), typeof(bool), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(false, OnLockCornerRadiusChanged));

		public bool LockBorderThickness
		{
			get => (bool)GetValue(LockBorderThicknessProperty);
			set => SetValue(LockBorderThicknessProperty, value);
		}

		public static DependencyProperty LockBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(LockBorderThickness), typeof(bool), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(false, OnLockBorderThicknessChanged));

		public bool LockSize
		{
			get => (bool)GetValue(LockSizeProperty);
			set => SetValue(LockSizeProperty, value);
		}

		public static DependencyProperty LockSizeProperty { get; } =
			DependencyProperty.Register(nameof(LockSize), typeof(bool), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(false, OnLockSizeChanged));

		public int TopLeftCornerRadius
		{
			get => (int)GetValue(TopLeftCornerRadiusProperty);
			set => SetValue(TopLeftCornerRadiusProperty, value);
		}

		public static DependencyProperty TopLeftCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(TopLeftCornerRadius), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));

		public int TopRightCornerRadius
		{
			get => (int)GetValue(TopRightCornerRadiusProperty);
			set => SetValue(TopRightCornerRadiusProperty, value);
		}

		public static DependencyProperty TopRightCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(TopRightCornerRadius), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));

		public int BottomRightCornerRadius
		{
			get => (int)GetValue(BottomRightCornerRadiusProperty);
			set => SetValue(BottomRightCornerRadiusProperty, value);
		}

		public static DependencyProperty BottomRightCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(BottomRightCornerRadius), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));

		public int BottomLeftCornerRadius
		{
			get => (int)GetValue(BottomLeftCornerRadiusProperty);
			set => SetValue(BottomLeftCornerRadiusProperty, value);
		}

		public static DependencyProperty BottomLeftCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(BottomLeftCornerRadius), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));

		public int LeftBorderThickness
		{
			get => (int)GetValue(LeftBorderThicknessProperty);
			set => SetValue(LeftBorderThicknessProperty, value);
		}

		public static DependencyProperty LeftBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(LeftBorderThickness), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));

		public int TopBorderThickness
		{
			get => (int)GetValue(TopBorderThicknessProperty);
			set => SetValue(TopBorderThicknessProperty, value);
		}

		public static DependencyProperty TopBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(TopBorderThickness), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));

		public int RightBorderThickness
		{
			get => (int)GetValue(RightBorderThicknessProperty);
			set => SetValue(RightBorderThicknessProperty, value);
		}

		public static DependencyProperty RightBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(RightBorderThickness), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));

		public int BottomBorderThickness
		{
			get => (int)GetValue(BottomBorderThicknessProperty);
			set => SetValue(BottomBorderThicknessProperty, value);
		}

		public static DependencyProperty BottomBorderThicknessProperty { get; } =
			DependencyProperty.Register(nameof(BottomBorderThickness), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));

		public int ElementWidth
		{
			get => (int)GetValue(ElementWidthProperty);
			set => SetValue(ElementWidthProperty, value);
		}

		public static DependencyProperty ElementWidthProperty { get; } =
			DependencyProperty.Register(nameof(ElementWidth), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(200, OnElementSizeChanged));

		public int ElementHeight
		{
			get => (int)GetValue(ElementHeightProperty);
			set => SetValue(ElementHeightProperty, value);
		}

		public static DependencyProperty ElementHeightProperty { get; } =
			DependencyProperty.Register(nameof(ElementHeight), typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(200, OnElementSizeChanged));

		private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if (ctrl.LockCornerRadius)
				{
					var val = (int)e.NewValue;
					ctrl.MyCornerRadius = new CornerRadius(val);
					ctrl.TopLeftCornerRadius = ctrl.TopRightCornerRadius = ctrl.BottomLeftCornerRadius = ctrl.BottomRightCornerRadius = val;
				}
				else
				{
					ctrl.MyCornerRadius = new CornerRadius(ctrl.TopLeftCornerRadius, ctrl.TopRightCornerRadius, ctrl.BottomRightCornerRadius, ctrl.BottomLeftCornerRadius);
				}
			}
		}

		private static void OnBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if (ctrl.LockBorderThickness)
				{
					var val = (int)e.NewValue;
					ctrl.MyBorderThickness = new Thickness(val);
					ctrl.LeftBorderThickness = ctrl.TopBorderThickness = ctrl.RightBorderThickness = ctrl.BottomBorderThickness = val;
				}
				else
				{
					ctrl.MyBorderThickness = new Thickness(ctrl.LeftBorderThickness, ctrl.TopBorderThickness, ctrl.RightBorderThickness, ctrl.BottomBorderThickness);
				}
			}
		}

		private static void OnElementSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if (ctrl.LockSize)
				{
					var val = (int)e.NewValue;
					ctrl.ElementHeight = ctrl.ElementWidth = val;
				}
			}
		}

		private static void OnLockCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if ((bool)e.NewValue)
				{
					var max = Math.Max(Math.Max(ctrl.TopLeftCornerRadius, ctrl.TopRightCornerRadius), Math.Max(ctrl.BottomLeftCornerRadius, ctrl.BottomRightCornerRadius));
					ctrl.TopLeftCornerRadius = ctrl.TopRightCornerRadius = ctrl.BottomLeftCornerRadius = ctrl.BottomRightCornerRadius = max;
				}
			}
		}

		private static void OnLockBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if ((bool)e.NewValue)
				{
					var max = Math.Max(Math.Max(ctrl.LeftBorderThickness, ctrl.TopBorderThickness), Math.Max(ctrl.RightBorderThickness, ctrl.BottomBorderThickness));
					ctrl.LeftBorderThickness = ctrl.TopBorderThickness = ctrl.RightBorderThickness = ctrl.BottomBorderThickness = max;
				}
			}
		}

		private static void OnLockSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Border_CornerRadius_BorderThickness ctrl)
			{
				if ((bool)e.NewValue)
				{
					var max = Math.Max(ctrl.ElementWidth, ctrl.ElementHeight);
					ctrl.ElementWidth = ctrl.ElementHeight = max;
				}
			}
		}
	}
}
