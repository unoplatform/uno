using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
			;
		}
	}
}
