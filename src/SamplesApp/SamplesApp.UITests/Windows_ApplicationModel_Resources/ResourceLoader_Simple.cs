using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_ApplicationModel_Resources
{
	[TestFixture]
	public partial class ResourceLoader_Simple : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ValidateResourceLoader_Simple()
		{
			Run("UITests.Shared.Windows_ApplicationModel_Resources_ResourceLoader.ResourceLoader_Simple");

			_app.WaitForElement(_app.Marked("ResourceLoader_Simple_tb01"));

			Assert.AreEqual(
				@"This is ResourceLoader_Simple_tb01.Text in UITestsStrings\en-US\Resources.resw",
				_app.Marked("ResourceLoader_Simple_tb01").GetDependencyPropertyValue("Text")?.ToString());

			Assert.AreEqual(
				@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\Resources.resw",
				_app.Marked("MyPrefix/ResourceLoader_Simple_tb02").GetDependencyPropertyValue("Text")?.ToString());

			Assert.AreEqual(
				@"This is ResourceLoader_Simple_tb02.Text in UITestsStrings\en-US\NamedResources.resw",
				_app.Marked("/NamedResources/ResourceLoader_Simple_tb02").GetDependencyPropertyValue("Text")?.ToString());

			Assert.AreEqual(
				@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\NamedResources.resw",
				_app.Marked("/NamedResources/MyPrefix/ResourceLoader_Simple_tb02").GetDependencyPropertyValue("Text")?.ToString());

			Assert.AreEqual(
				@"This is MyPrefix/MyPrefix2/ResourceLoader_Simple_tb03 in UITestsStrings\en-US\NamedResources.resw",
				_app.Marked("/NamedResources/MyPrefix/MyPrefix2/ResourceLoader_Simple_tb03").GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
