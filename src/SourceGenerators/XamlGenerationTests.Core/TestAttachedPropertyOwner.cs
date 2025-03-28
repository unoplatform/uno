using Windows.UI.Xaml;

namespace XamlGenerationTests.Core
{
	public static class TestAttachedPropertyOwner
	{
		public static Style GetCustomStyle(DependencyObject obj)
		{
			return (Style)obj.GetValue(CustomStyleProperty);
		}

		public static void SetCustomStyle(DependencyObject obj, Style value)
		{
			obj.SetValue(CustomStyleProperty, value);
		}

		public static readonly DependencyProperty CustomStyleProperty =
			DependencyProperty.RegisterAttached("CustomStyle", typeof(Style), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(null));

		public static bool GetHasStuff(DependencyObject obj)
		{
			return (bool)obj.GetValue(HasStuffProperty);
		}

		public static void SetHasStuff(DependencyObject obj, bool value)
		{
			obj.SetValue(HasStuffProperty, value);
		}

		public static readonly DependencyProperty HasStuffProperty =
			DependencyProperty.RegisterAttached("HasStuff", typeof(bool), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(false));

		public static object GetExtraContent(DependencyObject obj)
		{
			return (object)obj.GetValue(ExtraContentProperty);
		}

		public static void SetExtraContent(DependencyObject obj, object value)
		{
			obj.SetValue(ExtraContentProperty, value);
		}

		public static readonly DependencyProperty ExtraContentProperty =
			DependencyProperty.RegisterAttached("ExtraContent", typeof(object), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(null));

		public static object GetMoreContent(DependencyObject obj)
		{
			return (object)obj.GetValue(MoreContentProperty);
		}

		public static void SetMoreContent(DependencyObject obj, object value)
		{
			obj.SetValue(MoreContentProperty, value);
		}

		public static readonly DependencyProperty MoreContentProperty =
			DependencyProperty.RegisterAttached("MoreContent", typeof(object), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(null));

		public static int? GetNullableType(DependencyObject obj) => (int?)obj.GetValue(NullableTypeProperty);

		public static void SetNullableType(DependencyObject obj, int? value)
		{
			obj.SetValue(NullableTypeProperty, value);
		}

		public static readonly DependencyProperty NullableTypeProperty =
			DependencyProperty.RegisterAttached("NullableType", typeof(int?), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(0));

		public static HorizontalAlignment? GetNullableEnum(DependencyObject obj) => (HorizontalAlignment?)obj.GetValue(NullableEnumProperty);

		public static void SetNullableEnum(DependencyObject obj, HorizontalAlignment? value)
		{
			obj.SetValue(NullableEnumProperty, value);
		}

		public static readonly DependencyProperty NullableEnumProperty =
			DependencyProperty.RegisterAttached("NullableEnum", typeof(HorizontalAlignment?), typeof(TestAttachedPropertyOwner), new FrameworkPropertyMetadata(null));
	}
}
