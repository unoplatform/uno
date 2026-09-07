using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// WASM (ARIA/DOM) tests for WA-02: aria-describedby / aria-controls / aria-flowto must only
	/// reference related elements that actually have a semantic node (no dangling IDREFs). Kept in a
	/// separate class from <see cref="Given_WasmAria"/> because its Collapsed relation target leaves
	/// residual accessibility-layer drain state that can perturb the aria-labelledby ordering of a
	/// subsequent same-class test; the two classes each pass as a unit.
	/// </summary>
	[TestClass]
	public class Given_WasmAriaRelations
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		[DataRow("aria-describedby")]
		[DataRow("aria-controls")]
		[DataRow("aria-flowto")]
		public async Task When_Related_Target_Follows_Source_Then_IdRef_Tracks_Its_Lifetime(string attribute)
		{
#if HAS_UNO
			var target = new TextBlock { Text = "Related target" };
			var source = new Button { Content = "Relation source" };
			var property = attribute switch
			{
				"aria-describedby" => AutomationProperties.DescribedByProperty,
				"aria-controls" => AutomationProperties.ControlledPeersProperty,
				"aria-flowto" => AutomationProperties.FlowsToProperty,
				_ => throw new ArgumentOutOfRangeException(nameof(attribute)),
			};
			source.SetValue(property, attribute == "aria-controls"
				? (object)new List<UIElement> { target }
				: new DependencyObjectCollection { target });
			var panel = new StackPanel { Children = { source, target } };

			try
			{
				await UITestHelper.Load(panel);
				EnableAccessibilityThroughDom();
				var targetId = GetSemanticElementId(target);
				await UITestHelper.WaitFor(
					() => SemanticElementExists(target) && GetSemanticAttribute(source, attribute) == targetId,
					timeoutMS: 5000,
					message: $"{attribute} must resolve a target registered after its source.");

				panel.Children.Remove(target);
				await UITestHelper.WaitFor(
					() => !SemanticElementExists(target) && !SemanticElementHasAttribute(source, attribute),
					timeoutMS: 5000,
					message: $"{attribute} must not retain a removed semantic target.");

				panel.Children.Add(target);
				await UITestHelper.WaitFor(
					() => SemanticElementExists(target) && GetSemanticAttribute(source, attribute) == targetId,
					timeoutMS: 5000,
					message: $"{attribute} must restore the relation when the target returns.");

				source.ClearValue(property);
				await UITestHelper.WaitFor(
					() => !SemanticElementHasAttribute(source, attribute),
					timeoutMS: 5000,
					message: $"{attribute} must be removed when the relation is cleared.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
				await UITestHelper.WaitForIdle();
			}
#endif
		}

#if HAS_UNO
		[TestCleanup]
		public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DescribedBy_Has_Nodeless_Target_Then_No_Dangling_IdRef()
		{
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			var visible = new TextBlock { Text = "Visible help" };
			AutomationProperties.SetName(visible, "Visible help");
			var collapsed = new TextBlock { Text = "Hidden help", Visibility = Visibility.Collapsed };
			AutomationProperties.SetName(collapsed, "Hidden help");
			var field = new TextBox();
			var describedByTargets = new DependencyObjectCollection { visible, collapsed };
			field.SetValue(AutomationProperties.DescribedByProperty, describedByTargets);
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

			describedByTargets.Remove(visible);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(field, "aria-describedby") == string.Empty,
				timeoutMS: 5000,
				message: "aria-describedby must be removed when every remaining target lacks a semantic node.");
		}
#endif
	}
}
