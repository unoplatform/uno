#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

partial class Given_WasmAriaRelations
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Relation_Query_Reenters_Then_Latest_Relationships_Are_Applied(bool clearDescription)
	{
#if HAS_UNO
		var original = new TextBlock { Text = "Original help" };
		var replacement = new TextBlock { Text = "Current help" };
		var source = new ReentrantRelationButton { Content = "Relation source" };
		var targets = new DependencyObjectCollection { original };
		source.SetValue(AutomationProperties.DescribedByProperty, targets);
		var panel = new StackPanel { Children = { source, original, replacement } };

		try
		{
			await UITestHelper.Load(panel);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(replacement)
					&& GetSemanticAttribute(source, "aria-describedby") == GetSemanticElementId(original),
				timeoutMS: 5000,
				message: "The initial relation must resolve before the reentrant query.");

			var queryMutations = 0;
			source.WhenReadingDescription = () =>
			{
				queryMutations++;
				targets.Clear();
				if (!clearDescription)
				{
					targets.Add(replacement);
				}
			};
			source.SetValue(AutomationProperties.FlowsToProperty, new DependencyObjectCollection { replacement });

			var expectedDescription = clearDescription ? string.Empty : GetSemanticElementId(replacement);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(source, "aria-describedby") == expectedDescription
					&& GetSemanticAttribute(source, "aria-flowto") == GetSemanticElementId(replacement),
				timeoutMS: 5000,
				message: "A reentrant relation change must supersede the outer query's stale snapshot.");
			Assert.AreEqual(1, queryMutations);
			Assert.AreEqual(!clearDescription, SemanticElementHasAttribute(source, "aria-describedby"));

			panel.Children.Remove(original);
			panel.Children.Add(original);
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(expectedDescription, GetSemanticAttribute(source, "aria-describedby"),
				"Refreshing the old target must not resurrect an obsolete relationship.");

			source.ClearValue(AutomationProperties.FlowsToProperty);
			targets.Clear();
			await UITestHelper.WaitFor(
				() => !SemanticElementHasAttribute(source, "aria-describedby")
					&& !SemanticElementHasAttribute(source, "aria-flowto"),
				timeoutMS: 5000,
				message: "The final relationship removal must clear both DOM IDREFs.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			await UITestHelper.WaitForIdle();
			ResetAccessibilityThroughDom();
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	public async Task When_Detached_Relation_Subtree_Is_Only_Materialized_After_Attachment()
	{
#if HAS_UNO
		try
		{
			EnableAccessibilityThroughDom();
			var target = new TextBlock { Text = "Detached help" };
			var source = new Button { Content = "Detached source" };
			source.SetValue(AutomationProperties.DescribedByProperty, new DependencyObjectCollection { target });
			var panel = new StackPanel { Children = { source, target } };
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(panel.IsLoaded);
			Assert.IsFalse(SemanticElementExists(source), "Constructing a detached subtree must not publish its source node.");
			Assert.IsFalse(SemanticElementExists(target), "Constructing a detached subtree must not publish its relation target.");

			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(
				() => SemanticElementExists(source)
					&& SemanticElementExists(target)
					&& GetSemanticAttribute(source, "aria-describedby") == GetSemanticElementId(target),
				timeoutMS: 5000,
				message: "Attaching the complete subtree must materialize its nodes and resolve their relationship.");

			TestServices.WindowHelper.WindowContent = null;
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(source));
			Assert.IsFalse(SemanticElementExists(target));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			await UITestHelper.WaitForIdle();
			ResetAccessibilityThroughDom();
		}
#endif
	}

#if HAS_UNO
	private sealed class ReentrantRelationButton : Button
	{
		public Action? WhenReadingDescription { get; set; }

		protected override AutomationPeer OnCreateAutomationPeer() => new ReentrantRelationButtonPeer(this);
	}

	private sealed class ReentrantRelationButtonPeer : ButtonAutomationPeer
	{
		public ReentrantRelationButtonPeer(ReentrantRelationButton owner) : base(owner)
		{
		}

		protected override IEnumerable<AutomationPeer> GetDescribedByCore()
		{
			var snapshot = base.GetDescribedByCore().ToArray();
			var owner = (ReentrantRelationButton)Owner;
			var callback = owner.WhenReadingDescription;
			owner.WhenReadingDescription = null;
			callback?.Invoke();
			return snapshot;
		}
	}
#endif
}
