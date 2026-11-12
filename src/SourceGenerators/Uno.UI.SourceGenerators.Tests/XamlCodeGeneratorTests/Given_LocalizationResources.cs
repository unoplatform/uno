using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Uno.UI.SourceGenerators.Tests.Verifiers;

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
		var resourceFile = new ResourceFile("en", "Resources.resw",
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
				.WithArguments("Unable to parse resource file [//Project/0/Strings/en/Resources.resw], make sure it is a valid resw file. (The 'data' start tag on line 3 position 4 does not match the end tag of 'root'. Line 5, position 3.)"));

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

		var test = CreateTest(resourceFile);

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
}
