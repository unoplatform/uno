using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Xamarin.UITest;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		public void StaticResource_XAML_Validation()
		{
			Run("UITests.Shared.Resources.StaticResource.StaticResource_Simple");

			_app.WaitForElement(_app.Marked("XAMLResource_Text"));

			var resourceText = _app.Marked("XAMLResource_Text");

			Assert.AreEqual("This resource was registered in XAML", resourceText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		public void StaticResource_CSharp_Validation()
		{
			Run("UITests.Shared.Resources.StaticResource.StaticResource_Simple");

			_app.WaitForElement(_app.Marked("CSharpResource_Text"));

			var resourceText = _app.Marked("CSharpResource_Text");

			Assert.AreEqual("This resource was registered in C#", resourceText.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		public void StaticResource_Converter_Validation()
		{
			Run("UITests.Shared.Resources.StaticResource.StaticResource_Simple");

			_app.WaitForElement(_app.Marked("ConverterResource_Text"));

			var resourceText = _app.Marked("ConverterResource_Text");

			Assert.AreEqual("Hello Converter!", resourceText.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
