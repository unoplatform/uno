using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_MarkupExtension();
			app.HostView.Children.Add(control);

			Assert.AreEqual("Just a simple ... markup extension", control.TestText1.Text);
			Assert.AreEqual("We should see the number 100 below:", control.TestText2.Text);
			Assert.AreEqual("100", control.TestText3.Text);
			Assert.AreEqual("True", control.TestText4.Text);
			Assert.AreEqual("From a Resource String markup extension", control.TestText5.Text);
			Assert.AreEqual("String from attached property", control.TestText6.Text);
			Assert.AreEqual("True", control.TestText7.Text);
		}
	}

	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public class SimpleMarkupExt : Windows.UI.Xaml.Markup.MarkupExtension
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
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(bool)value;
		}
	}

	public class TestEntityObject
	{
		public string StringProperty { get; set; } = string.Empty;

		public int IntProperty { get; set; }
	}
}
