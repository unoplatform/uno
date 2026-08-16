using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_OptimizedStyle
{
	private const string ControlsSource = """
		namespace MyApp
		{
			public partial class MyControl : Microsoft.UI.Xaml.Controls.Control
			{
			}

			public partial class MyOptimizedControl : Microsoft.UI.Xaml.Controls.Control
			{
			}
		}
		""";

	[TestMethod]
	public async Task When_IsOptimizedStyle()
	{
		// Styles marked with IsOptimizedStyle are registered on the optimized channel
		// (FeatureConfiguration.Style.UseDefaultStyleOptimizations), while the other styles of the
		// same dictionary keep using the standard registration.
		var xamlFile = new XamlFile("MyResourceDictionary.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:local="using:MyApp">
				<Style TargetType="local:MyControl">
					<Setter Property="Background" Value="Red" />
				</Style>
				<Style TargetType="local:MyOptimizedControl"
					   IsOptimizedStyle="True">
					<Setter Property="Background" Value="Red" />
				</Style>
			</ResourceDictionary>
			""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					ControlsSource,
				}
			}
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
