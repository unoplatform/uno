using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.App.Views
{
	public class MyBehavior
	{

		public static double GetBulbousness(DependencyObject obj)
		{
			return (double)obj.GetValue(BulbousnessProperty);
		}

		public static void SetBulbousness(DependencyObject obj, double value)
		{
			obj.SetValue(BulbousnessProperty, value);
		}

		// Using a DependencyProperty as the backing store for Bulbousness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BulbousnessProperty =
			DependencyProperty.RegisterAttached("Bulbousness", typeof(double), typeof(MyBehavior), new PropertyMetadata(0d));

		public static double GetNoDPProperty(FrameworkElement obj)
		{
			if (obj.Tag is double value)
			{
				return value;
			}

			return default(double);
		}

		public static void SetNoDPProperty(FrameworkElement obj, double value)
		{
			obj.Tag = value;
		}
	}
}
