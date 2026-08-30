using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// A custom markup extension may have its own <c>Name</c> property, which is a value rather than
/// an element name. https://github.com/unoplatform/uno/issues/24290
/// </summary>
[TestClass]
public class Given_MarkupExtensionNameProperty
{
	// The MRT path form used by `{local:ResourceString Name=...}` contains characters that are illegal in a
	// C# identifier, so leaking it into the ElementNameSubject field name broke the whole generated file.
	[TestMethod]
	public async Task When_MarkupExtension_Name_On_Lazy_Element()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Grid x:Name="LayoutRoot">
			<Button x:Name="ExitButton"
					Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"
					AutomationProperties.Name="{local:ResourceString Name=ExitButton/[using:Windows.UI.Xaml.Automation]AutomationProperties/Name}"
					ToolTipService.ToolTip="{local:ResourceString Name=ExitButton/[using:Windows.UI.Xaml.Controls]ToolTipService/ToolTip}"
					x:Load="False" />
		</Grid>
	</Page>
	""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Markup;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public sealed class ResourceStringExtension : MarkupExtension
						{
							public string Name { get; set; }

							protected override object ProvideValue() => Name;
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	// The same leak also poisons the x:Name cache: a Setter target must not resolve to a markup extension.
	[TestMethod]
	public async Task When_MarkupExtension_Name_Matches_VisualState_Setter_Target()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Grid>
			<VisualStateManager.VisualStateGroups>
				<VisualStateGroup x:Name="HoverStates">
					<VisualState x:Name="MemoryButtonsVisible">
						<VisualState.Setters>
							<Setter Target="MemoryHoverButtons.Opacity" Value="1" />
						</VisualState.Setters>
					</VisualState>
				</VisualStateGroup>
			</VisualStateManager.VisualStateGroups>
			<TextBlock Text="{local:ResourceString Name=MemoryHoverButtons}" />
		</Grid>
	</Page>
	""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Markup;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public sealed class ResourceStringExtension : MarkupExtension
						{
							public string Name { get; set; }

							protected override object ProvideValue() => Name;
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	// Regression guard for #21992: a control with a companion "<Name>Extension" markup extension in the
	// same namespace is an element, not a markup extension, so its x:Name must stay resolvable.
	[TestMethod]
	public async Task When_Element_Type_Has_Companion_Extension_Class()
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
	<Page
		x:Class="TestRepro.MainPage"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:local="using:TestRepro"
		xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
		xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
		mc:Ignorable="d">

		<Grid>
			<VisualStateManager.VisualStateGroups>
				<VisualStateGroup x:Name="States">
					<VisualState x:Name="Visible">
						<VisualState.Setters>
							<Setter Target="MyBadge.Opacity" Value="1" />
						</VisualState.Setters>
					</VisualState>
				</VisualStateGroup>
			</VisualStateManager.VisualStateGroups>
			<local:Badge x:Name="MyBadge" />
		</Grid>
	</Page>
	""");

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;
					using Microsoft.UI.Xaml.Markup;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class Badge : Control
						{
						}

						public sealed class BadgeExtension : MarkupExtension
						{
							protected override object ProvideValue() => null;
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
