using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;
using Uno.UI.SourceGenerators.XamlGenerator;

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_LocalizationResources
{
	// Hot Reload rejects a change of the UnoHasLocalizationResources assembly attribute (ENC0003),
	// so it must not depend on whether the project's only .resw currently parses.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_Only_Resw_Is_Malformed()
	{
		// The generator caches resource files per process: a locale folder no other test uses keeps this file
		// from falling back to the keys another test parsed for the same path.
		var resourceFile = new ResourceFile("qps-plocm", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			  <data name="MyKey" xml:space="preserve">
			    <value>SomeValue</value>
			</root>
			""");

		var test = CreateTest(resourceFile);

		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0003")
				.WithArguments("Unable to parse resource file [//Project/0/Strings/qps-plocm/Resources.resw], make sure it is a valid resw file. (The 'data' start tag on line 3 position 4 does not match the end tag of 'root'. Line 5, position 3.)"));

		await test.RunAsync();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_Resw_Has_No_Entries()
	{
		var resourceFile = new ResourceFile("en", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			</root>
			""");

		// True is intended: the attribute follows whether the project has resource files, not their content.
		var test = CreateTest(resourceFile);

		await test.RunAsync();
	}

	// While a .resw is being edited, Hot Reload regenerates the pages using it: they must keep their x:Uid lookups.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_Resw_Breaks_After_Parsing()
	{
		// A locale folder no other test uses, so the last version of this file that parsed is the one below.
		var valid = new ResourceFile("qps-ploc", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			  <data name="Greeting.Text" xml:space="preserve">
			    <value>Hello</value>
			  </data>
			</root>
			""");
		var broken = valid with { Contents = valid.Contents.Replace("</root>", "") };

		await CreatePageTest(valid).RunAsync();

		// Both runs are checked against the same snapshots: the x:Uid lookup survives the broken file.
		var test = CreatePageTest(broken);
		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0003")
				.WithArguments("Unable to parse resource file [//Project/0/Strings/qps-ploc/Resources.resw], make sure it is a valid resw file. (Unexpected end of file has occurred. The following elements are not closed: root. Line 6, position 1.) Until it parses, the keys of its last valid version are used."));

		await test.RunAsync();
	}

	// The file may stay broken for longer than the generator keeps unused cache entries.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_Resw_Breaks_After_Cache_Expired()
	{
		var valid = new ResourceFile("qps-ploca", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			  <data name="Greeting.Text" xml:space="preserve">
			    <value>Hello</value>
			  </data>
			</root>
			""");
		var broken = valid with { Contents = valid.Contents.Replace("</root>", "") };

		await CreatePageTest(valid).RunAsync();

		var test = CreatePageTest(broken);
		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0003")
				.WithArguments("Unable to parse resource file [//Project/0/Strings/qps-ploca/Resources.resw], make sure it is a valid resw file. (Unexpected end of file has occurred. The following elements are not closed: root. Line 6, position 1.) Until it parses, the keys of its last valid version are used."));

		var lifetime = XamlCodeGeneration.CacheEntryLifetime;
		XamlCodeGeneration.CacheEntryLifetime = TimeSpan.Zero;
		try
		{
			await test.RunAsync();
		}
		finally
		{
			XamlCodeGeneration.CacheEntryLifetime = lifetime;
		}
	}

	// With no earlier parse to fall back to (e.g. the IDE started on the broken file), the page has no lookup.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26364")]
	public async Task When_Resw_Broken_From_Start()
	{
		var broken = new ResourceFile("qps-plocb", "Resources.resw",
			"""
			<?xml version="1.0" encoding="utf-8"?>
			<root>
			  <data name="Greeting.Text" xml:space="preserve">
			    <value>Hello</value>
			  </data>
			""");

		var test = CreatePageTest(broken);
		test.ExpectedDiagnostics.Add(
			DiagnosticResult.CompilerError("UXAML0003")
				.WithArguments("Unable to parse resource file [//Project/0/Strings/qps-plocb/Resources.resw], make sure it is a valid resw file. (Unexpected end of file has occurred. The following elements are not closed: root. Line 5, position 10.)"));

		await test.RunAsync();
	}

	private static Verify.TestBase CreateTest(ResourceFile resourceFile, [CallerMemberName] string testMethodName = "")
		=> new Verify.Test([], [resourceFile], testMethodName: testMethodName)
		{
			TestState =
			{
				Sources = { "namespace TestRepro { internal class C { } }" },
			},
			ReferenceAssemblies = _Dotnet.CurrentAndroid.WithUnoPackage(),
		}.AddGeneratedSources();

	private static Verify.TestBase CreatePageTest(ResourceFile resourceFile, [CallerMemberName] string testMethodName = "")
	{
		var xamlFile = new XamlFile("MainPage.xaml", """
			<Page
				x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock x:Uid="Greeting" />
			</Page>
			""");

		return new Verify.Test([xamlFile], [resourceFile], testMethodName: testMethodName)
		{
			TestState =
			{
				Sources =
				{
					"""
					using Microsoft.UI.Xaml.Controls;

					namespace TestRepro;
					public sealed partial class MainPage : Page
					{
						public MainPage()
						{
							this.InitializeComponent();
						}
					}
					"""
				}
			},
			ReferenceAssemblies = _Dotnet.CurrentAndroid.WithUnoPackage(),
		}.AddGeneratedSources();
	}
}
