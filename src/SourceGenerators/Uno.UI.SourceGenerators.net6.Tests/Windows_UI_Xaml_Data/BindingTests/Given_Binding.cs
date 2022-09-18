using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Data.BindingTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Binding
{
	[TestMethod]
	public async Task When_Xaml_Object_With_Common_Properties()
	{
		var test = new TestSetup(xamlFileName: "Binding_Xaml_Object_With_Common_Properties.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}

	[TestMethod]
	public async Task When_Xaml_Object_With_Xaml_Object_Properties()
	{
		var test = new TestSetup(xamlFileName: "Binding_Xaml_Object_With_Xaml_Object_Properties.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"));
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}

	[TestMethod]
	public async Task When_Binding_ElementName_In_Template()
	{
		var test = new TestSetup(xamlFileName: "Binding_ElementName_In_Template.xaml", subFolder: Path.Combine("Uno.UI.Tests", "Windows_UI_Xaml_Data", "BindingTests", "Controls"))
		{
			PreprocessorSymbols =
			{
				"UNO_REFERENCE_API",
			},
		};
		await Verify.AssertXamlGeneratorDiagnostics(test);
	}
}
