using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class RowDefinition : DependencyObject
	{
		public RowDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		#region Height DependencyProperty

		public GridLength Height
		{
			get { return (GridLength)this.GetValue(HeightProperty); }
			set { this.SetValue(HeightProperty, value); }
		}

		public static DependencyProperty HeightProperty { get ; } =
			DependencyProperty.Register(
				"Height",
				typeof(GridLength),
				typeof(RowDefinition),
				new FrameworkPropertyMetadata(
					GridLengthHelper.OneStar,
					(s, e) => ((RowDefinition)s)?.OnHeightChanged(e)
				)
			);

		private void OnHeightChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		public static implicit operator RowDefinition(string value)
		{
			return new RowDefinition { Height = GridLength.ParseGridLength(value).First() };
		}

		public double MinHeight
		{
			get => (double)this.GetValue(MinHeightProperty);
			set => this.SetValue(MinHeightProperty, value);
		}

		public double MaxHeight
		{
			get => (double)this.GetValue(MaxHeightProperty);
			set => this.SetValue(MaxHeightProperty, value);
		}
		public static DependencyProperty MinHeightProperty { get; } =
		DependencyProperty.Register(
			"MinHeight", typeof(double),
			typeof(RowDefinition),
			new FrameworkPropertyMetadata(0d));

		public static DependencyProperty MaxHeightProperty { get; } =
		DependencyProperty.Register(
			"MaxHeight", typeof(double),
			typeof(RowDefinition),
			new FrameworkPropertyMetadata(double.PositiveInfinity));


		public double ActualHeight
		{
			get
			{
				var parent = this.GetParent();
				var result = (parent as Grid)?.GetActualHeight(this) ?? 0d;
				return result;
			}
		}
	}
}
