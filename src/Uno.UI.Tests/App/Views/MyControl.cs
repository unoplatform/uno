using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.App.Views
{
	public partial class MyControl : Control
	{
		private double _splinitude;
		public double Splinitude
		{
			get => _splinitude;
			set => _splinitude = value;
		}

		public MyPoco Poco
		{
			get { return (MyPoco)GetValue(PocoProperty); }
			set { SetValue(PocoProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Poco.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PocoProperty =
			DependencyProperty.Register("Poco", typeof(MyPoco), typeof(MyControl), new PropertyMetadata(null, OnPocoChanged));

		private static void OnPocoChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}

		public TimeSpan MyInterval
		{
			get { return (TimeSpan)GetValue(MyIntervalProperty); }
			set { SetValue(MyIntervalProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyInterval.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyIntervalProperty =
			DependencyProperty.Register("MyInterval", typeof(TimeSpan), typeof(MyControl), new PropertyMetadata(default(TimeSpan)));


		public double Pilleability
		{
			get { return (double)GetValue(PilleabilityProperty); }
			set { SetValue(PilleabilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Pilleability.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PilleabilityProperty =
			DependencyProperty.Register("Pilleability", typeof(double), typeof(MyControl), new PropertyMetadata(0d));



		public double Arduousness
		{
			get { return (double)GetValue(ArduousnessProperty); }
			set { SetValue(ArduousnessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Arduousness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ArduousnessProperty =
			DependencyProperty.Register("Arduousness", typeof(double), typeof(MyControl), new PropertyMetadata(0d));


		public Brush Midground
		{
			get { return (Brush)GetValue(MidgroundProperty); }
			set { SetValue(MidgroundProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Midground.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MidgroundProperty =
			DependencyProperty.Register("Midground", typeof(Brush), typeof(MyControl), new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));
	}
}
