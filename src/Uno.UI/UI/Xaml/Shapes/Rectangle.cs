using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public double RadiusY
		{
			get { return (double)this.GetValue(RadiusYProperty); }
			set { this.SetValue(RadiusYProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RadiusY.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RadiusYProperty =
			DependencyProperty.Register("RadiusY", typeof(double), typeof(Rectangle), new PropertyMetadata(0.0, (s,e)=>
			 ((Rectangle)s).OnRadiusYChangedPartial()));


		public double RadiusX
		{
			get { return (double)this.GetValue(RadiusXProperty); }
			set { this.SetValue(RadiusXProperty, value); }
		}

		// Using a DependencyProperty as the backing store for RadiusX.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RadiusXProperty =
			DependencyProperty.Register("RadiusX", typeof(double), typeof(Rectangle), new PropertyMetadata(0.0, (s, e) =>
			 ((Rectangle)s).OnRadiusXChangedPartial()));

		partial void OnRadiusXChangedPartial();
		partial void OnRadiusYChangedPartial();


	}
}
