#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Semantic nodes stay over the elements they represent when an element without a semantic node moves.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_WasmSemanticPositioning
{
	[TestCleanup]
	public void Cleanup()
	{
		DisableAccessibility();
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_Pruned_Ancestor_Moves_Then_Semantic_Node_Follows()
	{
		var button = new Button { Content = "Probe" };
		var text = new TextBlock { Text = "Probe text" };
		var panel = new StackPanel { Children = { button, text } };
		await UITestHelper.Load(new Grid { Width = 600, Height = 400, Children = { panel } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(button), message: "The semantic node should be created once accessibility is enabled.");
		await AssertSemanticRectMatchesAsync(button);

		// Only the pruned panel moves: its children keep their offsets relative to it.
		panel.Margin = new Thickness(120, 80, 0, 0);
		await UITestHelper.WaitForIdle();

		await AssertSemanticRectMatchesAsync(button);
		await AssertSemanticRectMatchesAsync(text);
	}

	[TestMethod]
	public async Task When_Nested_Pruned_Ancestors_Move_Then_Semantic_Node_Follows()
	{
		var button = new Button { Content = "Probe" };
		var panel = new StackPanel { Children = { button } };
		var border = new Border { Padding = new Thickness(10), Child = panel };
		await UITestHelper.Load(new Grid { Width = 600, Height = 400, Children = { border } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(button), message: "The semantic node should be created once accessibility is enabled.");
		await AssertSemanticRectMatchesAsync(button);

		// Both pruned ancestors move in the same layout pass.
		border.Margin = new Thickness(50, 40, 0, 0);
		border.Padding = new Thickness(30);
		await UITestHelper.WaitForIdle();

		await AssertSemanticRectMatchesAsync(button);
	}

	[TestMethod]
	public async Task When_Content_Is_Replaced_By_New_Instance_Then_Semantic_Nodes_Are_Positioned()
	{
		// What C# Hot Reload does to an updated page: the instance is replaced by a new one of the same type.
		var host = new ContentControl { Width = 600, Height = 400, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
		host.Content = new ProbePage();
		await UITestHelper.Load(host);

		EnableAccessibilityThroughDom();
		var first = (ProbePage)host.Content;
		await UITestHelper.WaitFor(() => SemanticElementExists(first.Button), message: "The semantic node should be created once accessibility is enabled.");
		await AssertSemanticRectMatchesAsync(first.Button);

		var second = new ProbePage();
		host.Content = second;
		await UITestHelper.WaitFor(() => SemanticElementExists(second.Button), message: "The new instance should be exposed.");
		await UITestHelper.WaitForIdle();

		await AssertSemanticRectMatchesAsync(second.Button);
		await AssertSemanticRectMatchesAsync(second.Text);
	}

	private static async Task AssertSemanticRectMatchesAsync(FrameworkElement element)
	{
		var expected = element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
		Rect? actual = null;
		await UITestHelper.WaitFor(
			() =>
			{
				actual = GetSemanticElementRect(element);
				return actual is { } a
					&& Math.Abs(a.X - expected.X) < 1.5
					&& Math.Abs(a.Y - expected.Y) < 1.5
					&& Math.Abs(a.Width - expected.Width) < 1.5
					&& Math.Abs(a.Height - expected.Height) < 1.5;
			},
			timeoutMS: 3000,
			message: $"The semantic node of {element.GetType().Name} should be at {expected}, was {actual}");
	}

	private sealed partial class ProbePage : UserControl
	{
		public ProbePage()
		{
			Button = new Button { Content = "Probe" };
			Text = new TextBlock { Text = "Probe text" };
			Content = new StackPanel { Margin = new Thickness(200, 150, 0, 0), Spacing = 20, Children = { Button, Text } };
		}

		public Button Button { get; }

		public TextBlock Text { get; }
	}
}
#endif
