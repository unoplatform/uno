using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public partial class CustomMarkupExtensions : UserControl
	{
		public CustomMarkupExtensions()
		{
			this.InitializeComponent();
		}
	}
}

namespace XamlGenerationTests.Shared.MarkupExtensions
{
	public class TestObject
	{
		public string StringProp { get; set; }

		public int IntProp { get; set; }

		public bool BoolProp { get; set; }
	}

	public class TestConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}

	public static class AttachedTest
	{
		public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

		public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

		public static readonly DependencyProperty IsAttachedProperty =
			DependencyProperty.RegisterAttached(
				"IsAttached",
				typeof(bool),
				typeof(AttachedTest),
				new PropertyMetadata(false));
	}

	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public class TestMarkup : MarkupExtension
	{
		public string String1 { get; set; }

		public string String2 { get; set; }

		public int Number { get; set; }

		protected override object ProvideValue()
		{
			return $"{String1 ?? string.Empty} AND {String2 ?? string.Empty} THEN #{Number}";
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(IValueConverter))]
	public class InverseBoolMarkup : MarkupExtension, IValueConverter
	{
		protected override object ProvideValue() => this;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(bool)value;
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(TestObject))]
	public class ComplexMarkup : MarkupExtension
	{
		public string String { get; set; }

		public int Number { get; set; }

		public bool Boolean { get; set; }

		protected override object ProvideValue()
		{
			return new TestObject()
			{
				StringProp = String,
				IntProp = Number
			};
		}
	}

	public class TestMarkupSuffixExtension : MarkupExtension
	{
		protected override object ProvideValue()
		{
			return null;
		}
	}

	public class ComplexObject
	{
		public string StringProp { get; set; }
		public int IntProp { get; set; }
	}

	public class NoReturnTypeMarkupExt : Windows.UI.Xaml.Markup.MarkupExtension
	{
		public Values UseValue { get; set; }

		public object Value1 { get; set; }

		public object Value2 { get; set; }

		protected override object ProvideValue()
		{
			switch (UseValue)
			{
				case Values.UseValue1:
					return Value1;

				case Values.UseValue2:
					return Value2;

				default:
					return Value1;
			}
		}
	}

	public enum Values
	{
		UseValue1,
		UseValue2
	}

	public static class MarkupExtensionTestBehavior
	{
		public static string GetCustomText(TextBlock obj) => (string)obj.GetValue(CustomTextProperty);

		public static void SetCustomText(TextBlock obj, string value) => obj.SetValue(CustomTextProperty, value);

		public static readonly DependencyProperty CustomTextProperty =
			DependencyProperty.RegisterAttached(
				"CustomText",
				typeof(string),
				typeof(MarkupExtensionTestBehavior),
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
