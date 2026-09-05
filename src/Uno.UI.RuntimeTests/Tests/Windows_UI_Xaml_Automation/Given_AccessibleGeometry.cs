using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Geometry contract for accessibility/native-overlay placement: the rectangle published for an
	/// element must be its bounds expressed with the complete owner-to-root chain — every ancestor
	/// arrange offset and render transform composed exactly once — and it must keep tracking that
	/// chain after the initial layout.
	/// </summary>
	[TestClass]
	public class Given_AccessibleGeometry
	{
		/// <summary>
		/// Cross-platform baseline for the matrix every native geometry path is built on
		/// (<c>UnoExploreByTouchHelper</c> on Android, the WASM semantic nodes, the native element
		/// hosts). Nested render transforms and the layout offsets of the elements in between must
		/// compose into a single owner-to-root transform, applied once.
		/// </summary>
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
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Same contract under a scale: a scaled ancestor moves *and* resizes its descendants, so the
		/// published rectangle has to be the transformed bounds, not the untransformed size.
		/// </summary>
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
				TestServices.WindowHelper.WindowContent = null;
			}
		}

#if __SKIA__
		/// <summary>
		/// Regression for #24280 (WASM half): a layout container such as a Border is pruned from the
		/// accessibility tree, so its offset only ever exists inside the position its semantic
		/// descendants hold relative to the semantic ancestor they are DOM-nested under. When that
		/// pruned container moves after the initial layout, nothing used to re-emit those descendants
		/// — every semantic node kept the geometry it was built with, which is how vertically separated
		/// inputs all ended up reporting the same <c>top</c>.
		/// </summary>
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

				var before = GetSemanticElementRect(button);

				pruned.Margin = new Thickness(0, 120, 0, 0);
				await UITestHelper.WaitForIdle();
				await UITestHelper.WaitFor(() => Math.Abs(GetSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the pruned ancestor.");

				var after = GetSemanticElementRect(button);

				Assert.AreEqual(120, after.Y - before.Y, 2, "The semantic rectangle must follow the offset of a pruned ancestor.");
				Assert.AreEqual(0, after.X - before.X, 2, "A vertical move must not shift the semantic rectangle horizontally.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Regression for #24280: nested render transforms applied to pruned ancestors after layout
		/// must compose into the semantic rectangle. A RenderTransform never changes the offset or the
		/// size of the visual it is set on, so the accessibility geometry only tracks it when transform
		/// changes are routed as well.
		/// </summary>
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

				var before = GetSemanticElementRect(button);

				outer.RenderTransform = new TranslateTransform { X = 40, Y = 60 };
				inner.RenderTransform = new TranslateTransform { X = 15, Y = 25 };
				await UITestHelper.WaitForIdle();
				await UITestHelper.WaitFor(() => Math.Abs(GetSemanticElementRect(button).Y - before.Y) > 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor render transforms.");

				var after = GetSemanticElementRect(button);

				Assert.AreEqual(55, after.X - before.X, 2, "Both ancestor translations must be composed on X.");
				Assert.AreEqual(85, after.Y - before.Y, 2, "Both ancestor translations must be composed on Y.");
				Assert.AreEqual(before.Width, after.Width, 2, "A translation must not resize the semantic rectangle.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// A scaled ancestor must scale the semantic rectangle too — the published size is the
		/// transformed bounds, not the element's own size.
		/// </summary>
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

				var before = GetSemanticElementRect(button);
				Assert.IsTrue(before.Width > 0, "The semantic rectangle must have a non-zero width before scaling.");

				scaled.RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };
				await UITestHelper.WaitForIdle();
				await UITestHelper.WaitFor(() => GetSemanticElementRect(button).Width > before.Width + 1, timeoutMS: 5000, message: "The semantic rectangle never followed the ancestor scale.");

				var after = GetSemanticElementRect(button);

				Assert.AreEqual(before.Width * 2, after.Width, 2, "Width must be scaled by the ancestor ScaleX.");
				Assert.AreEqual(before.Height * 2, after.Height, 2, "Height must be scaled by the ancestor ScaleY.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Guards the structural ownership of the semantic tree for templated text controls: the
		/// TextBox inside an AutoSuggestBox keeps its own semantic node, and neither it nor a plain
		/// TextBox, a PasswordBox or a ComboBox is re-parented onto another control's node.
		/// </summary>
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
				await UITestHelper.WaitFor(() => SemanticElementExists(textBox), timeoutMS: 5000, message: "Timed out waiting for the semantic elements to be created.");
				await UITestHelper.WaitForIdle();

				Assert.IsTrue(SemanticElementExists(autoSuggestBox), "The AutoSuggestBox must own a semantic node.");
				Assert.IsTrue(SemanticElementExists(textBox), "A standalone TextBox must own a semantic node.");
				Assert.IsTrue(SemanticElementExists(passwordBox), "A PasswordBox must own a semantic node.");
				Assert.IsTrue(SemanticElementExists(comboBox), "A ComboBox must own a semantic node.");

				var innerTextBox = FindFirstTextBox(autoSuggestBox);
				Assert.IsNotNull(innerTextBox, "The AutoSuggestBox template is expected to contain a TextBox.");
				Assert.IsTrue(SemanticElementExists(innerTextBox), "The templated TextBox must keep its own semantic node rather than borrowing the AutoSuggestBox one.");
				Assert.AreNotEqual(GetSemanticElementId(autoSuggestBox), GetSemanticElementId(innerTextBox), "The templated TextBox must not be collapsed onto the AutoSuggestBox node.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static TextBox FindFirstTextBox(DependencyObject root)
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
#endif
	}
}
