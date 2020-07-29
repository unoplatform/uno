using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
    public abstract partial class RangeBase : Control
    {
		public RangeBase()
		{
			DefaultStyleKey = typeof(RangeBase);
		}

		#region Value

		public double Value
		{
			get { return (double)this.GetValue(ValueProperty); }
			set { this.SetValue(ValueProperty, value); }
		}

		public static DependencyProperty ValueProperty { get ; } =
			DependencyProperty.Register(
				"Value",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
					(double)0,
					(s, e) => (s as RangeBase)?.OnValueChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnValueChanged(double oldValue, double newValue)
		{
			ValueChanged?.Invoke(this, new RangeBaseValueChangedEventArgs(this) { OldValue = oldValue, NewValue = newValue });

			UpdateValues();
		}

		#endregion

		#region Minimum

		public double Minimum
		{
			get { return (double)this.GetValue(MinimumProperty); }
			set { this.SetValue(MinimumProperty, value); }
		}

		public static DependencyProperty MinimumProperty { get ; } =
			DependencyProperty.Register(
				"Minimum",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
					(double)0,
					(s, e) => (s as RangeBase)?.OnMinimumChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnMinimumChanged(double oldValue, double newValue)
		{
			if (Minimum < 0)
			{
				Maximum = 0;
			}
			if (Minimum > Maximum)
			{
				Maximum = Minimum;
			}

			UpdateValues();
		}

		#endregion

		#region Maximum

		public double Maximum
		{
			get { return (double)this.GetValue(MaximumProperty); }
			set { this.SetValue(MaximumProperty, value); }
		}

		public static DependencyProperty MaximumProperty { get ; } =
			DependencyProperty.Register(
				"Maximum",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
					(double)1,
					(s, e) => (s as RangeBase)?.OnMaximumChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnMaximumChanged(double oldValue, double newValue)
		{
			// TODO: To confirm
			if (Maximum < Minimum)
			{
				Minimum = Maximum;
			}

			UpdateValues();
        }

		#endregion
		
		#region SmallChange

		public double SmallChange
		{
			get { return (double)this.GetValue(SmallChangeProperty); }
			set { this.SetValue(SmallChangeProperty, value); }
		}

		public static DependencyProperty SmallChangeProperty { get ; } =
			DependencyProperty.Register(
				"SmallChange",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
                    (double)0.1,
					(s, e) => (s as RangeBase)?.OnSmallChangeChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnSmallChangeChanged(double oldValue, double newValue)
		{

		}

		#endregion

		#region LargeChange

		public double LargeChange
		{
			get { return (double)this.GetValue(LargeChangeProperty); }
			set { this.SetValue(LargeChangeProperty, value); }
		}

		public static DependencyProperty LargeChangeProperty { get ; } =
			DependencyProperty.Register(
				"LargeChange",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
					(double)1,
					(s, e) => OnLargeChangeChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		private static void OnLargeChangeChanged(double oldValue, double newValue)
		{

		}

		#endregion
		
		#region ActualValue

		public double ActualValue
		{
			get { return (double)this.GetValue(ActualValueProperty); }
			private set { this.SetValue(ActualValueProperty, value); }
		}

		public static DependencyProperty ActualValueProperty { get ; } =
			DependencyProperty.Register(
				"ActualValue",
				typeof(double),
				typeof(RangeBase),
				new FrameworkPropertyMetadata(
					(double)0,
					(s, e) => (s as RangeBase)?.OnActualValueChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnActualValueChanged(double oldValue, double newValue)
		{

		}

		#endregion

		private void UpdateValues()
		{
			if (Value < Minimum)
			{
				Value = Minimum;
			}
			else if (Value > Maximum)
			{
				Value = Maximum;
			}
			
			ActualValue = (Value - Minimum) / (Maximum - Minimum);
		}

		public event RangeBaseValueChangedEventHandler ValueChanged;
	}
}
