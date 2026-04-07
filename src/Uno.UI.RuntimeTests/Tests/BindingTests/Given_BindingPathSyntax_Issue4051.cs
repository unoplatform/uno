using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

// Repro tests for https://github.com/unoplatform/uno/issues/4051

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_BindingPathSyntax_Issue4051
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4051")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Binding_Path_Binding_Syntax_Works()
	{
		// Issue: {Binding Path={Binding}} syntax causes compile error in Uno XAML generator.
		// Expected: Should compile and behave the same as {Binding} or {Binding Path=.}.
		//
		// If this test COMPILES and RUNS, the bug is fixed.
		// If it FAILS TO BUILD with a compiler error, the bug is still present.

		var sut = new BindingPathSyntaxTest();
		sut.DataContext = "TestValue";

		WindowHelper.WindowContent = sut;
		await WindowHelper.WaitForLoaded(sut);
		await WindowHelper.WaitForIdle();

		// The TextBlock bound with {Binding Path={Binding}} should show the DataContext value
		var textBlock = sut.FindName("TestTextBlock") as TextBlock ?? throw new Exception("TestTextBlock not found");
		Assert.IsNotNull(textBlock);
		Assert.AreEqual("TestValue", textBlock.Text,
			$"Expected TextBlock.Text to be 'TestValue' (bound to DataContext via Path={{Binding}}), " +
			$"but got '{textBlock.Text}'.");
	}
}
