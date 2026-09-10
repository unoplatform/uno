#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Runtime tests for the geometry and placement of Skia-WASM semantic nodes created through the dynamic
/// (post-enable) path: a node must carry the on-screen bounds of the UIElement it describes, sit under its
/// nearest semantic ancestor, and never exist for an element under a Collapsed ancestor. Every test enables
/// accessibility before building its tree, so the one-shot build-time path (CreateAOM) is out of scope here.
/// </summary>
[TestClass]
public class Given_AccessibleGeometry
{
#if HAS_UNO
	private const double Tolerance = 1.5;

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Button_Added_To_Live_Panel_Then_Semantic_Rect_Matches_Layout()
	{
		await EnsureAccessibilityEnabledAsync();

		var host = new Grid { Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var button = CreateNamedButton("Geometry Button", 120, 40, new Thickness(37, 23, 0, 0));

		try
		{
			await UITestHelper.Load(host);

			// The parent is live at add time: the node is created immediately and sized by the arrange.
			host.Children.Add(button);
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the button's semantic node.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Then_Semantic_Rect_Matches_Layout()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Detached Then Attached", 90, 36, new Thickness(41, 29, 0, 0));

		// Build the subtree bottom-up while nothing is in the live tree, as generated XAML does.
		var inner = new StackPanel();
		inner.Children.Add(button);
		var outer = new Grid { Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		outer.Children.Add(inner);

		try
		{
			await UITestHelper.Load(outer);
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the attached button's semantic node.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Then_Node_Is_Under_Semantic_Ancestor()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Grouped Button");

		// The WASM bridge emits a named, peer-less Panel as a role="group" node (see IsSemanticElement), so
		// the group is the button's nearest semantic ancestor and its node must be the button's DOM parent.
		var group = new StackPanel();
		AutomationProperties.SetName(group, "Detached Group");
		group.Children.Add(button);
		var outer = new Grid();
		outer.Children.Add(group);

		try
		{
			await UITestHelper.Load(outer);
			await UITestHelper.WaitFor(() => SemanticElementExists(button) && SemanticElementExists(group), timeoutMS: 5000, message: "Timed out waiting for the group and button semantic nodes.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(GetSemanticElementId(group), GetSemanticParentId(button), "A control built inside a detached named group must be parented to that group's semantic node once attached.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Subtree_Built_Detached_Then_Attached_Under_Collapsed_Ancestor_Then_No_Semantic_Node()
	{
		await EnsureAccessibilityEnabledAsync();

		var button = CreateNamedButton("Hidden Ancestor Button");
		var inner = new StackPanel();
		inner.Children.Add(button);
		var hiddenHost = new Border { Visibility = Visibility.Collapsed, Child = inner };

		var sibling = CreateNamedButton("Visible Sibling");

		var panel = new StackPanel();
		panel.Children.Add(hiddenHost);
		panel.Children.Add(sibling);

		try
		{
			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(() => SemanticElementExists(sibling), timeoutMS: 5000, message: "Timed out waiting for the visible sibling's semantic node.");
			await UITestHelper.WaitForIdle();

			Assert.IsTrue(SemanticElementExists(sibling), "The visible sibling must emit a semantic node.");
			Assert.IsFalse(SemanticElementExists(button), "A control under a Collapsed ancestor must not be exposed: it is never arranged, so its node would be a zero-size phantom at the root.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Child_Added_Under_Live_Collapsed_Ancestor_Then_No_Semantic_Node_Until_Shown()
	{
		await EnsureAccessibilityEnabledAsync();

		var inner = new StackPanel();
		var hiddenHost = new Border { Visibility = Visibility.Collapsed, Child = inner };
		var sibling = CreateNamedButton("Visible Sibling");
		var panel = new StackPanel();
		panel.Children.Add(hiddenHost);
		panel.Children.Add(sibling);

		var button = CreateNamedButton("Late Hidden Ancestor Button", 90, 36);

		try
		{
			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(() => SemanticElementExists(sibling), timeoutMS: 5000, message: "Timed out waiting for the visible sibling's semantic node.");

			// The parent is live but sits under a Collapsed ancestor: nothing may be emitted yet.
			inner.Children.Add(button);
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(button), "A control added under a live Collapsed ancestor must not be exposed while the ancestor is hidden.");

			hiddenHost.Visibility = Visibility.Visible;
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic node once the ancestor was shown.");
			await UITestHelper.WaitForIdle();

			AssertSemanticRectMatchesLayout(button);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Button CreateNamedButton(string name, double? width = null, double? height = null, Thickness? margin = null)
	{
		var button = new Button
		{
			Content = name,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		AutomationProperties.SetName(button, name);

		if (width is { } w)
		{
			button.Width = w;
		}

		if (height is { } h)
		{
			button.Height = h;
		}

		if (margin is { } m)
		{
			button.Margin = m;
		}

		return button;
	}

	private static void AssertSemanticRectMatchesLayout(FrameworkElement element)
	{
		var id = GetSemanticElementId(element);
		var rect = GetSemanticElementRect(element);
		Assert.IsNotNull(rect, $"The semantic node {id} for {element.GetType().Name} is missing.");

		var origin = element.TransformToVisual(null).TransformPoint(new Point(0, 0));

		Assert.AreEqual(origin.X, rect!.Value.X, Tolerance, $"{id}: x mismatch (semantic rect {rect}).");
		Assert.AreEqual(origin.Y, rect.Value.Y, Tolerance, $"{id}: y mismatch (semantic rect {rect}).");
		Assert.AreEqual(element.ActualWidth, rect.Value.Width, Tolerance, $"{id}: width mismatch (semantic rect {rect}).");
		Assert.AreEqual(element.ActualHeight, rect.Value.Height, Tolerance, $"{id}: height mismatch (semantic rect {rect}).");
	}
#endif
}
