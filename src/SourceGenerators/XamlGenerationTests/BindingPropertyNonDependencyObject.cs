using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace XamlGenerationTests.Shared
{
	public class BindingPropertyNonDependencyObject
	{
		public Binding MyBinding { get; set; }



		public static BindingPropertyNonDependencyObject GetMyAttached(DependencyObject obj)
		{
			return (BindingPropertyNonDependencyObject)obj.GetValue(MyAttachedProperty);
		}

		public static void SetMyAttached(DependencyObject obj, BindingPropertyNonDependencyObject value)
		{
			obj.SetValue(MyAttachedProperty, value);
		}

		// Using a DependencyProperty as the backing store for MyAttached.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyAttachedProperty =
			DependencyProperty.RegisterAttached(
				"MyAttached",
				typeof(BindingPropertyNonDependencyObject),
				typeof(BindingPropertyNonDependencyObject),
				new FrameworkPropertyMetadata(null)
			);
	}
}
