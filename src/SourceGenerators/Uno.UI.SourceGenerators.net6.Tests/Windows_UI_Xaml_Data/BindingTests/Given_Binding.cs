namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Data.BindingTests;

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

	[TestMethod]
	public async Task When_Binding_ElementName_In_Template()
	{
		var diagnostics = await RunXamlCodeGeneratorForFileAsync(
			xamlFileName: "Binding_ElementName_In_Template.xaml",
			subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"),
			preprocessorSymbols: new[] { "UNO_REFERENCE_API", });

		diagnostics.Should().BeEmpty();
	}
}
