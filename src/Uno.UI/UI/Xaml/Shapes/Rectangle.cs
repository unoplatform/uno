using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public static readonly DependencyProperty RadiusYProperty = DependencyProperty.Register(
			"RadiusY",
			typeof(double),
			typeof(Rectangle),
			new PropertyMetadata(0.0, (s, e) => ((Rectangle)s).OnRadiusYChangedPartial()));

		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}

		public static readonly DependencyProperty RadiusXProperty = DependencyProperty.Register(
			"RadiusX",
			typeof(double),
			typeof(Rectangle),
			new PropertyMetadata(0.0, (s, e) => ((Rectangle)s).OnRadiusXChangedPartial()));

		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}

		partial void OnRadiusXChangedPartial();
		partial void OnRadiusYChangedPartial();
	}
}
