#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
public sealed class AdapterEscapingTests
{
	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void CreateWrapperScript_PreservesShellMetacharacters()
	{
		var script = MacAdapter.CreateWrapperScript(
			"/usr/local/bin/dot'net",
			"/tmp/app's.dll",
			"sample=Category/My\"Test'$HOME");

		script.Should().Be(
			"#!/bin/bash\n" +
			"exec '/usr/local/bin/dot'\\''net' '/tmp/app'\\''s.dll' 'sample=Category/My\"Test'\\''$HOME'\n");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void EscapeCssAttribute_EscapesSelectorDelimiters()
	{
		var escaped = WasmAdapter.EscapeCssAttribute("a\\b\"]");

		escaped.Should().Be("a\\\\b\\\"\\]");
	}
}
