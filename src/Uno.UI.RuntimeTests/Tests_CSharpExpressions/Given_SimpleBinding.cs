#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests_CSharpExpressions.Pages;

namespace Uno.UI.RuntimeTests.Tests_CSharpExpressions;

/// <summary>
/// T026 (US1) — runtime tests for simple-path C# expression lowering on
/// <see cref="SimpleBindingPage"/>. Exercises the post-refactor architecture where every
/// C# expression routes through the existing {x:Bind} pipeline, rooted on the page
/// itself (x:Class), with compile-time mode inference (TwoWay when the leaf is settable,
/// OneWay otherwise).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_SimpleBinding
{
	[TestMethod]
	public async Task When_TwoWaySimplePath_UpdatesOnPageChange()
	{
		var SUT = new SimpleBindingPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual("Ada", SUT.FirstNameBox.Text);

		SUT.FirstName = "Grace";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("Grace", SUT.FirstNameBox.Text);
	}

	[TestMethod]
	public async Task When_OneWayComputed_UpdatesOnDependencyChange()
	{
		var SUT = new SimpleBindingPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual("Ada Lovelace", SUT.FullNameLabel.Text);

		SUT.FirstName = "Grace";
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("Grace Lovelace", SUT.FullNameLabel.Text);
	}

	[TestMethod]
	public async Task When_ThisCapture_IsOneWayFromPageMember()
	{
		var SUT = new SimpleBindingPage();
		await UITestHelper.Load(SUT);

		Assert.AreEqual("Captured Once", SUT.ThisCapturedLabel.Text);
	}
}
