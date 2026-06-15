#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Runtime tests for heading-level mapping (FR-011 / US4). HTML only has &lt;h1&gt;-&lt;h6&gt;,
/// so the WinUI HeadingLevel is split: the &lt;hN&gt; tag is clamped to &lt;h6&gt; while
/// aria-level carries the true level (1-9). aria-level also live-syncs on a runtime
/// HeadingLevel change.
/// </summary>
[TestClass]
public class Given_AccessibleHeading
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(AutomationHeadingLevel.Level1, 1)]
	[DataRow(AutomationHeadingLevel.Level2, 2)]
	[DataRow(AutomationHeadingLevel.Level3, 3)]
	[DataRow(AutomationHeadingLevel.Level4, 4)]
	[DataRow(AutomationHeadingLevel.Level5, 5)]
	[DataRow(AutomationHeadingLevel.Level6, 6)]
	[DataRow(AutomationHeadingLevel.Level7, 7)]
	[DataRow(AutomationHeadingLevel.Level8, 8)]
	[DataRow(AutomationHeadingLevel.Level9, 9)]
	public async Task When_HeadingLevel_Set_Then_AriaMapper_Level_Is_True_Level(AutomationHeadingLevel headingLevel, int expectedLevel)
	{
		// The AriaMapper passes the true level (1-9) through to the factory; only the HTML
		// tag is clamped at the TS layer. This guards the C# side of the passthrough.
		var heading = new TextBlock { Text = "Heading" };
		AutomationProperties.SetHeadingLevel(heading, headingLevel);

		await UITestHelper.Load(heading);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(heading);
		Assert.IsNotNull(peer, "Heading should have an automation peer");

		Assert.AreEqual(SemanticElementType.Heading, AriaMapper.GetSemanticElementType(peer, heading), "An element with HeadingLevel set must classify as a Heading.");

		var attributes = AriaMapper.GetAriaAttributes(peer);
		Assert.AreEqual(expectedLevel, attributes.Level, "AriaMapper must carry the true heading level (1-9), never clamped to 6.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(AutomationHeadingLevel.Level1, 1, "h1")]
	[DataRow(AutomationHeadingLevel.Level2, 2, "h2")]
	[DataRow(AutomationHeadingLevel.Level3, 3, "h3")]
	[DataRow(AutomationHeadingLevel.Level4, 4, "h4")]
	[DataRow(AutomationHeadingLevel.Level5, 5, "h5")]
	[DataRow(AutomationHeadingLevel.Level6, 6, "h6")]
	public async Task When_HeadingLevel_1_To_6_Then_Tag_And_AriaLevel_Match(AutomationHeadingLevel headingLevel, int expectedLevel, string expectedTag)
	{
		var heading = new TextBlock { Text = "Section heading" };
		AutomationProperties.SetHeadingLevel(heading, headingLevel);

		await UITestHelper.Load(heading);
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(heading), timeoutMS: 5000, message: "Timed out waiting for the heading semantic element to be created.");

		Assert.AreEqual(expectedTag, GetSemanticElementTagName(heading), $"Heading level {expectedLevel} must emit a <{expectedTag}> tag.");
		Assert.AreEqual(expectedLevel.ToString(), GetSemanticAttribute(heading, "aria-level"), $"Heading level {expectedLevel} must expose aria-level={expectedLevel}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[DataRow(AutomationHeadingLevel.Level7, 7)]
	[DataRow(AutomationHeadingLevel.Level8, 8)]
	[DataRow(AutomationHeadingLevel.Level9, 9)]
	public async Task When_HeadingLevel_7_To_9_Then_Tag_Is_H6_But_AriaLevel_Is_True_Level(AutomationHeadingLevel headingLevel, int expectedLevel)
	{
		// FR-011: levels 7-9 have no HTML tag, so the tag clamps to <h6> while aria-level
		// preserves the true depth so assistive tech announces the real heading level.
		var heading = new TextBlock { Text = "Deep heading" };
		AutomationProperties.SetHeadingLevel(heading, headingLevel);

		await UITestHelper.Load(heading);
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(heading), timeoutMS: 5000, message: "Timed out waiting for the heading semantic element to be created.");

		Assert.AreEqual("h6", GetSemanticElementTagName(heading), $"Heading level {expectedLevel} must clamp the tag to <h6>.");
		Assert.AreEqual(expectedLevel.ToString(), GetSemanticAttribute(heading, "aria-level"), $"Heading level {expectedLevel} must still expose the true aria-level={expectedLevel}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_HeadingLevel_Changes_At_Runtime_Then_AriaLevel_Live_Syncs()
	{
		// T036 (best-effort live-sync): aria-level updates on a runtime HeadingLevel change.
		// The <hN> tag is created once (not re-created), so this asserts aria-level only.
		var heading = new TextBlock { Text = "Live heading" };
		AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level2);

		await UITestHelper.Load(heading);
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(heading), timeoutMS: 5000, message: "Timed out waiting for the heading semantic element to be created.");

		Assert.AreEqual("2", GetSemanticAttribute(heading, "aria-level"), "Initial aria-level must reflect the starting HeadingLevel.");

		AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level7);
		await UITestHelper.WaitFor(() => GetSemanticAttribute(heading, "aria-level") == "7", timeoutMS: 5000, message: "Timed out waiting for aria-level to live-sync to the new heading level.");

		Assert.AreEqual("7", GetSemanticAttribute(heading, "aria-level"), "A runtime HeadingLevel change must live-update aria-level to the true level (FR-011).");
	}

	private static void EnableAccessibilityThroughDom()
	{
		InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
	}

	// Targets the exact semantic element for a given element via its visual handle, mirroring the
	// id scheme used by the WASM runtime (uno-semantics-{handle}).
	private static string GetSemanticElementId(UIElement element)
		=> $"uno-semantics-{((long)element.Visual.Handle)}";

	private static bool SemanticElementExists(UIElement element)
		=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(element)}') ? '1' : '0';}})()") == "1";

	private static string GetSemanticAttribute(UIElement element, string attribute)
		=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('{attribute}') ?? '') : '';}})()");

	private static string GetSemanticElementTagName(UIElement element)
		=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? e.tagName.toLowerCase() : '';}})()");

	private static string InvokeBrowserJs(string javascript)
	{
		var runtimeType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly", throwOnError: false);
		Assert.IsNotNull(runtimeType, "Unable to locate Uno.Foundation.WebAssemblyRuntime at runtime.");

		var invokeJs = runtimeType.GetMethod("InvokeJS", new[] { typeof(string) });
		Assert.IsNotNull(invokeJs, "Unable to locate Uno.Foundation.WebAssemblyRuntime.InvokeJS(string).");

		return invokeJs.Invoke(obj: null, parameters: new object[] { javascript }) as string ?? string.Empty;
	}
#endif
}
