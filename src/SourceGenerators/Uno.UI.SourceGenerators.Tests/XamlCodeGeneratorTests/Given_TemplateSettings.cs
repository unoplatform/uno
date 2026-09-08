using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_TemplateSettings
{
	[TestMethod]
	public async Task When_Template_Has_NonDependencyObject_Collection()
	{
		// Hot reload turns the apply blocks into class-level ApplyTo_* methods, which take the
		// materialization settings as a parameter rather than closing over them. ShadowCollection is
		// not a DependencyObject, so it has no templated parent of its own to receive - but the apply
		// call for the Shadow inside it is emitted into its body and passes __settings along, so the
		// parameter has to be there anyway.
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page x:Class="TestRepro.MainPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      xmlns:local="using:TestRepro">
			  <Page.Resources>
			    <Color x:Key="ShadowColor">#7a67f8</Color>
			    <DataTemplate x:Key="TemplateWithCollection">
			      <local:ShadowHost>
			        <local:ShadowHost.Shadows>
			          <local:ShadowCollection x:Name="Shadows">
			            <local:Shadow Radius="20" Color="{StaticResource ShadowColor}" />
			          </local:ShadowCollection>
			        </local:ShadowHost.Shadows>
			      </local:ShadowHost>
			    </DataTemplate>
			  </Page.Resources>
			  <ListView />
			</Page>
			""");

		var configOverride = new Dictionary<string, string> { { "build_property.UnoForceHotReloadCodeGen", "true" } };

		var test = new Verify.Test(xamlFile)
		{
			TestState =
			{
				Sources =
				{
					"""
					using System.Collections.ObjectModel;
					using Microsoft.UI.Xaml;
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class MainPage : Page
						{
							public MainPage()
							{
								this.InitializeComponent();
							}
						}

						public partial class Shadow : DependencyObject
						{
							public double Radius
							{
								get => (double)GetValue(RadiusProperty);
								set => SetValue(RadiusProperty, value);
							}

							public static readonly DependencyProperty RadiusProperty =
								DependencyProperty.Register(nameof(Radius), typeof(double), typeof(Shadow), new PropertyMetadata(0d));

							public Windows.UI.Color Color
							{
								get => (Windows.UI.Color)GetValue(ColorProperty);
								set => SetValue(ColorProperty, value);
							}

							public static readonly DependencyProperty ColorProperty =
								DependencyProperty.Register(nameof(Color), typeof(Windows.UI.Color), typeof(Shadow), new PropertyMetadata(default(Windows.UI.Color)));
						}

						// Deliberately not a DependencyObject, mirroring Uno.Toolkit's ShadowCollection.
						public class ShadowCollection : ObservableCollection<Shadow>
						{
						}

						public partial class ShadowHost : Control
						{
							public ShadowCollection Shadows
							{
								get => (ShadowCollection)GetValue(ShadowsProperty);
								set => SetValue(ShadowsProperty, value);
							}

							public static readonly DependencyProperty ShadowsProperty =
								DependencyProperty.Register(nameof(Shadows), typeof(ShadowCollection), typeof(ShadowHost), new PropertyMetadata(null));
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.Current.WithUnoPackage(),
			GlobalConfigOverride = configOverride,
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
