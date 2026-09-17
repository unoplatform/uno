#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Geometry contract for transformed elements and Skia-WASM semantic nodes. Bounds must compose the
/// complete element-to-root transform chain exactly once and remain synchronized as that chain changes.
/// </summary>
[TestClass]
public class Given_AccessibleGeometry
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Nested_Transforms_Then_TransformToVisual_Composes_Whole_Chain()
	{
		var target = new Border { Width = 40, Height = 20 };
		var inner = new Border
		{
			Margin = new Thickness(7, 11, 0, 0),
			RenderTransform = new TranslateTransform { X = 15, Y = 25 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = target
		};
		var outer = new Border
		{
			Margin = new Thickness(3, 5, 0, 0),
			RenderTransform = new TranslateTransform { X = 40, Y = 60 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = inner
		};
		var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { outer } };

		try
		{
			await UITestHelper.Load(root);

			var bounds = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));

			Assert.AreEqual(3 + 40 + 7 + 15, bounds.X, 0.5, "X must compose both margins and both translations exactly once.");
			Assert.AreEqual(5 + 60 + 11 + 25, bounds.Y, 0.5, "Y must compose both margins and both translations exactly once.");
			Assert.AreEqual(40, bounds.Width, 0.5, "Width must be unchanged by translations.");
			Assert.AreEqual(20, bounds.Height, 0.5, "Height must be unchanged by translations.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ancestor_Scaled_Then_TransformToVisual_Scales_Bounds()
	{
		var target = new Border { Width = 40, Height = 20 };
		var scaled = new Border
		{
			Margin = new Thickness(10, 20, 0, 0),
			RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 3 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = target
		};
		var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { scaled } };

		try
		{
			await UITestHelper.Load(root);

			var bounds = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));

			Assert.AreEqual(10, bounds.X, 0.5, "The scale origin is the top-left of the scaled ancestor, which sits at its margin.");
			Assert.AreEqual(20, bounds.Y, 0.5, "The scale origin is the top-left of the scaled ancestor, which sits at its margin.");
			Assert.AreEqual(80, bounds.Width, 0.5, "Width must be scaled by the ancestor ScaleX.");
			Assert.AreEqual(60, bounds.Height, 0.5, "Height must be scaled by the ancestor ScaleY.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

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

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24280")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Pruned_Ancestor_Moves_Then_Semantic_Rect_Follows()
	{
		var button = new Button { Content = "Target" };
		var pruned = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { pruned } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(SemanticElementExists(pruned), "The Border is expected to be pruned from the accessibility tree for this scenario to be meaningful.");

			var before = GetRequiredSemanticElementRect(button);

			pruned.Margin = new Thickness(0, 120, 0, 0);
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => Math.Abs(GetRequiredSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the pruned ancestor.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(120, after.Y - before.Y, 2, "The semantic rectangle must follow the offset of a pruned ancestor.");
			Assert.AreEqual(0, after.X - before.X, 2, "A vertical move must not shift the semantic rectangle horizontally.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24280")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Nested_Ancestors_Are_Transformed_Then_Semantic_Rect_Follows()
	{
		var button = new Button { Content = "Target" };
		var inner = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var outer = new Border { Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { outer } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			var before = GetRequiredSemanticElementRect(button);

			outer.RenderTransform = new TranslateTransform { X = 40, Y = 60 };
			inner.RenderTransform = new TranslateTransform { X = 15, Y = 25 };
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => Math.Abs(GetRequiredSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor render transforms.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(55, after.X - before.X, 2, "Both ancestor translations must be composed on X.");
			Assert.AreEqual(85, after.Y - before.Y, 2, "Both ancestor translations must be composed on Y.");
			Assert.AreEqual(before.Width, after.Width, 2, "A translation must not resize the semantic rectangle.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Ancestor_Is_Scaled_Then_Semantic_Rect_Is_Scaled()
	{
		var button = new Button { Content = "Target" };
		var scaled = new Border { Child = button, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid { Children = { scaled } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic element to be created.");
			await UITestHelper.WaitForIdle();

			var before = GetRequiredSemanticElementRect(button);
			Assert.IsTrue(before.Width > 0, "The semantic rectangle must have a non-zero width before scaling.");

			scaled.RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };
			await UITestHelper.WaitForIdle();
			await UITestHelper.WaitFor(() => GetRequiredSemanticElementRect(button).Width > before.Width + 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor scale.");

			var after = GetRequiredSemanticElementRect(button);

			Assert.AreEqual(before.Width * 2, after.Width, 2, "Width must be scaled by the ancestor ScaleX.");
			Assert.AreEqual(before.Height * 2, after.Height, 2, "Height must be scaled by the ancestor ScaleY.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Text_Controls_Are_Loaded_Then_Each_Owns_Its_Semantic_Node()
	{
		var autoSuggestBox = new AutoSuggestBox { Width = 200 };
		var textBox = new TextBox { Width = 200 };
		var passwordBox = new PasswordBox { Width = 200 };
		var comboBox = new ComboBox { Width = 200, ItemsSource = new[] { "a", "b" } };
		var root = new StackPanel { Children = { autoSuggestBox, textBox, passwordBox, comboBox } };

		try
		{
			await UITestHelper.Load(root);

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(autoSuggestBox)
					&& SemanticElementExists(textBox)
					&& SemanticElementExists(passwordBox)
					&& SemanticElementExists(comboBox),
				timeoutMS: 5000,
				message: "Timed out waiting for the semantic elements to be created.");
			await UITestHelper.WaitForIdle();

			var innerTextBox = FindFirstTextBox(autoSuggestBox);
			Assert.IsNotNull(innerTextBox, "The AutoSuggestBox template is expected to contain a TextBox.");
			Assert.IsTrue(SemanticElementExists(innerTextBox), "The templated TextBox must keep its own semantic node rather than borrowing the AutoSuggestBox one.");
			Assert.AreNotEqual(GetSemanticElementId(autoSuggestBox), GetSemanticElementId(innerTextBox), "The templated TextBox must not be collapsed onto the AutoSuggestBox node.");
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

	private static TextBox? FindFirstTextBox(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is TextBox textBox)
			{
				return textBox;
			}

			if (FindFirstTextBox(child) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static Rect GetRequiredSemanticElementRect(UIElement element)
	{
		var rect = GetSemanticElementRect(element);
		Assert.IsNotNull(rect, $"The semantic node {GetSemanticElementId(element)} for {element.GetType().Name} is missing.");
		return rect.Value;
	}

	private static void AssertSemanticRectMatchesLayout(FrameworkElement element)
	{
		var id = GetSemanticElementId(element);
		var rect = GetRequiredSemanticElementRect(element);
		var expected = element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

		Assert.AreEqual(expected.X, rect.X, Tolerance, $"{id}: x mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Y, rect.Y, Tolerance, $"{id}: y mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Width, rect.Width, Tolerance, $"{id}: width mismatch (semantic rect {rect}).");
		Assert.AreEqual(expected.Height, rect.Height, Tolerance, $"{id}: height mismatch (semantic rect {rect}).");
	}
#endif
}
