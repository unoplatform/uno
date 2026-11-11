using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.HotReload;
using UnoApp50;
using _HR = Uno.UI.RuntimeTests.Tests.HotReload.HotReloadHelper;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_CSharpLatestSyntax : BaseTestClass
{
	[TestMethod]
	[Description(
		"An edit inside a method written with the SDK's default C# version (C# 15 on net11) applies: the dev-server " +
		"compiler must parse that version, not only tolerate it in untouched files (spec 061 G2, #26316).")]
	public async Task When_EditingMethodUsingLatestSyntax_Then_UpdateApplies()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
		var file = Path.Combine(_HR.GetProjectPath(), "CSharpLatestSyntax.cs");

		Assert.AreEqual(3, CSharpLatestSyntax.CountUntilFirstSkip());

		await using (await _HR.UpdateAsync([new FileEdit(file, "if (j == 1)", "if (j == 2)")], ct))
		{
			Assert.AreEqual(6, CSharpLatestSyntax.CountUntilFirstSkip(), "The edited method body was not applied.");
		}
	}
}
