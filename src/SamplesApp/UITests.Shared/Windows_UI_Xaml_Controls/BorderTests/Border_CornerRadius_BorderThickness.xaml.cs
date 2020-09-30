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
			get { return (Thickness)GetValue(MyBorderThicknessProperty); }
			set { SetValue(MyBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyBorderThicknessProperty =
			DependencyProperty.Register("MyBorderThickness", typeof(Thickness), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(new Thickness(0, 0, 0, 0)));



		public CornerRadius MyCornerRadius
		{
			get { return (CornerRadius)GetValue(MyCornerRadiusProperty); }
			set { SetValue(MyCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyCornerRadiusProperty =
			DependencyProperty.Register("MyCornerRadius", typeof(CornerRadius), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(new CornerRadius(0, 0, 0, 0)));



		public bool LockCornerRadius
		{
			get { return (bool)GetValue(LockCornerRadiusProperty); }
			set { SetValue(LockCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LockCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LockCornerRadiusProperty =
			DependencyProperty.Register("LockCornerRadius", typeof(bool), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(false, OnLockCornerRadiusChanged));



		public bool LockBorderThickness
		{
			get { return (bool)GetValue(LockBorderThicknessProperty); }
			set { SetValue(LockBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LockBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LockBorderThicknessProperty =
			DependencyProperty.Register("LockBorderThickness", typeof(bool), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(false, OnLockBorderThicknessChanged));



		public int TopLeftCornerRadius
		{
			get { return (int)GetValue(TopLeftCornerRadiusProperty); }
			set { SetValue(TopLeftCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TopLeftCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLeftCornerRadiusProperty =
			DependencyProperty.Register("TopLeftCornerRadius", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));

		public int TopRightCornerRadius
		{
			get { return (int)GetValue(TopRightCornerRadiusProperty); }
			set { SetValue(TopRightCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TopRightCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopRightCornerRadiusProperty =
			DependencyProperty.Register("TopRightCornerRadius", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));



		public int BottomRightCornerRadius
		{
			get { return (int)GetValue(BottomRightCornerRadiusProperty); }
			set { SetValue(BottomRightCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BottomRightCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BottomRightCornerRadiusProperty =
			DependencyProperty.Register("BottomRightCornerRadius", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));



		public int BottomLeftCornerRadius
		{
			get { return (int)GetValue(BottomLeftCornerRadiusProperty); }
			set { SetValue(BottomLeftCornerRadiusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BottomLeftCornerRadius.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BottomLeftCornerRadiusProperty =
			DependencyProperty.Register("BottomLeftCornerRadius", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnCornerRadiusChanged));


		public int LeftBorderThickness
		{
			get { return (int)GetValue(LeftBorderThicknessProperty); }
			set { SetValue(LeftBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LeftBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LeftBorderThicknessProperty =
			DependencyProperty.Register("LeftBorderThickness", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));



		public int TopBorderThickness
		{
			get { return (int)GetValue(TopBorderThicknessProperty); }
			set { SetValue(TopBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TopBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopBorderThicknessProperty =
			DependencyProperty.Register("TopBorderThickness", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));



		public int RightBorderThickness
		{
			get { return (int)GetValue(RightBorderThicknessProperty); }
			set { SetValue(RightBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RightBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RightBorderThicknessProperty =
			DependencyProperty.Register("RightBorderThickness", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));



		public int BottomBorderThickness
		{
			get { return (int)GetValue(BottomBorderThicknessProperty); }
			set { SetValue(BottomBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for BottomBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BottomBorderThicknessProperty =
			DependencyProperty.Register("BottomBorderThickness", typeof(int), typeof(Border_CornerRadius_BorderThickness), new PropertyMetadata(0, OnBorderThicknessChanged));


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
	}
}
