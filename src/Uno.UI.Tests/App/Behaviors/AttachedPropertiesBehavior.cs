using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Behaviors
{
	public static class AttachedPropertiesBehavior
	{
		public static string GetCustomText(TextBlock obj) => (string)obj.GetValue(CustomTextProperty);

		public static void SetCustomText(TextBlock obj, string value) => obj.SetValue(CustomTextProperty, value);

		public static readonly DependencyProperty CustomTextProperty =
			DependencyProperty.RegisterAttached(
				"CustomText",
				typeof(string),
				typeof(AttachedPropertiesBehavior),
				new PropertyMetadata(string.Empty, OnCustomTextChanged));

		private static void OnCustomTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is TextBlock tb)
			{
				tb.Text = GetCustomText(tb);
			}
		}
	}
}
