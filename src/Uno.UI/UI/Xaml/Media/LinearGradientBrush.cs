using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = "GradientStops")]
	public partial class LinearGradientBrush : Brush
	{
        public LinearGradientBrush()
        {
            GradientStops = new GradientStopCollection();
        }

        public GradientStopCollection GradientStops
		{
			get { return (GradientStopCollection)this.GetValue(GradientStopsProperty); }
			set { this.SetValue(GradientStopsProperty, value); }
		}
		public static readonly DependencyProperty GradientStopsProperty = DependencyProperty.Register(
			"GradientStops",
			typeof(GradientStopCollection),
			typeof(LinearGradientBrush),
			new PropertyMetadata(null)
		);

		public Point StartPoint
		{
			get { return (Point)this.GetValue(StartPointProperty); }
			set { this.SetValue(StartPointProperty, value); }
		}
		public static readonly DependencyProperty StartPointProperty = DependencyProperty.Register(
			"StartPoint",
			typeof(Point),
			typeof(LinearGradientBrush),
			new PropertyMetadata(default(Point))
		);

		public Point EndPoint
		{
			get { return (Point)this.GetValue(EndPointProperty); }
			set { this.SetValue(EndPointProperty, value); }
		}
		public static readonly DependencyProperty EndPointProperty = DependencyProperty.Register(
			"EndPoint",
			typeof(Point),
			typeof(LinearGradientBrush),
			new PropertyMetadata(new Point(1,1))
		);

	}
}
