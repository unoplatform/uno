using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public static class TestDP
{
	#region DependencyProperty: TestValue

	public static DependencyProperty TestValueProperty { get; } = DependencyProperty.RegisterAttached(
		"TestValue",
		typeof(object),
		typeof(TestDP),
		new PropertyMetadata(default(object)));

	public static object GetTestValue(DependencyObject obj) => (object)obj.GetValue(TestValueProperty);
	public static void SetTestValue(DependencyObject obj, object value) => obj.SetValue(TestValueProperty, value);

	#endregion
}
