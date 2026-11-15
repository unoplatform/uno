using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_SnapshotNaming
{
	[TestMethod]
	public async Task When_Sanitized_Xaml_Names_Collide()
	{
		// A.Page.xaml and A_Page.xaml both sanitize to A_Page, so the hash the generator appends is the
		// only thing separating their two generated sources. The snapshot names have to keep it, or both
		// collapse to one file and the second expectation cannot be loaded back.
		var dotted = new XamlFile("A.Page.xaml", """
			<Page x:Class="TestRepro.DottedPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			  <TextBlock Text="Dotted" />
			</Page>
			""");

		var underscored = new XamlFile("A_Page.xaml", """
			<Page x:Class="TestRepro.UnderscoredPage"
			      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			  <TextBlock Text="Underscored" />
			</Page>
			""");

		var test = new Verify.Test([dotted, underscored])
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro
					{
						public sealed partial class DottedPage : Page
						{
							public DottedPage() => this.InitializeComponent();
						}

						public sealed partial class UnderscoredPage : Page
						{
							public UnderscoredPage() => this.InitializeComponent();
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
