using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Markup.XamlConditionTests;

[Sample(
	"XAML",
	Name = "IXamlCondition",
	Description = "Demonstrates a custom IXamlCondition implementation evaluated by XamlReader.Load against a conditional XAML namespace.")]
public sealed partial class XamlConditionSamplePage : Page
{
	private const string FeatureFlagConditionFullName =
		"UITests.Shared.Windows_UI_Xaml_Markup.XamlConditionTests.FeatureFlagCondition";

	public XamlConditionSamplePage()
	{
		this.InitializeComponent();
		this.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		// Compose runtime XAML strings that reference our IXamlCondition implementation
		// via a conditional XAML namespace. Uno's runtime XAML reader resolves the
		// predicate by fully qualified CLR type name; the XamlPredicateService caches
		// the result for the lifetime of the process. The defaults declared on
		// FeatureFlagCondition.FeatureFlags are NewExperience=true and LegacyMode=false.
		var conditionalElementsXaml = $$"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:newExp="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(NewExperience)"
						xmlns:legacy="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(LegacyMode)"
						Spacing="8">
				<newExp:TextBlock Text="New experience: this TextBlock is included because the 'NewExperience' flag was true at parse time."
								  TextWrapping="Wrap" />
				<legacy:TextBlock Text="Legacy mode: this TextBlock is included because the 'LegacyMode' flag was true at parse time."
								  TextWrapping="Wrap" />
			</StackPanel>
			""";

		var conditionalAttributesXaml = $$"""
			<TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					   xmlns:newExp="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(NewExperience)"
					   xmlns:legacy="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(LegacyMode)"
					   newExp:Text="Active flag: NewExperience"
					   legacy:Text="Active flag: LegacyMode"
					   FontSize="18" />
			""";

		var conditionalSettersXaml = $$"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:newExp="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(NewExperience)"
						xmlns:legacy="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{FeatureFlagConditionFullName}}(LegacyMode)"
						Spacing="8">
				<StackPanel.Resources>
					<Style x:Key="ConditionalHeadingStyle" TargetType="TextBlock">
						<newExp:Setter Property="FontWeight" Value="SemiBold" />
						<newExp:Setter Property="FontSize" Value="28" />
						<legacy:Setter Property="FontWeight" Value="Normal" />
						<legacy:Setter Property="FontSize" Value="14" />
					</Style>
				</StackPanel.Resources>
				<TextBlock Style="{StaticResource ConditionalHeadingStyle}" Text="Heading styled with conditional Setters" />
			</StackPanel>
			""";

		ConditionalElementsHost.Child = (Microsoft.UI.Xaml.UIElement)XamlReader.Load(conditionalElementsXaml);
		ConditionalAttributesHost.Child = (Microsoft.UI.Xaml.UIElement)XamlReader.Load(conditionalAttributesXaml);
		ConditionalSettersHost.Child = (Microsoft.UI.Xaml.UIElement)XamlReader.Load(conditionalSettersXaml);
	}
}
