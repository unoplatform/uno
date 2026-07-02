using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.UITests;
using Uno.UITest.Helpers.Queries;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

[TestClass]
[RunsOnUIThread]
public class Given_StaticResource_UITest : SampleControlUITestBase
{
	// The sample self-initializes: its constructor registers the C# resource and the
	// converter into Application.Current.Resources and assigns its own DataContext,
	// so RunAsync (Activator.CreateInstance) yields a fully wired page.
	[TestMethod]
	[DataRow("XAMLResource_Text", "This resource was registered in XAML", DisplayName = "XAML-declared resource")]
	[DataRow("CSharpResource_Text", "This resource was registered in C#", DisplayName = "C#-registered resource")]
	[DataRow("ConverterResource_Text", "Hello Converter!", DisplayName = "Converter resource")]
	public async Task When_StaticResource_Resolves(string elementName, string expected)
	{
		try
		{
			await RunAsync("UITests.Shared.Resources.StaticResource.StaticResource_Simple");

			var element = App.Marked(elementName);
			await App.WaitForDependencyPropertyValueAsync(element, "Text", expected);

			Assert.AreEqual(expected, element.GetDependencyPropertyValue<string>("Text"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
