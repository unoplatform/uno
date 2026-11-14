#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// One faulty automation peer must not take the whole semantic DOM down.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_WasmAccessibilityResilience
{
	[TestCleanup]
	public void Cleanup()
	{
		DisableAccessibility();
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_Peer_Throws_During_Initial_Build_Then_Rest_Of_Tree_Is_Built()
	{
		var before = new Button { Content = "Before" };
		var faulty = new ThrowingPeerControl { Content = new Button { Content = "Inside faulty" } };
		var after = new Button { Content = "After" };
		await UITestHelper.Load(new StackPanel { Children = { before, faulty, after } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(after), message: "Elements after the faulty peer should still be exposed.");

		Assert.IsTrue(SemanticElementExists(before));
		Assert.AreEqual("1", InvokeBrowserJs("(function(){return String(document.querySelectorAll('#uno-focus-sentinel-start').length);})()"), "The subsystems initialized after the build should be set up.");
		Assert.AreEqual("1", InvokeBrowserJs("(function(){return String(document.querySelectorAll('#uno-live-region-polite').length);})()"), "The live regions should be set up.");
	}

	[TestMethod]
	public async Task When_Peer_Throws_During_Dynamic_Add_Then_Siblings_Are_Exposed()
	{
		await EnsureAccessibilityEnabledAsync();

		var faulty = new ThrowingPeerControl { Content = new Button { Content = "Inside faulty" } };
		var after = new Button { Content = "After" };
		await UITestHelper.Load(new StackPanel { Children = { faulty, after } });

		await UITestHelper.WaitFor(() => SemanticElementExists(after), message: "Elements after the faulty peer should still be exposed.");
	}

	private sealed partial class ThrowingPeerControl : ContentControl
	{
		protected override AutomationPeer OnCreateAutomationPeer() => new ThrowingPeer(this);

		private sealed class ThrowingPeer : FrameworkElementAutomationPeer
		{
			public ThrowingPeer(ThrowingPeerControl owner) : base(owner) { }

			protected override int GetSizeOfSetCore() => throw new InvalidOperationException("Faulty peer for testing.");
		}
	}
}
#endif
