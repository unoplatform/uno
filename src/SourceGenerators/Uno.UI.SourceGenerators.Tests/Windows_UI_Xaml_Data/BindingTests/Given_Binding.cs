#if NET6_0_OR_GREATER
namespace Uno.UI.SourceGenerators.IntegrationTests.Windows_UI_Xaml_Data.BindingTests;

using static XamlCodeGeneratorHelper;

[TestClass]
public class Given_Binding
{
	[TestMethod]
	public async Task When_Xaml_Object_With_Common_Properties()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Binding_Xaml_Object_With_Common_Properties.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Xaml_Object_With_Xaml_Object_Properties()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Binding_Xaml_Object_With_Xaml_Object_Properties.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));

		diagnostics.Should().BeEmpty();
	}
}
#endif
