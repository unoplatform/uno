using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
			// Reset the tree between tests so the accessibility/semantic DOM from one test does not
			// leak into the next (the semantic tree is a single shared DOM per app instance).
			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Named_Generic_Then_Role_Group_Not_Generic()
		{
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
		public async Task When_LabeledBy_Has_Node_Then_LabelledBy_Set_And_AriaLabel_Absent()
		{
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
			Assert.IsFalse(SemanticElementHasAttribute(field, "aria-label"), "aria-label must be suppressed when aria-labelledby is present (no double naming).");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DescribedBy_Has_Nodeless_Target_Then_No_Dangling_IdRef()
		{
			var visible = new TextBlock { Text = "Visible help" };
			AutomationProperties.SetName(visible, "Visible help");
			var collapsed = new TextBlock { Text = "Hidden help", Visibility = Visibility.Collapsed };
			AutomationProperties.SetName(collapsed, "Hidden help");
			var field = new TextBox();
			field.SetValue(AutomationProperties.DescribedByProperty, new List<DependencyObject> { visible, collapsed });
			var panel = new StackPanel { Children = { visible, collapsed, field } };

			await UITestHelper.Load(panel);
			visible.GetOrCreateAutomationPeer();
			field.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(field) && SemanticElementExists(visible), timeoutMS: 5000, message: "Timed out waiting for the semantic elements.");
			await UITestHelper.WaitForIdle();

			var describedBy = GetSemanticAttribute(field, "aria-describedby");
			Assert.AreEqual(GetSemanticElementId(visible), describedBy, "aria-describedby must reference only the target that has a semantic node.");
			Assert.IsFalse(describedBy.Contains(GetSemanticElementId(collapsed)), "A node-less (collapsed) target must not produce a dangling IDREF.");
		}
#endif
	}
}
