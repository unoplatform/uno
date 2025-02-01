using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Xuid
{
	[TestMethod]
	public async Task When_Xuid_Basic()
	{
		var xamlFile = new XamlFile("ContentDialog1.xaml", """
		                                             <ContentDialog
		                                             	x:Class="TestRepro.XuidGeneratorError"
		                                             	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		                                             	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		                                             	x:Uid="XuidGeneratorErrorUid">
		                                             </ContentDialog>
		                                             """);

		var resourceFile = new ResourceFile("en", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			  <data name="XuidGeneratorErrorUid.PrimaryButtonText" xml:space="preserve">
			    <value>SomeValue</value>
			  </data>
			</root>
			""");

		var test = new Verify.Test([xamlFile], [resourceFile])
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;
					
					namespace TestRepro;
					public sealed partial class XuidGeneratorError : ContentDialog
					{
						public XuidGeneratorError()
						{
							this.InitializeComponent();
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.CurrentAndroid.WithUnoPackage(),
			DisableBuildReferences = true,
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
