#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// T042 (US5, FR-013/FR-014): a ScrollViewer becomes a <c>role=region</c> landmark ONLY when it is
/// actually scrollable AND carries a real accessible name. A non-scrollable ScrollViewer, or a
/// scrollable-but-unnamed one, must NOT be exposed as a region (an unlabeled region is an axe
/// "region must have a name" violation), and <c>aria-roledescription</c> must never be emitted on an
/// element with no accessible name. These assertions run against the live WASM AOM/DOM.
/// </summary>
[TestClass]
public class Given_AccessibleScrollViewer
{
#if HAS_UNO
	/// <summary>
	/// A non-scrollable ScrollViewer (content fits the viewport) must NOT get role=region, even when
	/// it is named — there is nothing to scroll, so it is not a meaningful landmark (FR-013).
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Not_Scrollable_Then_No_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Content smaller than the viewport → nothing to scroll.
			Content = new Border { Width = 50, Height = 50 },
		};
		AutomationProperties.SetName(scrollViewer, "Fits Content");

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A non-scrollable ScrollViewer must NOT be exposed as role=region (FR-013).");
	}

	/// <summary>
	/// A scrollable, named ScrollViewer earns role=region and carries its accessible name as aria-label
	/// (FR-013/FR-014). This is the positive case ensuring the gate is not vacuously over-broad.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Scrollable_And_Named_Then_Labeled_Region()
	{
		const string name = "Article Body";
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Content much taller than the viewport → vertically scrollable.
			Content = new Border { Width = 50, Height = 2000 },
		};
		AutomationProperties.SetName(scrollViewer, name);

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A scrollable, named ScrollViewer must be exposed as role=region (FR-013).");
		Assert.AreEqual(name, GetSemanticAttribute(scrollViewer, "aria-label"),
			"A region's accessible name must be emitted as aria-label (FR-014).");
	}

	/// <summary>
	/// A scrollable but UNNAMED ScrollViewer must NOT get role=region — an unlabeled region is an axe
	/// "region must have a name" violation (FR-014). It still appears in the tree (as a plain div), but
	/// without the region role.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Scrollable_But_Unnamed_Then_No_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			// Scrollable, but no AutomationProperties.Name / labeled content.
			Content = new Border { Width = 50, Height = 2000 },
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"A scrollable but unnamed ScrollViewer must NOT be exposed as an unlabeled role=region (FR-014).");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Has_Only_Descendant_Text_Then_No_Named_Region()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new StackPanel
			{
				Children =
				{
					new TextBlock { Text = "Incidental descendant text" },
					new Border { Height = 2000 },
				},
			},
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");

		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"),
			"Descendant text must not be flattened into a ScrollViewer landmark name.");
		Assert.IsFalse(SemanticElementHasAttribute(scrollViewer, "aria-label"),
			"An unnamed ScrollViewer must not copy incidental descendant text into aria-label.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Name_Changes_Then_Region_Role_Upgrades_And_Downgrades()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Height = 2000 },
		};

		await UITestHelper.Load(scrollViewer);
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the dynamic ScrollViewer semantic node.");
		Assert.AreNotEqual("region", GetSemanticAttribute(scrollViewer, "role"));

		AutomationProperties.SetName(scrollViewer, "Dynamic article");
		await UITestHelper.WaitFor(
			() => GetSemanticAttribute(scrollViewer, "role") == "region",
			timeoutMS: 3000,
			message: "Adding an authored name did not promote the scrollable ScrollViewer to region.");

		AutomationProperties.SetName(scrollViewer, string.Empty);
		await UITestHelper.WaitFor(
			() => !SemanticElementHasAttribute(scrollViewer, "role") &&
				!SemanticElementHasAttribute(scrollViewer, "aria-label"),
			timeoutMS: 3000,
			message: "Removing the authored name left an unlabeled region.");
	}

	/// <summary>
	/// FR-014 invariant: aria-roledescription is not a substitute for an accessible name. An unnamed
	/// ScrollViewer (region candidate) must never emit aria-roledescription.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_ScrollViewer_Unnamed_Then_No_RoleDescription_Without_Name()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new Border { Width = 50, Height = 2000 },
		};

		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(scrollViewer), timeoutMS: 5000,
			message: "Timed out waiting for the ScrollViewer's semantic node.");
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(string.Empty, GetSemanticAttribute(scrollViewer, "aria-roledescription"),
			"aria-roledescription must never be emitted on an element with no accessible name (FR-014).");
	}





#endif
}
