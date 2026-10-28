using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// WASM (ARIA/DOM) accessibility tests for the parity-remediation fixes:
	/// WA-01 (role=generic is name-prohibited → promote named generics to role=group) and
	/// WA-04 (do not emit a flattened aria-label when a valid aria-labelledby IDREF is present).
	/// These assert on the emitted semantic DOM and only run on Skia-WASM.
	/// </summary>
	[TestClass]
	public class Given_WasmAria
	{
#if HAS_UNO
		[TestCleanup]
		public void Cleanup()
		{
			DisableAccessibility();

			// Reset the tree between tests so the accessibility/semantic DOM from one test does not
			// leak into the next (the semantic tree is a single shared DOM per app instance).
			TestServices.WindowHelper.WindowContent = null;
		}

		private static async Task ResetAsync()
		{
			// Start each test from a clean, settled semantic DOM. The tree is a single shared DOM per
			// app instance, and a previous test's elements (esp. collapsed / relation targets) can
			// otherwise still be draining when the next test loads, changing build ordering.
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Named_Generic_Then_Role_Group_Not_Generic()
		{
			await ResetAsync();

			// A ContentControl's peer reports the Custom control type (the base default), which maps to
			// role=generic. role=generic is ARIA name-prohibited, so a named one must be promoted to
			// role=group (which permits a name).
			var control = new ContentControl { Content = "content" };
			AutomationProperties.SetName(control, "My group");

			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();

			var role = GetSemanticAttribute(control, "role");
			Assert.AreNotEqual("generic", role, "A named element must not carry the name-prohibited role=generic.");
			Assert.AreEqual("group", role, "A named generic container should be promoted to role=group.");
			Assert.AreEqual("My group", GetSemanticAttribute(control, "aria-label"), "The name should be exposed as aria-label on the group.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Generic_Named_After_Creation_Then_Role_Group()
		{
			await ResetAsync();

			// Same as above, but the name arrives after the semantic node exists (e.g. a binding resolved
			// after the tree was built), so it goes through the live name update instead of creation.
			var control = new CustomPeerControl { Width = 50, Height = 50 };
			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"));

			AutomationProperties.SetName(control, "My group");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-label") == "My group", timeoutMS: 3000, message: "The name should be exposed as aria-label.");

			Assert.AreEqual("group", GetSemanticAttribute(control, "role"), "A named element must not carry the name-prohibited role=generic.");

			AutomationProperties.SetName(control, "");
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(control, "aria-label"), timeoutMS: 3000, message: "Clearing the name should remove aria-label.");

			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"), "An unnamed container goes back to role=generic.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Generic_Name_Becomes_Whitespace_Then_Role_Generic()
		{
			await ResetAsync();

			var control = new CustomPeerControl { Width = 50, Height = 50 };
			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();

			AutomationProperties.SetName(control, "My group");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "role") == "group", timeoutMS: 3000, message: "A named generic should become role=group.");

			// A whitespace-only name is dropped from aria-label, so it must not keep the node a group.
			AutomationProperties.SetName(control, "   ");
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(control, "aria-label"), timeoutMS: 3000, message: "A whitespace-only name should not be exposed as aria-label.");

			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"), "A whitespace-only name does not name the node.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Region_Landmark_Named_After_Creation_Then_Role_Region()
		{
			await ResetAsync();

			// A region landmark is only exposed once named (FR-014), so the name is what promotes it.
			var control = new CustomPeerControl { Width = 50, Height = 50 };
			AutomationProperties.SetLandmarkType(control, AutomationLandmarkType.Custom);
			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"), "An unnamed region landmark is not exposed.");

			AutomationProperties.SetName(control, "Results");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-label") == "Results", timeoutMS: 3000, message: "The name should be exposed as aria-label.");

			Assert.AreEqual("region", GetSemanticAttribute(control, "role"), "A named region landmark should be exposed.");

			AutomationProperties.SetName(control, "");
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(control, "aria-label"), timeoutMS: 3000, message: "Clearing the name should remove aria-label.");

			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"), "An unnamed region landmark is no longer exposed.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GridCell_Named_After_Creation_Then_Role_Stays_GridCell()
		{
			await ResetAsync();

			// A grid cell peer reports the Custom control type, but the factory emits it as role=gridcell.
			var control = new GridCellPeerControl { Width = 50, Height = 50 };
			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"));

			AutomationProperties.SetName(control, "Cell");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-label") == "Cell", timeoutMS: 3000, message: "The name should be exposed as aria-label.");

			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"), "A name change must not replace the factory-built role.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Standalone_Text_Gets_Name_Then_Paragraph_Has_No_Role()
		{
			await ResetAsync();

			var text = new TextBlock { Text = "Before" };
			await UITestHelper.Load(new StackPanel { Children = { text } });

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(text), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual("Before", GetSemanticTextContent(text));

			// An explicit name makes the TextBlock take the generic path at creation, but this node is already a <p>.
			AutomationProperties.SetName(text, "Named");
			await UITestHelper.WaitFor(() => GetSemanticTextContent(text) == "Named", timeoutMS: 3000,
				message: $"The paragraph must expose the name as its text (text='{GetSemanticTextContent(text)}').");

			Assert.IsFalse(SemanticElementHasAttribute(text, "role"), "A standalone text paragraph takes no role.");
			Assert.IsFalse(SemanticElementHasAttribute(text, "aria-label"), "A standalone text paragraph must not carry aria-label.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Standalone_Text_Changes_Then_Paragraph_Text_Updated_Without_AriaLabel()
		{
			await ResetAsync();

			var text = new TextBlock { Text = "Before" };
			await UITestHelper.Load(new StackPanel { Children = { text } });

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(text), timeoutMS: 5000, message: "Timed out waiting for the semantic element.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual("Before", GetSemanticTextContent(text));

			text.Text = "After";
			await UITestHelper.WaitFor(() => GetSemanticTextContent(text) == "After", timeoutMS: 3000,
				message: $"The paragraph must expose the new text (text='{GetSemanticTextContent(text)}').");

			// A paragraph is named by its content; aria-label is prohibited on it.
			Assert.IsFalse(SemanticElementHasAttribute(text, "aria-label"), "A standalone text paragraph must not carry aria-label.");
		}

		// The base peer reports the Custom control type (role=generic), like many app-defined controls.
		private class CustomPeerControl : ContentControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
		}

		private class GridCellPeerControl : ContentControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new GridCellPeer(this);

			private sealed class GridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider
			{
				public GridCellPeer(GridCellPeerControl owner) : base(owner) { }

				protected override AutomationControlType GetAutomationControlTypeCore()
					=> AutomationControlType.Custom;

				protected override object GetPatternCore(PatternInterface patternInterface)
					=> patternInterface == PatternInterface.GridItem ? this : base.GetPatternCore(patternInterface);

				public int Row => 0;
				public int Column => 0;
				public int RowSpan => 1;
				public int ColumnSpan => 1;
				public IRawElementProviderSimple ContainingGrid => null;
			}
		}

		private static string GetSemanticTextContent(Microsoft.UI.Xaml.UIElement element)
			=> InvokeBrowserJs($"String(document.getElementById('{GetSemanticElementId(element)}')?.textContent ?? '')");

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_LabeledBy_Has_Node_Then_LabelledBy_Set_And_AriaLabel_Absent()
		{
			await ResetAsync();

			var label = new TextBlock { Text = "Username" };
			AutomationProperties.SetName(label, "Username");
			var field = new TextBox();
			AutomationProperties.SetLabeledBy(field, label);
			var panel = new StackPanel { Children = { label, field } };

			await UITestHelper.Load(panel);
			label.GetOrCreateAutomationPeer();
			field.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(field) && SemanticElementExists(label), timeoutMS: 5000, message: "Timed out waiting for the semantic elements.");
			await UITestHelper.WaitForIdle();

			var labelledBy = GetSemanticAttribute(field, "aria-labelledby");
			Assert.AreEqual(GetSemanticElementId(label), labelledBy, "aria-labelledby should reference the labeller's semantic node.");
			// Poll for the settled state: aria-labelledby takes precedence and the competing aria-label
			// must end up removed (the async semantic-tree build can briefly set aria-label first).
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(field, "aria-label"), timeoutMS: 3000, message: "aria-label must be suppressed when aria-labelledby is present (no double naming).");
		}
#endif
	}
}
