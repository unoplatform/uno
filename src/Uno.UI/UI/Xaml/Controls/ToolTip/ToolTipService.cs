using System;

namespace Windows.UI.Xaml.Controls
{
	partial class ToolTipService
	{
		public static DependencyProperty ToolTipProperty { get; } =
			DependencyProperty.RegisterAttached(
				"ToolTip", typeof(object),
				typeof(ToolTipService),
				new FrameworkPropertyMetadata(default, OnToolTipChanged));

		private static void OnToolTipChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			throw new NotImplementedException();
		}

		public static object GetToolTip(DependencyObject element)
		{
			return element.GetValue(ToolTipProperty);
		}

		public static void SetToolTip( global::Windows.UI.Xaml.DependencyObject element,  object value)
		{
			element.SetValue(ToolTipProperty, value);
		}
	}
}