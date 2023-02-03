using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml.MarkupExtensionTests
{
	[TestClass]
	public class Given_MarkupExtension
	{
		[TestMethod]
		public void When_ExtensionProvidesValues()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				var app = UnitTestsApp.App.EnsureApplication();

				var control = new Test_MarkupExtension();
				app.HostView.Children.Add(control);

				Assert.AreEqual("Just a simple ... markup extension", control.TestText1.Text);
				Assert.AreEqual("We should see the number 100 below:", control.TestText2.Text);
				Assert.AreEqual("100", control.TestText3.Text);
				Assert.AreEqual("True", control.TestText4.Text);
				Assert.AreEqual("From a Resource String markup extension", control.TestText5.Text);
				Assert.AreEqual("String from attached property markup extension", control.TestText6.Text);
				Assert.AreEqual("True", control.TestText7.Text);
				Assert.AreEqual("I am Value 1", control.TestText8.Text);
				Assert.AreEqual(22.0, control.TestText9.FontSize);
				Assert.AreEqual(3, control.TestText9.MaxLines);
				Assert.IsInstanceOfType(control.TestText10.DataContext, typeof(TestEntityObject));
				Assert.AreEqual("Just a simple ... fullname markup extension", control.TestText11.Text);
				Assert.AreEqual("From a Resource String FullName markup extension", control.TestText12.Text);
				Assert.AreEqual("String from attached property Fullname markup extension", control.TestText13.Text);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[TestMethod]
		public void When_Multiple_Extensions_Same_Name()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				var app = UnitTestsApp.App.EnsureApplication();

				var control = new Test_MarkupExtension();
				app.HostView.Children.Add(control);

				Assert.AreEqual("**BaseNamespaceShiny**", control.BaseShinyTextBlock.Text);
				Assert.AreEqual("~~NestedNamespaceShiny~~", control.NestedShinyTextBlock.Text);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[TestMethod]
		public void When_Extension_In_Style_Setter()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				StyleSetterExtension.Calls = 0;
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				var app = UnitTestsApp.App.EnsureApplication();

				var control = new Test_MarkupExtension();
				app.HostView.Children.Add(control);

				Assert.AreEqual("**Test**", control.StyleSetterTextBlock1.Text);
				Assert.AreEqual("**Test**", control.StyleSetterTextBlock2.Text);
				Assert.AreEqual(1, StyleSetterExtension.Calls);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[TestMethod]
		public void When_Extension_In_VisualState_Setter()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				var app = UnitTestsApp.App.EnsureApplication();

				var control = new Test_MarkupExtension();
				app.HostView.Children.Add(control);

				Assert.AreEqual("**Test**", control.VisualStateTextBlock.Text);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[TestMethod]
		public void When_Shortened_Name_Overlaps_Type()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				var app = UnitTestsApp.App.EnsureApplication();

				var control = new Test_MarkupExtension();
				app.HostView.Children.Add(control);

				Assert.AreEqual("TextBlockExtension value", control.TextBlockExtensionTextBlock.Text);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public class SimpleMarkupExtExtension : Windows.UI.Xaml.Markup.MarkupExtension
	{
		public string TextValue { get; set; }

		protected override object ProvideValue()
		{
			return TextValue + " markup extension";
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(TestEntityObject))]
	public class EntityMarkupExt : Windows.UI.Xaml.Markup.MarkupExtension
	{
		public string TextValue { get; set; }

		public int IntValue { get; set; }

		protected override object ProvideValue()
		{
			return new TestEntityObject()
			{
				StringProperty = TextValue,
				IntProperty = IntValue
			};
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(IValueConverter))]
	public class InverseBoolMarkupExt : Windows.UI.Xaml.Markup.MarkupExtension, IValueConverter
	{
		protected override object ProvideValue() => this;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return !(bool)(value ?? false);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(bool)(value ?? false);
		}
	}

	public class NoReturnTypeMarkupExt : Windows.UI.Xaml.Markup.MarkupExtension
	{
		public Values UseValue { get; set; }

		public object Value1 { get; set; }

		public int Value2 { get; set; }

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

	public class ShinyExtension : MarkupExtension
	{
		public object Wrapped { get; set; }

		protected override object ProvideValue() => $"**{Wrapped}**";
	}

	public class StyleSetterExtension : MarkupExtension
	{
		public static int Calls { get; set; }

		public object Wrapped { get; set; }

		protected override object ProvideValue()
		{
			Calls++;
			return $"**{Wrapped}**";
		}
	}

	public class VisualStateSetterExtension : MarkupExtension
	{
		public object Wrapped { get; set; }

		protected override object ProvideValue()
		{
			return $"**{Wrapped}**";
		}
	}

	// This should not be mistaken for a TextBlock
	public class TextBlockExtension : MarkupExtension
	{
		protected override object ProvideValue() => "TextBlockExtension value";
	}

	public enum Values
	{
		UseValue1,
		UseValue2
	}

	public class TestEntityObject
	{
		public string StringProperty { get; set; } = string.Empty;

		public int IntProperty { get; set; }
	}
}

namespace Uno.UI.Tests.Windows_UI_Xaml.MarkupExtensionTests.Nested
{

	public class ShinyExtension : MarkupExtension
	{
		public object Wrapped { get; set; }

		protected override object ProvideValue() => $"~~{Wrapped}~~";
	}
}
