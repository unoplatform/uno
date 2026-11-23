using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// With <c>UnoXamlBinaryFormat</c>, XAML objects are built as XBF node streams instead of C#.
/// </summary>
[TestClass]
public class Given_Xbf
{
	private static readonly Dictionary<string, string> _xbfEnabled = new() { ["build_property.UnoXamlBinaryFormat"] = "true" };

	private const string MainPageCodeBehind = """
		using Microsoft.UI.Xaml;
		using Microsoft.UI.Xaml.Controls;

		namespace TestRepro;

		public sealed partial class MainPage : Page
		{
			public MainPage()
			{
				this.InitializeComponent();
			}

			private void OnClick(object sender, RoutedEventArgs e) { }
		}
		""";

	[TestMethod]
	public async Task When_Page_With_Names_And_Events()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
				<Grid x:Name="Root" Padding="12,4" RowSpacing="8">
					<Grid.RowDefinitions>
						<RowDefinition Height="Auto" />
						<RowDefinition Height="*" />
					</Grid.RowDefinitions>
					<Button x:Name="Action" Content="Go" Click="OnClick" />
					<TextBlock Grid.Row="1" Text="{Binding Content, ElementName=Action}" Opacity="0.5" />
				</Grid>
			</Page>
			""");

		await new Verify.Test(pageFile)
		{
			TestState = { Sources = { MainPageCodeBehind } },
			GlobalConfigOverride = _xbfEnabled,
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources().RunAsync();
	}

	[TestMethod]
	public async Task When_Dictionary_With_Style_And_Template()
	{
		var dictionaryFile = new XamlFile("Styles.xaml", """
			<ResourceDictionary
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<SolidColorBrush x:Key="AccentBrush" Color="#FF336699" />
				<Style x:Key="CardButton" TargetType="Button">
					<Setter Property="Padding" Value="8" />
					<Setter Property="Background" Value="{ThemeResource AccentBrush}" />
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate TargetType="Button">
								<Border x:Name="Chrome" Background="{TemplateBinding Background}" CornerRadius="4">
									<VisualStateManager.VisualStateGroups>
										<VisualStateGroup x:Name="CommonStates">
											<VisualState x:Name="Normal" />
											<VisualState x:Name="PointerOver">
												<VisualState.Setters>
													<Setter Target="Chrome.Opacity" Value="0.8" />
												</VisualState.Setters>
											</VisualState>
										</VisualStateGroup>
									</VisualStateManager.VisualStateGroups>
									<ContentPresenter Content="{TemplateBinding Content}" Margin="{TemplateBinding Padding}" />
								</Border>
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
			</ResourceDictionary>
			""");

		await new Verify.Test(dictionaryFile)
		{
			TestState = { Sources = { "namespace TestRepro { }" } },
			GlobalConfigOverride = _xbfEnabled,
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources().RunAsync();
	}

	/// <summary>
	/// What the node stream cannot express yet is still built in C#, for the whole file of an x:Class.
	/// </summary>
	[TestMethod]
	public async Task When_Page_With_xBind_Falls_Back()
	{
		var pageFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock Text="{x:Bind Tag}" />
			</Page>
			""");

		await new Verify.Test(pageFile)
		{
			TestState = { Sources = { MainPageCodeBehind } },
			GlobalConfigOverride = _xbfEnabled,
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
		}.AddGeneratedSources().RunAsync();
	}
}
