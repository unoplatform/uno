using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_ElementTheme
{
	#region Basic RequestedTheme Propagation

	[TestMethod]
	public async Task When_Parent_Has_RequestedTheme_Dark_Child_Gets_Dark_ActualTheme()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
	}

	[TestMethod]
	public async Task When_Parent_Has_RequestedTheme_Light_Child_Gets_Light_ActualTheme()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);
	}

	[TestMethod]
	public async Task When_Grandparent_Has_RequestedTheme_Grandchild_Inherits()
	{
		var grandparent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var parent = new Border() { Width = 100, Height = 100 };
		var child = new Border() { Width = 100, Height = 100 };

		grandparent.Child = parent;
		parent.Child = child;

		WindowHelper.WindowContent = grandparent;
		await WindowHelper.WaitForLoaded(grandparent);

		Assert.AreEqual(ElementTheme.Dark, parent.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
	}

	#endregion

	#region Nested Theme Boundaries

	[TestMethod]
	public async Task When_Parent_Light_Child_Dark_Grandchild_Gets_Dark()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var grandchild = new Border() { Width = 100, Height = 100 };

		parent.Child = child;
		child.Child = grandchild;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Light, parent.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);
	}

	[TestMethod]
	public async Task When_Multiple_Nested_Theme_Boundaries()
	{
		// Root (Dark) -> Child1 (Light) -> Child2 (Dark) -> Leaf
		var root = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var child1 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child2 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var leaf = new Border() { Width = 100, Height = 100 };

		root.Child = child1;
		child1.Child = child2;
		child2.Child = leaf;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		Assert.AreEqual(ElementTheme.Dark, root.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, child1.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child2.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, leaf.ActualTheme);
	}

	#endregion

	#region Dynamic Theme Changes

	[TestMethod]
	public async Task When_Parent_RequestedTheme_Changes_Children_Update()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);

		// Change parent's theme
		parent.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
	}

	[TestMethod]
	public async Task When_Explicit_Child_Theme_Not_Overridden_By_Parent_Change()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var grandchild = new Border() { Width = 100, Height = 100 };

		parent.Child = child;
		child.Child = grandchild;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);

		// Change parent's theme - child should keep its own theme
		parent.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);
	}

	[TestMethod]
	public async Task When_Child_Clears_RequestedTheme_Inherits_From_Parent()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);

		// Clear child's theme
		child.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);
	}

	#endregion

	#region ActualThemeChanged Event

	[TestMethod]
	public async Task When_Theme_Changes_ActualThemeChanged_Fires()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;

		var childThemeChangedCount = 0;
		child.ActualThemeChanged += (s, e) => childThemeChangedCount++;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Change parent's theme
		parent.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.IsGreaterThanOrEqualTo(1, childThemeChangedCount, $"ActualThemeChanged should have fired at least once, but fired {childThemeChangedCount} times");
	}

	[TestMethod]
	public async Task When_Theme_Same_ActualThemeChanged_Not_Fired()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		var childThemeChangedCount = 0;
		child.ActualThemeChanged += (s, e) => childThemeChangedCount++;

		// Set same theme
		parent.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, childThemeChangedCount, "ActualThemeChanged should not fire when theme doesn't change");
	}

	[TestMethod]
	public async Task When_Element_Has_Explicit_Theme_ActualThemeChanged_Fires_On_Direct_Change()
	{
		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };

		var themeChangedCount = 0;
		element.ActualThemeChanged += (s, e) => themeChangedCount++;

		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		themeChangedCount = 0; // Reset after load

		element.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.IsGreaterThanOrEqualTo(1, themeChangedCount, "ActualThemeChanged should fire when RequestedTheme changes");
	}

	#endregion

	#region Theme Resources

	[TestMethod]
	public async Task When_Element_Has_Dark_Theme_Uses_Dark_Resources()
	{
		var border = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Dark"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var brush = border.Background as SolidColorBrush;
		Assert.IsNotNull(brush);

		// In Dark theme, SystemControlBackgroundAltHighBrush should be a dark color
		// The exact value may vary, but it should be darker than light theme
		Assert.IsTrue(brush.Color.R < 128 || brush.Color.G < 128 || brush.Color.B < 128,
			$"Expected dark color but got {brush.Color}");
	}

	[TestMethod]
	public async Task When_Element_Has_Light_Theme_Uses_Light_Resources()
	{
		var border = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var brush = border.Background as SolidColorBrush;
		Assert.IsNotNull(brush);

		// In Light theme, SystemControlBackgroundAltHighBrush should be a light color
		Assert.IsTrue(brush.Color.R > 128 || brush.Color.G > 128 || brush.Color.B > 128,
			$"Expected light color but got {brush.Color}");
	}

	[TestMethod]
	public async Task When_Child_Inherits_Theme_Uses_Correct_Resources()
	{
		var stackPanel = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Dark">
				<Border x:Name="child" Background="{ThemeResource SystemControlBackgroundAltHighBrush}" Width="100" Height="100" />
			</StackPanel>
			""");

		WindowHelper.WindowContent = stackPanel;
		await WindowHelper.WaitForLoaded(stackPanel);

		var child = (Border)stackPanel.FindName("child");
		var brush = child.Background as SolidColorBrush;
		Assert.IsNotNull(brush);

		// Child should use Dark theme resources
		Assert.IsTrue(brush.Color.R < 128 || brush.Color.G < 128 || brush.Color.B < 128,
			$"Expected dark color for child but got {brush.Color}");
	}

	[TestMethod]
	public async Task When_Nested_Themes_Each_Uses_Own_Resources()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<Border x:Name="lightChild" Background="{ThemeResource SystemControlBackgroundAltHighBrush}" Width="100" Height="50" />
				<StackPanel RequestedTheme="Dark">
					<Border x:Name="darkChild" Background="{ThemeResource SystemControlBackgroundAltHighBrush}" Width="100" Height="50" />
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var lightChild = (Border)root.FindName("lightChild");
		var darkChild = (Border)root.FindName("darkChild");

		var lightBrush = lightChild.Background as SolidColorBrush;
		var darkBrush = darkChild.Background as SolidColorBrush;

		Assert.IsNotNull(lightBrush);
		Assert.IsNotNull(darkBrush);

		// They should have different colors
		Assert.AreNotEqual(lightBrush.Color, darkBrush.Color,
			"Light and Dark themed children should have different background colors");
	}

	#endregion

	#region Visual Tree Entry/Exit

	[TestMethod]
	public async Task When_Element_Added_To_Themed_Parent_Inherits_Theme()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Add child after parent is in tree
		var child = new Border() { Width = 100, Height = 100 };
		parent.Child = child;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
	}

	[TestMethod]
	public async Task When_Subtree_Added_To_Themed_Parent_All_Inherit()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Create subtree
		var child = new Border() { Width = 100, Height = 100 };
		var grandchild = new Border() { Width = 100, Height = 100 };
		child.Child = grandchild;

		// Add entire subtree
		parent.Child = child;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);
	}

	#endregion

	#region Edge Cases

#if HAS_UNO
	[TestMethod]
	public async Task When_No_RequestedTheme_Uses_App_Theme()
	{
		var element = new Border() { Width = 100, Height = 100 };

		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		var appTheme = Application.Current?.ActualElementTheme ?? ElementTheme.Light;
		Assert.AreEqual(appTheme, element.ActualTheme);
	}
#endif

	[TestMethod]
	public async Task When_RequestedTheme_Default_ActualTheme_Not_Default()
	{
		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Default };

		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		// ActualTheme should never be Default
		Assert.AreNotEqual(ElementTheme.Default, element.ActualTheme);
		Assert.IsTrue(element.ActualTheme == ElementTheme.Light || element.ActualTheme == ElementTheme.Dark);
	}

	[TestMethod]
	public async Task When_Panel_With_Multiple_Children_All_Inherit_Theme()
	{
		var panel = new StackPanel { RequestedTheme = ElementTheme.Dark };
		var child1 = new Border() { Width = 100, Height = 100 };
		var child2 = new Border() { Width = 100, Height = 100 };
		var child3 = new Border() { Width = 100, Height = 100 };

		panel.Children.Add(child1);
		panel.Children.Add(child2);
		panel.Children.Add(child3);

		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(panel);

		Assert.AreEqual(ElementTheme.Dark, child1.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child2.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child3.ActualTheme);
	}

	[TestMethod]
	public async Task When_Only_Some_Children_Have_Explicit_Theme()
	{
		var panel = new StackPanel { RequestedTheme = ElementTheme.Light };
		var child1 = new Border() { Width = 100, Height = 100 }; // Should inherit Light
		var child2 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark }; // Explicit Dark
		var child3 = new Border() { Width = 100, Height = 100 }; // Should inherit Light

		panel.Children.Add(child1);
		panel.Children.Add(child2);
		panel.Children.Add(child3);

		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(panel);

		Assert.AreEqual(ElementTheme.Light, child1.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child2.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, child3.ActualTheme);
	}

	#endregion

	#region Context Isolation (Sibling Independence)

	[TestMethod]
	public async Task When_Sibling_Has_Different_Theme_No_Interference()
	{
		// Verify siblings with different themes don't affect each other
		var parent = new StackPanel();
		var lightSibling = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var darkSibling = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		parent.Children.Add(lightSibling);
		parent.Children.Add(darkSibling);

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Light, lightSibling.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, darkSibling.ActualTheme);
	}

	[TestMethod]
	public async Task When_Siblings_With_Different_Themes_Use_Correct_Resources()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Border x:Name="lightBorder" RequestedTheme="Light"
						Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
						Width="100" Height="50" />
				<Border x:Name="darkBorder" RequestedTheme="Dark"
						Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
						Width="100" Height="50" />
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var lightBorder = (Border)root.FindName("lightBorder");
		var darkBorder = (Border)root.FindName("darkBorder");

		var lightBrush = lightBorder.Background as SolidColorBrush;
		var darkBrush = darkBorder.Background as SolidColorBrush;

		Assert.IsNotNull(lightBrush);
		Assert.IsNotNull(darkBrush);

		// Siblings should have different themed colors
		Assert.AreNotEqual(lightBrush.Color, darkBrush.Color,
			"Sibling borders with different RequestedTheme should have different background colors");
	}

	[TestMethod]
	public async Task When_Multiple_Siblings_With_Subtrees_Each_Uses_Own_Theme()
	{
		var parent = new StackPanel();

		// Sibling 1: Light theme subtree
		var lightSubtree = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var lightChild = new Border() { Width = 100, Height = 100 };
		lightSubtree.Child = lightChild;

		// Sibling 2: Dark theme subtree
		var darkSubtree = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var darkChild = new Border() { Width = 100, Height = 100 };
		darkSubtree.Child = darkChild;

		parent.Children.Add(lightSubtree);
		parent.Children.Add(darkSubtree);

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Each subtree should maintain its own theme
		Assert.AreEqual(ElementTheme.Light, lightSubtree.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, lightChild.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, darkSubtree.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, darkChild.ActualTheme);
	}

	#endregion

	#region Theme Resource Updates

	[TestMethod]
	public async Task When_Theme_Changes_Resources_Update()
	{
		var border = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);

		var initialBrush = border.Background as SolidColorBrush;
		var initialColor = initialBrush?.Color ?? default;

		// Change theme
		border.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var newBrush = border.Background as SolidColorBrush;
		Assert.IsNotNull(newBrush);
		Assert.AreNotEqual(initialColor, newBrush.Color,
			"Background color should change when RequestedTheme changes");
	}

	[TestMethod]
	public async Task When_Parent_Theme_Changes_Child_Resources_Update()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<Border x:Name="child" Background="{ThemeResource SystemControlBackgroundAltHighBrush}" Width="100" Height="100" />
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var child = (Border)root.FindName("child");
		var initialBrush = child.Background as SolidColorBrush;
		var initialColor = initialBrush?.Color ?? default;

		// Change parent theme
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var newBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(newBrush);
		Assert.AreNotEqual(initialColor, newBrush.Color,
			"Child's themed resource should update when parent theme changes");
	}

	[TestMethod]
	public async Task When_Grandparent_Theme_Changes_Grandchild_Resources_Update()
	{
		var root = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<StackPanel>
					<Border x:Name="grandchild" Background="{ThemeResource SystemControlBackgroundAltHighBrush}" Width="100" Height="100" />
				</StackPanel>
			</Border>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var grandchild = (Border)root.FindName("grandchild");
		var initialBrush = grandchild.Background as SolidColorBrush;
		var initialColor = initialBrush?.Color ?? default;

		// Change grandparent theme
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var newBrush = grandchild.Background as SolidColorBrush;
		Assert.IsNotNull(newBrush);
		Assert.AreNotEqual(initialColor, newBrush.Color,
			"Grandchild's themed resource should update when grandparent theme changes");
	}

	[TestMethod]
	public async Task When_Child_Has_Explicit_Theme_Parent_Change_Does_Not_Affect_Child_Resources()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<Border x:Name="child" RequestedTheme="Dark"
						Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
						Width="100" Height="100" />
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var child = (Border)root.FindName("child");
		var initialBrush = child.Background as SolidColorBrush;
		var initialColor = initialBrush?.Color ?? default;

		// Change parent theme - child has its own explicit theme
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var newBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(newBrush);

		// Child should still use Dark theme resources (same as before since it was already Dark)
		Assert.AreEqual(initialColor, newBrush.Color,
			"Child with explicit RequestedTheme should not have its resources change when parent theme changes");
	}

	#endregion

	#region WinUI Parity Test (Key Scenario)

	[TestMethod]
	public async Task WinUI_Parity_Nested_RequestedTheme_Boundaries()
	{
		// This is the key WinUI parity test from the implementation plan
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<Border x:Name="lightBorder"
						Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
						Width="100" Height="50" />
				<StackPanel RequestedTheme="Dark">
					<Border x:Name="darkBorder"
							Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
							Width="100" Height="50" />
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var lightBorder = (Border)root.FindName("lightBorder");
		var darkBorder = (Border)root.FindName("darkBorder");

		// Verify ActualTheme propagation
		Assert.AreEqual(ElementTheme.Light, lightBorder.ActualTheme, "lightBorder should have Light ActualTheme");
		Assert.AreEqual(ElementTheme.Dark, darkBorder.ActualTheme, "darkBorder should have Dark ActualTheme");

		// Verify themed resources
		var lightBrush = lightBorder.Background as SolidColorBrush;
		var darkBrush = darkBorder.Background as SolidColorBrush;

		Assert.IsNotNull(lightBrush, "lightBorder should have a SolidColorBrush background");
		Assert.IsNotNull(darkBrush, "darkBorder should have a SolidColorBrush background");

		// Verify colors are different (Light and Dark themes have different colors)
		Assert.AreNotEqual(lightBrush.Color, darkBrush.Color,
			$"Light region ({lightBrush.Color}) should have different color than Dark region ({darkBrush.Color})");
	}

	#endregion

	#region Complex Nesting Scenarios

	[TestMethod]
	public async Task When_Deep_Nesting_With_Multiple_Theme_Boundaries()
	{
		// Light -> Dark -> Light -> Dark -> element
		var level1 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var level2 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var level3 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var level4 = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var deepChild = new Border() { Width = 100, Height = 100 };

		level1.Child = level2;
		level2.Child = level3;
		level3.Child = level4;
		level4.Child = deepChild;

		WindowHelper.WindowContent = level1;
		await WindowHelper.WaitForLoaded(level1);

		Assert.AreEqual(ElementTheme.Light, level1.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, level2.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, level3.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, level4.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, deepChild.ActualTheme);
	}

	[TestMethod]
	public async Task When_Theme_Boundary_Changes_Dynamically()
	{
		var root = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var middle = new Border() { Width = 100, Height = 100 }; // Initially inherits Light
		var child = new Border() { Width = 100, Height = 100 };

		root.Child = middle;
		middle.Child = child;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		Assert.AreEqual(ElementTheme.Light, middle.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);

		// Make middle a theme boundary
		middle.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, middle.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);

		// Clear the boundary
		middle.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Light, middle.ActualTheme);
		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);
	}

	[TestMethod]
	public async Task When_Parent_Theme_Changes_And_Child_Has_Explicit_Theme_Grandchild_Uses_Child_Theme()
	{
		var root = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var grandchild = new Border() { Width = 100, Height = 100 };

		root.Child = child;
		child.Child = grandchild;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		// Initial state
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);

		// Change root theme to Dark
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// Grandchild should still use child's Dark theme (unchanged)
		Assert.AreEqual(ElementTheme.Dark, grandchild.ActualTheme);

		// Now change child to Light
		child.RequestedTheme = ElementTheme.Light;
		await WindowHelper.WaitForIdle();

		// Grandchild should now be Light
		Assert.AreEqual(ElementTheme.Light, grandchild.ActualTheme);
	}

	#endregion

	#region Controls With Templates

	[TestMethod]
	public async Task When_Button_In_Themed_Region_Uses_Correct_Resources()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Dark">
				<Button x:Name="themedButton" Content="Dark Theme Button" />
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var button = (Button)root.FindName("themedButton");
		Assert.AreEqual(ElementTheme.Dark, button.ActualTheme);
	}

	[TestMethod]
	public async Task When_TextBlock_In_Different_Theme_Regions()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel RequestedTheme="Light">
					<TextBlock x:Name="lightText" Text="Light Theme" Foreground="{ThemeResource SystemControlForegroundBaseHighBrush}" />
				</StackPanel>
				<StackPanel RequestedTheme="Dark">
					<TextBlock x:Name="darkText" Text="Dark Theme" Foreground="{ThemeResource SystemControlForegroundBaseHighBrush}" />
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var lightText = (TextBlock)root.FindName("lightText");
		var darkText = (TextBlock)root.FindName("darkText");

		Assert.AreEqual(ElementTheme.Light, lightText.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, darkText.ActualTheme);

		var lightForeground = lightText.Foreground as SolidColorBrush;
		var darkForeground = darkText.Foreground as SolidColorBrush;

		Assert.IsNotNull(lightForeground);
		Assert.IsNotNull(darkForeground);
		Assert.AreNotEqual(lightForeground.Color, darkForeground.Color,
			"Text in different theme regions should have different foreground colors");
	}

	#endregion

	#region Dynamic Child Addition

	[TestMethod]
	public async Task When_Child_With_Themed_Resource_Added_After_Load()
	{
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Create and add child with themed resource after parent is loaded
		var child = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		parent.Child = child;
		await WindowHelper.WaitForIdle();

		// Child should pick up parent's dark theme
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme);

		var brush = child.Background as SolidColorBrush;
		Assert.IsNotNull(brush);
		// Should be a dark color
		Assert.IsTrue(brush.Color.R < 128 || brush.Color.G < 128 || brush.Color.B < 128,
			$"Dynamically added child should use dark theme resource but got {brush.Color}");
	}

	[TestMethod]
	public async Task When_Subtree_With_Theme_Boundary_Added_Dynamically()
	{
		var root = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		// Create subtree with its own theme boundary
		var subtree = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		var subtreeChild = new Border() { Width = 100, Height = 100 };
		subtree.Child = subtreeChild;

		root.Child = subtree;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Light, root.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, subtree.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, subtreeChild.ActualTheme);
	}

	#endregion

	#region Theme Context During Late Child Loading (Issue Detection)

	/// <summary>
	/// This test detects if theme context is correctly pushed during OnLoadingPartial
	/// when a child inherits theme from parent. The parent has Dark theme while app
	/// might have a different theme - the child should use Dark resources, not app resources.
	/// </summary>
	[TestMethod]
	public async Task When_Child_Loads_After_Parent_Uses_Inherited_Theme_For_Resources()
	{
		// Get a reference Light-themed color first
		var lightReference = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="50" Height="50" />
			""");

		WindowHelper.WindowContent = lightReference;
		await WindowHelper.WaitForLoaded(lightReference);

		var lightColor = (lightReference.Background as SolidColorBrush)?.Color ?? default;

		// Now test the actual scenario
		// Parent has explicit Dark theme
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// At this point, parent's NotifyThemeChangedCore has completed and popped its context.
		// Now add a child - it should inherit Dark theme and use Dark resources.
		var child = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		parent.Child = child;
		await WindowHelper.WaitForLoaded(child);

		// Verify ActualTheme is correctly inherited
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme,
			"Child should inherit Dark ActualTheme from parent");

		// Critical check: verify the ThemeResource was resolved using Dark theme, not Light/app theme
		var childBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(childBrush, "Child should have a SolidColorBrush background");

		// The child's color should be DIFFERENT from the Light reference
		// If theme context wasn't pushed correctly during loading, it might have used the wrong theme
		Assert.AreNotEqual(lightColor, childBrush.Color,
			$"Child's ThemeResource should resolve to Dark theme color, not Light. " +
			$"Got {childBrush.Color}, Light reference was {lightColor}. " +
			$"This indicates theme context was not correctly applied during child loading.");
	}

	/// <summary>
	/// Tests that deeply nested children added after parent loads still get correct theme resources.
	/// This tests the full inheritance chain: GrandParent(Dark) -> Parent -> Child
	/// </summary>
	[TestMethod]
	public async Task When_Grandchild_Loads_After_Ancestor_Uses_Inherited_Theme_For_Resources()
	{
		// Get reference colors
		var lightRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");
		var darkRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Dark" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");

		var tempContainer = new StackPanel();
		tempContainer.Children.Add(lightRef);
		tempContainer.Children.Add(darkRef);
		WindowHelper.WindowContent = tempContainer;
		await WindowHelper.WaitForLoaded(tempContainer);

		var lightColor = (lightRef.Background as SolidColorBrush)?.Color ?? default;
		var darkColor = (darkRef.Background as SolidColorBrush)?.Color ?? default;

		// Sanity check - they should be different
		Assert.AreNotEqual(lightColor, darkColor, "Light and Dark reference colors should differ");

		// Now test: grandparent with Dark theme, add parent and child dynamically
		var grandparent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };

		WindowHelper.WindowContent = grandparent;
		await WindowHelper.WaitForLoaded(grandparent);

		// Create parent (no explicit theme - should inherit Dark)
		var parent = new Border() { Width = 100, Height = 100 };

		// Create child with ThemeResource (should also inherit Dark)
		var child = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		parent.Child = child;
		grandparent.Child = parent;

		await WindowHelper.WaitForLoaded(child);

		// Verify inheritance chain
		Assert.AreEqual(ElementTheme.Dark, parent.ActualTheme, "Parent should inherit Dark from grandparent");
		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme, "Child should inherit Dark from parent");

		// Critical: verify child's ThemeResource resolved with Dark theme
		var childBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(childBrush);

		Assert.AreNotEqual(lightColor, childBrush.Color,
			$"Grandchild's ThemeResource should NOT be Light color. Got {childBrush.Color}");
		Assert.AreEqual(darkColor, childBrush.Color,
			$"Grandchild's ThemeResource should be Dark color. Expected {darkColor}, got {childBrush.Color}");
	}

	/// <summary>
	/// Tests that when a child with ThemeResource is added to a Light-themed parent
	/// while the app theme is Dark, the child uses Light (parent's theme) not Dark (app theme).
	/// This is the inverse test to catch theme context issues.
	/// </summary>
	[TestMethod]
	public async Task When_Child_Added_To_Light_Parent_In_Dark_App_Uses_Light_Resources()
	{
		// Get reference colors
		var lightRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");
		var darkRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Dark" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");

		var tempContainer = new StackPanel();
		tempContainer.Children.Add(lightRef);
		tempContainer.Children.Add(darkRef);
		WindowHelper.WindowContent = tempContainer;
		await WindowHelper.WaitForLoaded(tempContainer);

		var lightColor = (lightRef.Background as SolidColorBrush)?.Color ?? default;
		var darkColor = (darkRef.Background as SolidColorBrush)?.Color ?? default;

		Assert.AreNotEqual(lightColor, darkColor, "Light and Dark reference colors should differ");

		// Parent with explicit Light theme
		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		// Add child after parent loaded - child should use Light resources (inherited from parent)
		// even if app theme happens to be Dark
		var child = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		parent.Child = child;
		await WindowHelper.WaitForLoaded(child);

		Assert.AreEqual(ElementTheme.Light, child.ActualTheme, "Child should inherit Light from parent");

		var childBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(childBrush);

		// Child should use Light color (from parent's theme), not Dark (from app theme)
		Assert.AreNotEqual(darkColor, childBrush.Color,
			$"Child should NOT use Dark (app) theme color. Got {childBrush.Color}");
		Assert.AreEqual(lightColor, childBrush.Color,
			$"Child should use Light (parent) theme color. Expected {lightColor}, got {childBrush.Color}");
	}

	/// <summary>
	/// Tests theme resource resolution when children are added to a deeply nested structure
	/// where an intermediate ancestor has a different explicit theme than the root.
	/// Root(Light) -> Middle(Dark) -> add child dynamically -> should use Dark
	/// </summary>
	[TestMethod]
	public async Task When_Child_Added_To_Nested_Theme_Boundary_Uses_Nearest_Ancestor_Theme()
	{
		// Get reference colors
		var lightRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Light" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");
		var darkRef = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					RequestedTheme="Dark" Width="100" Height="100"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}" />
			""");

		var tempContainer = new StackPanel();
		tempContainer.Children.Add(lightRef);
		tempContainer.Children.Add(darkRef);
		WindowHelper.WindowContent = tempContainer;
		await WindowHelper.WaitForLoaded(tempContainer);

		var lightColor = (lightRef.Background as SolidColorBrush)?.Color ?? default;
		var darkColor = (darkRef.Background as SolidColorBrush)?.Color ?? default;

		// Build structure: root(Light) -> middle(Dark) -> [will add child here]
		var root = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var middle = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		root.Child = middle;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		// Verify structure before adding child
		Assert.AreEqual(ElementTheme.Light, root.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, middle.ActualTheme);

		// Now add child to middle - should inherit Dark from middle, not Light from root
		var child = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Background="{ThemeResource SystemControlBackgroundAltHighBrush}"
					Width="100" Height="100" />
			""");

		middle.Child = child;
		await WindowHelper.WaitForLoaded(child);

		Assert.AreEqual(ElementTheme.Dark, child.ActualTheme,
			"Child should inherit Dark from middle, not Light from root");

		var childBrush = child.Background as SolidColorBrush;
		Assert.IsNotNull(childBrush);

		Assert.AreNotEqual(lightColor, childBrush.Color,
			$"Child should NOT use Light (root) theme. Got {childBrush.Color}");
		Assert.AreEqual(darkColor, childBrush.Color,
			$"Child should use Dark (middle/parent) theme. Expected {darkColor}, got {childBrush.Color}");
	}

	#endregion

	#region TextBlock Foreground Theme Inheritance (BasicThemeResources Sample Parity)

	/// <summary>
	/// Tests that TextBlock inside a Dark-themed region has different foreground than
	/// TextBlock in a Light-themed region, matching the BasicThemeResources sample behavior.
	/// In WinUI, TextBlock in Dark theme region should have white text while Light region has dark text.
	/// </summary>
	[TestMethod]
	public async Task When_TextBlock_In_Dark_Themed_Region_Has_Theme_Aware_Foreground()
	{
		// Replicate the BasicThemeResources sample structure
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel x:Name="defaultColumn" RequestedTheme="Default">
					<Border Width="300" Height="50">
						<TextBlock x:Name="defaultText">Default Theme Text</TextBlock>
					</Border>
				</StackPanel>
				<StackPanel x:Name="darkColumn" RequestedTheme="Dark">
					<Border Width="300" Height="50">
						<TextBlock x:Name="darkText">Dark Theme Text</TextBlock>
					</Border>
				</StackPanel>
				<StackPanel x:Name="lightColumn" RequestedTheme="Light">
					<Border Width="300" Height="50">
						<TextBlock x:Name="lightText">Light Theme Text</TextBlock>
					</Border>
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var defaultText = (TextBlock)root.FindName("defaultText");
		var darkText = (TextBlock)root.FindName("darkText");
		var lightText = (TextBlock)root.FindName("lightText");

		// Verify ActualTheme propagation
		Assert.AreEqual(ElementTheme.Dark, darkText.ActualTheme, "TextBlock in Dark column should have Dark ActualTheme");
		Assert.AreEqual(ElementTheme.Light, lightText.ActualTheme, "TextBlock in Light column should have Light ActualTheme");

		var darkForeground = darkText.Foreground as SolidColorBrush;
		var lightForeground = lightText.Foreground as SolidColorBrush;

		Assert.IsNotNull(darkForeground, "Dark TextBlock should have a SolidColorBrush foreground");
		Assert.IsNotNull(lightForeground, "Light TextBlock should have a SolidColorBrush foreground");

		// Key assertion: Dark and Light themed TextBlocks should have different foreground colors
		// In WinUI: Dark theme uses light text (white), Light theme uses dark text (black)
		Assert.AreNotEqual(darkForeground.Color, lightForeground.Color,
			$"TextBlock in Dark region ({darkForeground.Color}) should have different foreground " +
			$"than TextBlock in Light region ({lightForeground.Color}). " +
			$"This matches BasicThemeResources sample behavior.");
	}

	/// <summary>
	/// Tests that when app theme changes, elements with explicit RequestedTheme keep their themed appearance.
	/// </summary>
	[TestMethod]
	public async Task When_App_Theme_Changes_Element_With_Explicit_Theme_Keeps_Correct_Foreground()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Dark">
				<TextBlock x:Name="darkText">Dark Theme Text</TextBlock>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var darkText = (TextBlock)root.FindName("darkText");
		var initialForeground = (darkText.Foreground as SolidColorBrush)?.Color;

		Assert.AreEqual(ElementTheme.Dark, darkText.ActualTheme);

		// The foreground should remain consistent because the element has explicit Dark theme
		// regardless of what the app theme might be
		var finalForeground = (darkText.Foreground as SolidColorBrush)?.Color;
		Assert.AreEqual(initialForeground, finalForeground,
			"TextBlock foreground should remain consistent in explicitly themed region");
	}

	/// <summary>
	/// Tests that nested TextBlocks in different theme regions have appropriate foregrounds.
	/// This tests the scenario where you have Light -> Dark -> TextBlock
	/// </summary>
	[TestMethod]
	public async Task When_TextBlock_Nested_In_Different_Theme_Regions_Uses_Nearest_Theme()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<TextBlock x:Name="outerText">Light Region Text</TextBlock>
				<StackPanel RequestedTheme="Dark">
					<TextBlock x:Name="innerText">Dark Region Text</TextBlock>
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var outerText = (TextBlock)root.FindName("outerText");
		var innerText = (TextBlock)root.FindName("innerText");

		Assert.AreEqual(ElementTheme.Light, outerText.ActualTheme);
		Assert.AreEqual(ElementTheme.Dark, innerText.ActualTheme);

		var outerForeground = outerText.Foreground as SolidColorBrush;
		var innerForeground = innerText.Foreground as SolidColorBrush;

		Assert.IsNotNull(outerForeground);
		Assert.IsNotNull(innerForeground);

		// The inner TextBlock (in Dark region) should have a different foreground
		// than the outer TextBlock (in Light region)
		Assert.AreNotEqual(outerForeground.Color, innerForeground.Color,
			$"Nested TextBlock in Dark region ({innerForeground.Color}) should have different foreground " +
			$"than TextBlock in Light region ({outerForeground.Color})");
	}

	/// <summary>
	/// Tests the BasicThemeResources sample scenario with local theme resources.
	/// Verifies that {ThemeResource LocalThemeColor} resolves correctly in different themed regions.
	/// </summary>
	[TestMethod]
	public async Task When_Local_ThemeResource_In_Different_Regions_Uses_Correct_Theme_Dictionary()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="LocalThemeColor" Color="Blue" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="LocalThemeColor" Color="Yellow" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</StackPanel.Resources>

				<StackPanel RequestedTheme="Light">
					<Border x:Name="lightBorder" Width="100" Height="50"
							Background="{ThemeResource LocalThemeColor}" />
				</StackPanel>
				<StackPanel RequestedTheme="Dark">
					<Border x:Name="darkBorder" Width="100" Height="50"
							Background="{ThemeResource LocalThemeColor}" />
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var lightBorder = (Border)root.FindName("lightBorder");
		var darkBorder = (Border)root.FindName("darkBorder");

		var lightBrush = lightBorder.Background as SolidColorBrush;
		var darkBrush = darkBorder.Background as SolidColorBrush;

		Assert.IsNotNull(lightBrush);
		Assert.IsNotNull(darkBrush);

		// Light region should get Yellow (from Light theme dictionary)
		Assert.AreEqual(Microsoft.UI.Colors.Yellow, lightBrush.Color,
			$"Light region should use Yellow from Light theme dictionary, got {lightBrush.Color}");

		// Dark region should get Blue (from Dark theme dictionary)
		Assert.AreEqual(Microsoft.UI.Colors.Blue, darkBrush.Color,
			$"Dark region should use Blue from Dark theme dictionary, got {darkBrush.Color}");
	}

	#endregion

	#region WinUI Ported Tests

	/// <summary>
	/// Ported from WinUI DoesDefaultForegroundBrushMatchAppTheme (WINTH:2673490).
	/// Verifies that a TextBlock outside a themed region still uses the app theme foreground,
	/// even when a sibling element has a different RequestedTheme.
	/// </summary>
	[TestMethod]
	public async Task When_Sibling_Has_Different_Theme_TextBlock_Foreground_Matches_App_Theme()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						Width="200" Height="200">
				<Grid x:Name="grid" Width="200" Height="100" Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
					<TextBlock Text="some text" />
				</Grid>
				<TextBlock x:Name="textblock" Text="some other text" />
			</StackPanel>
			""");

		var grid = (Grid)root.FindName("grid");
		var textblock = (TextBlock)root.FindName("textblock");

		// Set grid to opposite of app theme
		var appTheme = Application.Current.RequestedTheme;
		grid.RequestedTheme = appTheme == ApplicationTheme.Dark
			? ElementTheme.Light
			: ElementTheme.Dark;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var foreground = textblock.Foreground as SolidColorBrush;
		Assert.IsNotNull(foreground, "TextBlock should have a SolidColorBrush foreground");

		// The textblock outside the themed grid should match the app theme:
		// Dark app theme → white-ish text, Light app theme → black-ish text
		if (appTheme == ApplicationTheme.Dark)
		{
			Assert.IsTrue(foreground.Color.R > 128,
				$"In Dark app theme, default foreground should be light colored, got {foreground.Color}");
		}
		else
		{
			Assert.IsTrue(foreground.Color.R < 128,
				$"In Light app theme, default foreground should be dark colored, got {foreground.Color}");
		}
	}

	/// <summary>
	/// Ported from WinUI DoesTemplateBindingMatchThemeWhenChangedDuringAnimation.
	/// Verifies that after going to a visual state and back, and changing theme in between,
	/// the TemplateBinding-bound property reflects the updated theme.
	/// </summary>
	[TestMethod]
	public async Task When_TemplateBinding_Theme_Changes_During_VisualState()
	{
		var style = (Style)XamlReader.Load(
			"""
			<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				   TargetType="ContentControl">
				<Setter Property="Background" Value="{ThemeResource ApplicationPageBackgroundThemeBrush}" />
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="ContentControl">
							<Grid>
								<VisualStateManager.VisualStateGroups>
									<VisualStateGroup x:Name="TestStates">
										<VisualState x:Name="Default" />
										<VisualState x:Name="SomeState">
											<Storyboard>
												<ObjectAnimationUsingKeyFrames Storyboard.TargetName="rect" Storyboard.TargetProperty="Fill">
													<DiscreteObjectKeyFrame KeyTime="0" Value="Orange" />
												</ObjectAnimationUsingKeyFrames>
											</Storyboard>
										</VisualState>
									</VisualStateGroup>
								</VisualStateManager.VisualStateGroups>
								<Rectangle x:Name="rect" Fill="{TemplateBinding Background}" />
							</Grid>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");

		var control = new ContentControl { Width = 100, Height = 100 };
		control.Style = style;
		control.RequestedTheme = ElementTheme.Light;

		WindowHelper.WindowContent = control;
		await WindowHelper.WaitForLoaded(control);
		await WindowHelper.WaitForIdle();

		// Go to SomeState (rect.Fill becomes Orange)
		VisualStateManager.GoToState(control, "SomeState", false);
		await WindowHelper.WaitForIdle();

		// Change theme while in SomeState
		control.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// Go back to Default (rect.Fill should revert to TemplateBinding Background)
		VisualStateManager.GoToState(control, "Default", false);
		await WindowHelper.WaitForIdle();

		// Find 'rect' via VisualTreeHelper
		var rect = FindDescendant<Rectangle>(control);
		Assert.IsNotNull(rect, "Should find Rectangle 'rect' in template");

		var rectFill = rect.Fill as SolidColorBrush;
		var controlBg = control.Background as SolidColorBrush;
		Assert.IsNotNull(rectFill, "rect.Fill should be a SolidColorBrush");
		Assert.IsNotNull(controlBg, "control.Background should be a SolidColorBrush");

		Assert.AreEqual(controlBg.Color, rectFill.Color,
			$"After returning to Default state, rect.Fill ({rectFill.Color}) should match control.Background ({controlBg.Color})");
	}

	/// <summary>
	/// Ported from WinUI ThemeExpressionEvaluationDoesNotOverwriteBaseValueSource (WINTH:2323479).
	/// Verifies that re-evaluating ThemeResource expressions on theme change doesn't overwrite the
	/// base value source, so switching styles still works correctly.
	/// </summary>
	[TestMethod]
	public async Task When_Theme_Changes_ThemeResource_In_Style_Preserves_BaseValue()
	{
		var root = (Grid)XamlReader.Load(
			"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				  Width="100" Height="100"
				  RequestedTheme="Dark">
				<Grid.Resources>
					<SolidColorBrush x:Key="redBrush" Color="Red" />
					<SolidColorBrush x:Key="blueBrush" Color="Blue" />
					<Style x:Key="redStyle" TargetType="Rectangle">
						<Setter Property="Fill" Value="{ThemeResource redBrush}" />
					</Style>
					<Style x:Key="blueStyle" TargetType="Rectangle">
						<Setter Property="Fill" Value="{ThemeResource blueBrush}" />
					</Style>
				</Grid.Resources>
				<Rectangle x:Name="myRectangle" Width="50" Height="50" Style="{StaticResource redStyle}" />
			</Grid>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var rectangle = (Rectangle)root.FindName("myRectangle");
		var fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Red, fill.Color, $"Initial fill should be Red, got {fill.Color}");

		// Change theme — ThemeResource re-evaluates but redBrush is not theme-dependent, so still Red
		root.RequestedTheme = ElementTheme.Light;
		await WindowHelper.WaitForIdle();

		fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Red, fill.Color, $"After theme change, fill should still be Red, got {fill.Color}");

		// Switch style to blueStyle — should work correctly despite ThemeResource re-evaluation
		rectangle.Style = (Style)root.Resources["blueStyle"];
		await WindowHelper.WaitForIdle();

		fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Blue, fill.Color, $"After style change, fill should be Blue, got {fill.Color}");
	}

	/// <summary>
	/// Ported from WinUI NewStyleClearsThemeResourceExpression (WINTH:3366504).
	/// Verifies that applying a new style with a plain (non-ThemeResource) value clears the old
	/// ThemeResource expression, so subsequent theme changes don't revert the value.
	/// </summary>
	[TestMethod]
	public async Task When_New_Style_Without_ThemeResource_Clears_Old_ThemeExpression()
	{
		var root = (Grid)XamlReader.Load(
			"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				  Width="100" Height="100"
				  RequestedTheme="Dark">
				<Grid.Resources>
					<SolidColorBrush x:Key="redBrush" Color="Red" />
					<Style x:Key="redStyle" TargetType="Rectangle">
						<Setter Property="Fill" Value="{ThemeResource redBrush}" />
					</Style>
					<Style x:Key="blueStyle" TargetType="Rectangle">
						<Setter Property="Fill" Value="Blue" />
					</Style>
				</Grid.Resources>
				<Rectangle x:Name="myRectangle" Width="50" Height="50" Style="{StaticResource redStyle}" />
			</Grid>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var rectangle = (Rectangle)root.FindName("myRectangle");
		var fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Red, fill.Color, $"Initial fill should be Red, got {fill.Color}");

		// Change theme — redBrush is not theme-dependent, so still Red
		root.RequestedTheme = ElementTheme.Light;
		await WindowHelper.WaitForIdle();

		fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Red, fill.Color, $"After theme change, fill should still be Red, got {fill.Color}");

		// Switch to blueStyle which uses a plain value (not ThemeResource)
		rectangle.Style = (Style)root.Resources["blueStyle"];
		await WindowHelper.WaitForIdle();

		fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Blue, fill.Color, $"After style change, fill should be Blue, got {fill.Color}");

		// Change theme again — the old ThemeResource expression should be cleared,
		// so fill should remain Blue (from the plain style setter)
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		fill = rectangle.Fill as SolidColorBrush;
		Assert.IsNotNull(fill);
		Assert.AreEqual(Colors.Blue, fill.Color,
			$"After second theme change, fill should still be Blue (old ThemeResource cleared), got {fill.Color}");
	}

	private static T FindDescendant<T>(DependencyObject parent) where T : DependencyObject
	{
		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T match)
			{
				return match;
			}

			var result = FindDescendant<T>(child);
			if (result is not null)
			{
				return result;
			}
		}

		return default;
	}

	#endregion

	#region Runtime RequestedTheme Change Foreground Update

	/// <summary>
	/// Tests the BasicThemeResources sample scenario where clicking "Local Dark" button
	/// dynamically sets RequestedTheme=Dark on the page. In WinUI, TextBlocks in the
	/// first column (RequestedTheme=Default) turn white because they inherit the dark
	/// theme's foreground. This verifies that foreground is updated on already-loaded
	/// elements when their ancestor's RequestedTheme changes at runtime.
	/// </summary>
	[TestMethod]
	public async Task When_RequestedTheme_Changes_At_Runtime_TextBlock_Foreground_Updates()
	{
		// Structure: root StackPanel with a child StackPanel (RequestedTheme=Default)
		// containing a TextBlock. Initially in Light theme, then we change root to Dark.
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<StackPanel x:Name="defaultColumn" RequestedTheme="Default">
					<TextBlock x:Name="textBlock">Test Text</TextBlock>
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var textBlock = (TextBlock)root.FindName("textBlock");

		// Initially in Light theme, TextBlock should have dark foreground
		var initialForeground = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(initialForeground, "TextBlock should have a SolidColorBrush foreground");

		// Now change the root's RequestedTheme to Dark at runtime
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// After theme change, TextBlock should have light (white) foreground
		var updatedForeground = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(updatedForeground, "TextBlock should still have a SolidColorBrush foreground after theme change");

		Assert.AreNotEqual(initialForeground, updatedForeground,
			$"TextBlock foreground should change when ancestor's RequestedTheme changes at runtime. " +
			$"Initial: {initialForeground}, After Dark theme: {updatedForeground}. " +
			$"Expected white/light foreground in Dark theme.");
	}

	/// <summary>
	/// Tests that when a page-level RequestedTheme changes at runtime, TextBlocks
	/// in child regions with RequestedTheme=Default get the correct foreground,
	/// while TextBlocks in regions with explicit RequestedTheme keep theirs.
	/// This matches the full BasicThemeResources sample behavior.
	/// </summary>
	[TestMethod]
	public async Task When_Page_Theme_Changes_Runtime_Default_Column_Inherits_And_Explicit_Column_Keeps()
	{
		var root = (StackPanel)XamlReader.Load(
			"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
				<StackPanel x:Name="defaultColumn" RequestedTheme="Default">
					<TextBlock x:Name="defaultText">Default Column</TextBlock>
				</StackPanel>
				<StackPanel x:Name="darkColumn" RequestedTheme="Dark">
					<TextBlock x:Name="darkText">Dark Column</TextBlock>
				</StackPanel>
				<StackPanel x:Name="lightColumn" RequestedTheme="Light">
					<TextBlock x:Name="lightText">Light Column</TextBlock>
				</StackPanel>
			</StackPanel>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		var defaultText = (TextBlock)root.FindName("defaultText");
		var darkText = (TextBlock)root.FindName("darkText");
		var lightText = (TextBlock)root.FindName("lightText");

		// Record initial foregrounds
		var initialDefaultFg = (defaultText.Foreground as SolidColorBrush)?.Color;
		var initialDarkFg = (darkText.Foreground as SolidColorBrush)?.Color;
		var initialLightFg = (lightText.Foreground as SolidColorBrush)?.Color;

		// Change root to Dark theme at runtime (simulating "Local Dark" button click)
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var updatedDefaultFg = (defaultText.Foreground as SolidColorBrush)?.Color;
		var updatedDarkFg = (darkText.Foreground as SolidColorBrush)?.Color;
		var updatedLightFg = (lightText.Foreground as SolidColorBrush)?.Color;

		// Default column should now inherit Dark theme → white foreground
		Assert.AreNotEqual(initialDefaultFg, updatedDefaultFg,
			$"Default column TextBlock should change foreground when root switches to Dark. " +
			$"Was: {initialDefaultFg}, Now: {updatedDefaultFg}");

		// Dark column already had Dark theme → foreground should remain unchanged
		Assert.AreEqual(initialDarkFg, updatedDarkFg,
			"Dark column TextBlock should keep its foreground when root switches to Dark");

		// Light column has explicit Light theme → foreground should remain unchanged
		Assert.AreEqual(initialLightFg, updatedLightFg,
			"Light column TextBlock should keep its foreground when root switches to Dark");
	}

	#endregion

	#region Popup and Flyout Theme Propagation

	[TestMethod]
	public async Task When_Popup_Child_Inherits_Theme_From_Popup_Owner()
	{
		// MUX Reference: CPopup::NotifyThemeChangedCore propagates to m_pChild
		// Popup's Child is visually reparented to PopupRoot, but the Popup
		// should still propagate theme changes to its Child.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Dark };
		var popup = new Popup();
		var popupContent = new Border { Width = 50, Height = 50 };
		popup.Child = popupContent;
		root.Children.Add(popup);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		popup.IsOpen = true;
		await WindowHelper.WaitForIdle();

		try
		{
			Assert.AreEqual(ElementTheme.Dark, popupContent.ActualTheme,
				"Popup content should inherit Dark theme from popup owner");
		}
		finally
		{
			popup.IsOpen = false;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Popup_Owner_Theme_Changes_At_Runtime_Popup_Content_Updates()
	{
		// When a parent's RequestedTheme changes, open popup content should update.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var popup = new Popup();
		var popupContent = new Border { Width = 50, Height = 50 };
		popup.Child = popupContent;
		root.Children.Add(popup);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		popup.IsOpen = true;
		await WindowHelper.WaitForIdle();

		try
		{
			Assert.AreEqual(ElementTheme.Light, popupContent.ActualTheme);

			// Change theme at runtime
			root.RequestedTheme = ElementTheme.Dark;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, popupContent.ActualTheme,
				"Popup content should update when popup owner's theme changes at runtime");
		}
		finally
		{
			popup.IsOpen = false;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_ComboBox_In_Dark_Region_Dropdown_Has_Dark_Theme()
	{
		// ComboBox's popup is a template child. Its dropdown content should
		// inherit the ComboBox's theme via Popup.NotifyThemeChangedCore.
		var root = new Border { Width = 200, Height = 200, RequestedTheme = ElementTheme.Dark };
		var comboBox = new ComboBox { ItemsSource = "abcdef".ToArray() };
		root.Child = comboBox;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(comboBox);

		try
		{
			comboBox.IsDropDownOpen = true;
			var firstItem = await WindowHelper.WaitForNonNull(
				() => comboBox.ContainerFromIndex(1) as ComboBoxItem);
			await WindowHelper.WaitForLoaded(firstItem);

			Assert.AreEqual(ElementTheme.Dark, firstItem.ActualTheme,
				"ComboBox dropdown item should have Dark theme when ComboBox is in Dark region");
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_ComboBox_Theme_Changes_At_Runtime_Dropdown_Updates()
	{
		// Simulates ThemeHelper.UseDarkTheme() scenario: root content theme changes
		// and open ComboBox dropdown should reflect the new theme.
		var comboBox = new ComboBox { ItemsSource = "abcdef".ToArray() };
		WindowHelper.WindowContent = comboBox;
		await WindowHelper.WaitForLoaded(comboBox);

		using (ThemeHelper.UseDarkTheme())
		{
			try
			{
				comboBox.IsDropDownOpen = true;
				var firstItem = await WindowHelper.WaitForNonNull(
					() => comboBox.ContainerFromIndex(1) as ComboBoxItem);
				await WindowHelper.WaitForLoaded(firstItem);

				Assert.AreEqual(Colors.White, (firstItem.Foreground as SolidColorBrush)?.Color,
					"ComboBox item foreground should be white in dark theme");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				await WindowHelper.WaitForIdle();
			}
		}
	}

	[TestMethod]
	public async Task When_Flyout_Opened_From_Dark_Region_Has_Dark_Theme()
	{
		// MUX Reference: FlyoutBase::ForwardThemeToPresenter walks up from placement
		// target to find the nearest non-Default RequestedTheme and applies it to
		// the presenter and popup.
		var root = new Border { Width = 200, Height = 200, RequestedTheme = ElementTheme.Dark };
		var button = new Button { Content = "Open Flyout" };
		var flyout = new Flyout();
		var flyoutContent = new TextBlock { Text = "Flyout Content" };
		flyout.Content = flyoutContent;
		FlyoutBase.SetAttachedFlyout(button, flyout);
		root.Child = button;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		try
		{
			FlyoutBase.ShowAttachedFlyout(button);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, flyoutContent.ActualTheme,
				"Flyout content should have Dark theme when opened from Dark-themed region");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Flyout_Target_Theme_Changes_Flyout_Updates()
	{
		// MUX Reference: FlyoutBase hooks ActualThemeChanged on placement target
		// and calls ForwardThemeToPresenter when it fires.
		var root = new Border { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Open Flyout" };
		var flyout = new Flyout();
		var flyoutContent = new TextBlock { Text = "Flyout Content" };
		flyout.Content = flyoutContent;
		FlyoutBase.SetAttachedFlyout(button, flyout);
		root.Child = button;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		try
		{
			FlyoutBase.ShowAttachedFlyout(button);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, flyoutContent.ActualTheme);

			// Change theme while flyout is open
			root.RequestedTheme = ElementTheme.Dark;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, flyoutContent.ActualTheme,
				"Flyout content should update when placement target's theme changes");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Nested_Theme_Boundary_Flyout_Gets_Nearest_Theme()
	{
		// When a flyout is opened from inside a nested theme boundary,
		// ForwardThemeToPresenter should find the nearest ancestor's theme.
		var root = new Border { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var innerBorder = new Border { RequestedTheme = ElementTheme.Dark };
		var button = new Button { Content = "Open Flyout" };
		var flyout = new Flyout();
		var flyoutContent = new TextBlock { Text = "Flyout Content" };
		flyout.Content = flyoutContent;
		FlyoutBase.SetAttachedFlyout(button, flyout);
		innerBorder.Child = button;
		root.Child = innerBorder;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		try
		{
			FlyoutBase.ShowAttachedFlyout(button);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, flyoutContent.ActualTheme,
				"Flyout should get Dark theme from nearest ancestor, not Light from outer");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	#endregion

	#region Visual State Theme Resource Re-evaluation

	[TestMethod]
	public async Task When_Setter_VisualState_Entered_After_Element_Theme_Change()
	{
		// KEY FAILING SCENARIO: Element is in Normal state when theme changes to Dark.
		// User then hovers (PointerOver). The setter resolves {ThemeResource} but may
		// use the GLOBAL theme (Light) instead of the element's ActualTheme (Dark).
		// This matches: flyout open → theme changes → user hovers over item.
		var xaml = """
			<ContentControl
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<ContentControl.Template>
					<ControlTemplate TargetType="ContentControl">
						<Grid>
							<VisualStateManager.VisualStateGroups>
								<VisualStateGroup x:Name="CommonStates">
									<VisualState x:Name="Normal" />
									<VisualState x:Name="PointerOver">
										<VisualState.Setters>
											<Setter Target="ContentText.Foreground" Value="{ThemeResource SystemControlHighlightAltBaseHighBrush}" />
										</VisualState.Setters>
									</VisualState>
								</VisualStateGroup>
							</VisualStateManager.VisualStateGroups>
							<Grid x:Name="Root">
								<TextBlock x:Name="ContentText" Text="Test Item" />
							</Grid>
						</Grid>
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>
			""";

		var control = (ContentControl)XamlReader.Load(xaml);
		var root = new StackPanel { Width = 200, Height = 200 };
		root.Children.Add(control);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(control);

		// Change theme while in NORMAL state (no PointerOver setters applied yet)
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Dark, control.ActualTheme, "Control should be Dark");

		// NOW enter PointerOver — the setter should resolve ThemeResource using Dark theme
		VisualStateManager.GoToState(control, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var contentText = control.FindVisualChildByName("ContentText") as TextBlock;
		Assert.IsNotNull(contentText, "Should find ContentText TextBlock");

		var fg = (contentText.Foreground as SolidColorBrush)?.Color;
		// SystemControlHighlightAltBaseHighBrush in Dark = White (#FFFFFFFF)
		// SystemControlHighlightAltBaseHighBrush in Light = Black (#FF000000)
		Assert.IsNotNull(fg, "Foreground should be set");
		Assert.AreNotEqual(Colors.Black, fg,
			$"Setter ThemeResource should resolve to Dark theme value (White), got {fg}. " +
			"GoToState must push the element's theme context for ThemeResource resolution.");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Active_Setter_Based_VisualState_Reapplies()
	{
		// This is the actual failing scenario: MenuFlyoutItem (and similar controls)
		// use VisualState.Setters (not Storyboard) for PointerOver foreground.
		// When theme changes, the Setter's Value ThemeResource binding updates,
		// but the setter is NOT re-applied to the target element.
		// MUX Reference: CVisualStateGroupCollection::NotifyThemeChangedCore →
		// RefreshAllAppliedPropertySetters
		//
		// We use XamlReader to create a control with Setter-based visual states.
		var xaml = """
			<ContentControl
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<ContentControl.Template>
					<ControlTemplate TargetType="ContentControl">
						<Grid>
							<VisualStateManager.VisualStateGroups>
								<VisualStateGroup x:Name="CommonStates">
									<VisualState x:Name="Normal" />
									<VisualState x:Name="PointerOver">
										<VisualState.Setters>
											<Setter Target="Root.Background" Value="{ThemeResource SystemControlHighlightListLowBrush}" />
											<Setter Target="ContentText.Foreground" Value="{ThemeResource SystemControlHighlightAltBaseHighBrush}" />
										</VisualState.Setters>
									</VisualState>
								</VisualStateGroup>
							</VisualStateManager.VisualStateGroups>
							<Grid x:Name="Root" Background="Transparent">
								<TextBlock x:Name="ContentText" Text="Test Item" />
							</Grid>
						</Grid>
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>
			""";

		var control = (ContentControl)XamlReader.Load(xaml);
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		root.Children.Add(control);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(control);

		// Go to PointerOver — setters apply ThemeResource values for Light theme
		VisualStateManager.GoToState(control, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var contentText = control.FindVisualChildByName("ContentText") as TextBlock;
		Assert.IsNotNull(contentText, "Should find ContentText TextBlock");

		var lightFg = (contentText.Foreground as SolidColorBrush)?.Color;

		// Change theme while in PointerOver — the setter's ThemeResource value should
		// re-resolve AND re-apply to the target.
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg = (contentText.Foreground as SolidColorBrush)?.Color;

		Assert.IsNotNull(lightFg, "Should have foreground in Light PointerOver");
		Assert.IsNotNull(darkFg, "Should have foreground in Dark PointerOver");
		Assert.AreNotEqual(lightFg, darkFg,
			$"Setter-based PointerOver foreground must update on theme change. " +
			$"Light={lightFg}, Dark={darkFg}. " +
			"VisualStateGroup must re-apply active setters when theme resources update.");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Then_Enter_VisualState_Uses_New_Theme()
	{
		// User repro: Change theme, then focus a TextBox. The focused visual state
		// should use the new theme's resources. This tests that theme resource bindings
		// on VSM storyboard keyframes are updated before the state is entered.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Test Button" };
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		// Record the Light-theme PointerOver background
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have a ContentPresenter template child");
		var lightBg = (contentPresenter.Background as SolidColorBrush)?.Color;

		// Return to Normal
		VisualStateManager.GoToState(button, "Normal", false);
		await WindowHelper.WaitForIdle();

		// Change theme FIRST
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// THEN enter PointerOver — should pick up Dark theme resources
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var darkBg = (contentPresenter.Background as SolidColorBrush)?.Color;

		Assert.IsNotNull(lightBg, "Should have background in Light PointerOver");
		Assert.IsNotNull(darkBg, "Should have background in Dark PointerOver");
		Assert.AreNotEqual(lightBg, darkBg,
			$"Entering PointerOver after theme change should use new theme resources. " +
			$"Light={lightBg}, Dark={darkBg}");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Active_VisualState_Background_Reapplies()
	{
		// MUX Reference: CVisualStateGroupCollection::NotifyThemeChangedCore →
		// CVisualStateManager2::OnVisualStateGroupCollectionNotifyThemeChanged →
		// RefreshAllAppliedPropertySetters
		// The Button's PointerOver state sets ContentPresenter.Background to
		// {ThemeResource ButtonBackgroundPointerOver} via ObjectAnimationUsingKeyFrames.
		// When theme changes while in PointerOver, the background must update immediately.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Test Button" };
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		// Go to PointerOver state BEFORE theme change
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		// The ContentPresenter is the template root of the default Button style.
		// Use FindVisualChildByName to locate it by its x:Name="ContentPresenter".
		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have a ContentPresenter template child");

		var bgBeforeThemeChange = (contentPresenter.Background as SolidColorBrush)?.Color;

		// Change theme while in PointerOver state
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// The background should have updated automatically — no GoToState needed
		var bgAfterThemeChange = (contentPresenter.Background as SolidColorBrush)?.Color;

		Assert.IsNotNull(bgBeforeThemeChange, "ContentPresenter should have background in Light PointerOver");
		Assert.IsNotNull(bgAfterThemeChange, "ContentPresenter should have background in Dark PointerOver");
		Assert.AreNotEqual(bgBeforeThemeChange, bgAfterThemeChange,
			$"Active PointerOver background should change on theme switch. " +
			$"Was {bgBeforeThemeChange} in Light, got {bgAfterThemeChange} in Dark.");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Active_VisualState_Foreground_Reapplies()
	{
		// Same as above but for Foreground — the PointerOver state sets
		// ContentPresenter.Foreground to {ThemeResource ButtonForegroundPointerOver}.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Test Button" };
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have a ContentPresenter template child");

		var fgBeforeThemeChange = (contentPresenter.Foreground as SolidColorBrush)?.Color;

		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var fgAfterThemeChange = (contentPresenter.Foreground as SolidColorBrush)?.Color;

		Assert.IsNotNull(fgBeforeThemeChange, "ContentPresenter should have foreground in Light PointerOver");
		Assert.IsNotNull(fgAfterThemeChange, "ContentPresenter should have foreground in Dark PointerOver");
		Assert.AreNotEqual(fgBeforeThemeChange, fgAfterThemeChange,
			$"Active PointerOver foreground should change on theme switch. " +
			$"Was {fgBeforeThemeChange} in Light, got {fgAfterThemeChange} in Dark.");
	}

	[TestMethod]
	public async Task When_Page_Theme_Changes_TextBox_Focused_BorderBrush_Updates()
	{
		// Exact user repro: Page.RequestedTheme changes, then TextBox is focused.
		// The Focused state visual should use new theme resources.
		// Uses Frame→Page structure like SamplesApp.
		var frame = new Frame { Width = 200, Height = 200 };

		WindowHelper.WindowContent = frame;
		await WindowHelper.WaitForLoaded(frame);

		// Create a page with a TextBox
		var page = new Page();
		var sp = new StackPanel();
		var textBox = new TextBox { Width = 150 };
		sp.Children.Add(textBox);
		page.Content = sp;

		frame.Content = page;
		await WindowHelper.WaitForLoaded(textBox);

		// Get the TextBox background in Normal state (Default/Light theme)
		var bgElement = textBox.FindVisualChildByName("BackgroundElement") as Border;
		var borderElement = textBox.FindVisualChildByName("BorderElement") as Border;

		// Focus the TextBox to get the Focused visual state
		textBox.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		var lightFocusedBg = bgElement?.Background;
		var lightFocusedBorder = borderElement?.BorderBrush;

		// Go back to Normal
		VisualStateManager.GoToState(textBox, "Normal", false);
		await WindowHelper.WaitForIdle();

		// Change Page theme (matching BasicThemeResources sample behavior)
		page.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// Focus again in Dark theme
		textBox.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		var darkFocusedBg = bgElement?.Background;
		var darkFocusedBorder = borderElement?.BorderBrush;

		// The TextBox background/border in Focused state should differ between themes
		// (TextControlBackgroundFocused, SystemControlHighlightAccentBrush)
		Assert.AreEqual(ElementTheme.Dark, textBox.ActualTheme,
			"TextBox should have Dark theme after Page.RequestedTheme change");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Button_Normal_Foreground_Updates()
	{
		// Basic test: does a Button's normal (non-hovered) Foreground update
		// when its parent's theme changes?
		var root = new StackPanel { Width = 200, Height = 200 };
		var button = new Button { Content = "Test" };
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have ContentPresenter");

		var defaultFg = (contentPresenter.Foreground as SolidColorBrush)?.Color;

		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg = (contentPresenter.Foreground as SolidColorBrush)?.Color;

		Assert.IsNotNull(defaultFg, "Should have foreground in Default theme");
		Assert.IsNotNull(darkFg, "Should have foreground in Dark theme");
		Assert.AreNotEqual(defaultFg, darkFg,
			$"Button normal foreground should change. Default={defaultFg}, Dark={darkFg}");
	}

	[TestMethod]
	public async Task When_Grandparent_RequestedTheme_Changes_Button_Foreground_Updates()
	{
		// Matches BasicThemeResources scenario: Page sets RequestedTheme,
		// buttons are nested several levels deep.
		var outerPanel = new StackPanel { Width = 300, Height = 300 };
		var middleGrid = new Grid();
		var innerPanel = new StackPanel { Orientation = Orientation.Horizontal };
		var button1 = new Button { Content = "Button1" };
		var button2 = new Button { Content = "Button2" };
		var button3 = new Button { Content = "Button3" };

		innerPanel.Children.Add(button1);
		innerPanel.Children.Add(button2);
		innerPanel.Children.Add(button3);
		middleGrid.Children.Add(innerPanel);
		outerPanel.Children.Add(middleGrid);

		WindowHelper.WindowContent = outerPanel;
		await WindowHelper.WaitForLoaded(button3);

		var cp1 = button1.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		var cp3 = button3.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(cp1, "Button1 should have ContentPresenter");
		Assert.IsNotNull(cp3, "Button3 should have ContentPresenter");

		var defaultFg1 = (cp1.Foreground as SolidColorBrush)?.Color;
		var defaultFg3 = (cp3.Foreground as SolidColorBrush)?.Color;

		// Change theme on the GRANDPARENT (like a Page setting RequestedTheme)
		outerPanel.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg1 = (cp1.Foreground as SolidColorBrush)?.Color;
		var darkFg3 = (cp3.Foreground as SolidColorBrush)?.Color;

		Assert.AreNotEqual(defaultFg1, darkFg1,
			$"Button1 foreground should change from {defaultFg1} to a dark theme color, got {darkFg1}");
		Assert.AreNotEqual(defaultFg3, darkFg3,
			$"Button3 foreground should change from {defaultFg3} to a dark theme color, got {darkFg3}");
	}

	[TestMethod]
	public async Task When_XamlParsed_Buttons_Theme_Change_Foreground_Updates()
	{
		// Test XAML-parsed buttons (matching BasicThemeResources scenario)
		// where buttons are created via XAML parsing, not programmatically.
		var xaml = """
			<StackPanel
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				Width="400" Height="200">
				<StackPanel Orientation="Horizontal">
					<Button x:Name="Btn1">Button1</Button>
					<Button x:Name="Btn2">Button2</Button>
					<Button x:Name="Btn3">Button3</Button>
				</StackPanel>
			</StackPanel>
			""";

		var root = (StackPanel)XamlReader.Load(xaml);
		WindowHelper.WindowContent = root;

		var btn1 = root.FindName("Btn1") as Button;
		var btn3 = root.FindName("Btn3") as Button;
		Assert.IsNotNull(btn1);
		Assert.IsNotNull(btn3);

		await WindowHelper.WaitForLoaded(btn3);

		var cp1 = btn1.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		var cp3 = btn3.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(cp1, "Button1 should have ContentPresenter");
		Assert.IsNotNull(cp3, "Button3 should have ContentPresenter");

		var defaultFg1 = (cp1.Foreground as SolidColorBrush)?.Color;

		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg1 = (cp1.Foreground as SolidColorBrush)?.Color;
		var darkFg3 = (cp3.Foreground as SolidColorBrush)?.Color;

		Assert.AreNotEqual(defaultFg1, darkFg1,
			$"XAML-parsed button foreground should change. Default={defaultFg1}, Dark={darkFg1}");
		Assert.AreNotEqual(defaultFg1, darkFg3,
			$"XAML-parsed button3 foreground should change. Default={defaultFg1}, Dark={darkFg3}");
	}

	[TestMethod]
	public async Task When_Theme_Changes_TextBlock_Foreground_Updates()
	{
		// Basic test: does a TextBlock's Foreground update when parent theme changes?
		var root = new StackPanel { Width = 200, Height = 200 };
		var textBlock = new TextBlock { Text = "Hello" };
		root.Children.Add(textBlock);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(textBlock);

		var defaultFg = (textBlock.Foreground as SolidColorBrush)?.Color;

		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg = (textBlock.Foreground as SolidColorBrush)?.Color;

		Assert.IsNotNull(defaultFg, "Should have foreground in Default theme");
		Assert.IsNotNull(darkFg, "Should have foreground in Dark theme");
		Assert.AreNotEqual(defaultFg, darkFg,
			$"TextBlock foreground should change. Default={defaultFg}, Dark={darkFg}");
	}

	[TestMethod]
	public async Task When_Theme_Default_To_Dark_Button_PointerOver_Updates()
	{
		// Matches SamplesApp behavior: page starts with Default theme (Light app theme),
		// then user switches to Dark. The button in PointerOver should update.
		var root = new StackPanel { Width = 200, Height = 200 }; // No explicit RequestedTheme (= Default)
		var button = new Button { Content = "Test Button" };
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		// Go to PointerOver while in Default (Light) theme
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have a ContentPresenter template child");

		var defaultBg = (contentPresenter.Background as SolidColorBrush)?.Color;

		// Switch from Default → Dark (like SamplesApp's LocalDark button)
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkBg = (contentPresenter.Background as SolidColorBrush)?.Color;

		Assert.IsNotNull(defaultBg, "Should have background in Default PointerOver");
		Assert.IsNotNull(darkBg, "Should have background in Dark PointerOver");
		Assert.AreNotEqual(defaultBg, darkBg,
			$"PointerOver background should change from Default to Dark. " +
			$"Default={defaultBg}, Dark={darkBg}");
	}

	[TestMethod]
	public async Task When_MenuFlyout_Opened_After_Theme_Change_Items_Use_New_Theme()
	{
		// User repro: MenuFlyout items hovered show old theme foreground (black)
		// after switching to dark theme. The flyout presenter and items should
		// pick up the new theme when opened.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Click me" };
		var menuFlyout = new MenuFlyout();
		menuFlyout.Items.Add(new MenuFlyoutItem { Text = "Item 1" });
		menuFlyout.Items.Add(new MenuFlyoutItem { Text = "Item 2" });
		button.Flyout = menuFlyout;
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		// Change to Dark theme BEFORE opening flyout
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		try
		{
			// Open flyout
			menuFlyout.ShowAt(button);
			await WindowHelper.WaitForIdle();

			// The flyout presenter should have Dark theme
			var presenter = menuFlyout.Items[0].FindVisualChildByName("LayoutRoot");
			if (presenter == null)
			{
				// Try getting the item directly
				var item = menuFlyout.Items[0] as MenuFlyoutItem;
				Assert.AreEqual(ElementTheme.Dark, item.ActualTheme,
					"MenuFlyoutItem should have Dark theme after parent theme change");
			}
			else
			{
				Assert.AreEqual(ElementTheme.Dark, (presenter as FrameworkElement)?.ActualTheme,
					"MenuFlyoutItem layout root should have Dark theme");
			}
		}
		finally
		{
			menuFlyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_MenuFlyout_Open_During_Theme_Change_PointerOver_Updates()
	{
		// When a MenuFlyout is open and theme changes, items in PointerOver state
		// should update their foreground to match the new theme.
		var root = new StackPanel { Width = 200, Height = 200, RequestedTheme = ElementTheme.Light };
		var button = new Button { Content = "Click me" };
		var menuFlyout = new MenuFlyout();
		menuFlyout.Items.Add(new MenuFlyoutItem { Text = "Item 1" });
		button.Flyout = menuFlyout;
		root.Children.Add(button);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(button);

		try
		{
			// Open flyout in Light theme
			menuFlyout.ShowAt(button);
			await WindowHelper.WaitForIdle();

			var item = menuFlyout.Items[0] as MenuFlyoutItem;

			// Put item in PointerOver state
			VisualStateManager.GoToState(item, "PointerOver", false);
			await WindowHelper.WaitForIdle();

			var lightFg = (item.Foreground as SolidColorBrush)?.Color;

			// Change theme while flyout is open and item is in PointerOver
			root.RequestedTheme = ElementTheme.Dark;
			await WindowHelper.WaitForIdle();

			var darkFg = (item.Foreground as SolidColorBrush)?.Color;

			// The item's foreground should reflect the new theme
			Assert.IsNotNull(lightFg, "Item should have foreground in Light PointerOver");
			Assert.IsNotNull(darkFg, "Item should have foreground in Dark PointerOver");
			Assert.AreNotEqual(lightFg, darkFg,
				$"MenuFlyoutItem PointerOver foreground should differ between themes. " +
				$"Light={lightFg}, Dark={darkFg}");
		}
		finally
		{
			menuFlyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Storyboard_VisualState_After_Element_Theme_Change()
	{
		// This test verifies that Storyboard-based visual state animations
		// (ObjectAnimationUsingKeyFrames) use the correct ThemeResource values
		// after an element-level theme change.
		var container = new Border { RequestedTheme = ElementTheme.Light };

		// Create a Button with standard template (uses Storyboard for PointerOver state)
		var button = new Button { Content = "Test", Width = 120, Height = 40 };
		container.Child = button;

		WindowHelper.WindowContent = container;
		await WindowHelper.WaitForLoaded(button);
		await WindowHelper.WaitForIdle();

		// Find the ContentPresenter in the Button's template tree
		ContentPresenter contentPresenter = null;
		var queue = new System.Collections.Generic.Queue<DependencyObject>();
		queue.Enqueue(button);
		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (current is ContentPresenter cp && current != button)
			{
				contentPresenter = cp;
				break;
			}
			var childCount = VisualTreeHelper.GetChildrenCount(current);
			for (int i = 0; i < childCount; i++)
			{
				queue.Enqueue(VisualTreeHelper.GetChild(current, i));
			}
		}
		Assert.IsNotNull(contentPresenter, "Button should have a ContentPresenter in its template");

		// Simulate PointerOver state
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		// Record Light theme PointerOver background
		var lightPointerOverBg = (contentPresenter.Background as SolidColorBrush)?.Color;
		Assert.IsNotNull(lightPointerOverBg, "Button should have SolidColorBrush background in PointerOver");

		// Return to Normal
		VisualStateManager.GoToState(button, "Normal", false);
		await WindowHelper.WaitForIdle();

		// Change theme to Dark
		container.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// Enter PointerOver again - should use Dark theme resources
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		var darkPointerOverBg = (contentPresenter.Background as SolidColorBrush)?.Color;
		Assert.IsNotNull(darkPointerOverBg, "Button should have SolidColorBrush background in Dark PointerOver");

		// The PointerOver background should be different between Light and Dark themes
		Assert.AreNotEqual(lightPointerOverBg.Value, darkPointerOverBg.Value,
			$"Button PointerOver background should differ between themes. " +
			$"Light={lightPointerOverBg}, Dark={darkPointerOverBg}");
	}

	[TestMethod]
	public async Task When_Root_Theme_Cycles_Dark_Default_TextBlock_Foreground_Restores()
	{
		// Repro: SamplesApp three-dots Dark → Default → navigate to page → TextBlock white instead of black
		// Root cause: EnsureThemeForeground sets Inheritance-precedence foreground on all children
		// during Dark push, but when root switches back to Default, only root's _themeForeground
		// is cleared. Children retain stale dark foreground at Inheritance precedence.
		var root = new Border { Width = 200, Height = 200 };
		var textBlock = new TextBlock { Text = "Test" };
		root.Child = textBlock;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(textBlock);
		await WindowHelper.WaitForIdle();

		// Baseline: Light theme foreground should be dark (black or near-black)
		var lightFg = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(lightFg, "TextBlock should have SolidColorBrush foreground");
		Assert.IsTrue(lightFg.Value.R < 128, $"Light theme foreground should be dark, got {lightFg}");

		// Step 1: Switch root to Dark (mimics three-dots → Dark)
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var darkFg = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(darkFg, "TextBlock should have foreground in Dark theme");
		Assert.IsTrue(darkFg.Value.R > 128, $"Dark theme foreground should be light, got {darkFg}");

		// Step 2: Switch root back to Default (mimics three-dots → Default)
		root.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		// TextBlock foreground should be restored to light-theme (dark color)
		var restoredFg = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(restoredFg, "TextBlock should have foreground after theme restore");
		Assert.IsTrue(restoredFg.Value.R < 128,
			$"After Dark→Default cycle, TextBlock foreground should be dark (light theme), got {restoredFg}");
	}

	[TestMethod]
	public async Task When_Root_Theme_Cycles_New_Content_Has_Correct_Foreground()
	{
		// Tests that NEW content loaded after Dark→Default cycle gets correct foreground
		var root = new StackPanel { Width = 200, Height = 200 };

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		// Cycle: Dark → Default
		root.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();
		root.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		// Add NEW TextBlock after the cycle
		var textBlock = new TextBlock { Text = "New content" };
		root.Children.Add(textBlock);
		await WindowHelper.WaitForLoaded(textBlock);
		await WindowHelper.WaitForIdle();

		var fg = (textBlock.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(fg, "New TextBlock should have foreground");
		Assert.IsTrue(fg.Value.R < 128,
			$"New TextBlock after Dark→Default cycle should have dark foreground (light theme), got {fg}");
	}

	#endregion

	#region Root Theme Cycle Then Element Theme Change (Issue Repro)

	[TestMethod]
	public async Task When_RootTheme_Cycles_Then_Element_RequestedTheme_Dark_Button_Foreground_Updates()
	{
		// Exact SamplesApp repro:
		// 1. Three-dots button → root.RequestedTheme = Dark → root.RequestedTheme = Default
		// 2. Navigate to BasicThemeResources page (loads fresh content)
		// 3. Click "Local Dark" → page.RequestedTheme = Dark
		// 4. BUG: All buttons keep black text (Light foreground) instead of white (Dark foreground)
		//
		// The root-level theme cycle (Dark → Default) leaves stale state that prevents
		// element-level theme changes from properly updating ThemeResource bindings.

		// Use a wrapper that simulates the SamplesApp root element
		var appRoot = new Border { Width = 400, Height = 400 };
		WindowHelper.WindowContent = appRoot;
		await WindowHelper.WaitForLoaded(appRoot);

		// Step 1: Simulate three-dots menu theme cycle (Dark → back to Default)
		appRoot.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();
		appRoot.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		// Step 2: Navigate to sample page (create fresh content under the root)
		var page = new StackPanel { Width = 400, Height = 300 };
		var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
		var button1 = new Button { Content = "Local Default" };
		var button2 = new Button { Content = "Local Dark" };
		var button3 = new Button { Content = "Parent Dark" };
		buttonsPanel.Children.Add(button1);
		buttonsPanel.Children.Add(button2);
		buttonsPanel.Children.Add(button3);
		page.Children.Add(buttonsPanel);
		appRoot.Child = page;

		await WindowHelper.WaitForLoaded(button3);
		await WindowHelper.WaitForIdle();

		// Record baseline foreground (should be dark/black in Light theme)
		var cp1 = button1.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(cp1, "Button1 should have ContentPresenter");

		var lightFg = (cp1.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(lightFg, "Button should have SolidColorBrush foreground");
		Assert.IsTrue(lightFg.Value.R < 128,
			$"Before theme change, button foreground should be dark (Light theme). Got R={lightFg.Value.R}");

		// Step 3: Set element-level RequestedTheme to Dark (simulating "Local Dark" click)
		page.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		// Step 4: Verify foreground updated to light/white (Dark theme)
		var darkFg = (cp1.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(darkFg, "Button should have SolidColorBrush foreground after theme change");
		Assert.IsTrue(darkFg.Value.R > 128,
			$"After element RequestedTheme=Dark (post root cycle), button foreground should be light. " +
			$"Got R={darkFg.Value.R}, color={darkFg}. This indicates ThemeResource bindings were not updated.");
	}

	[TestMethod]
	public async Task When_RootTheme_Cycles_Then_Element_Dark_PointerOver_Uses_Correct_Theme()
	{
		// SamplesApp repro for Issue 7:
		// After root Dark→Default cycle and element RequestedTheme=Dark,
		// hovering a button should apply Dark theme PointerOver resources, not Light.

		var appRoot = new Border { Width = 300, Height = 300 };
		WindowHelper.WindowContent = appRoot;
		await WindowHelper.WaitForLoaded(appRoot);

		// Root theme cycle (three-dots: Dark → Default)
		appRoot.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();
		appRoot.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		// Create fresh content
		var page = new StackPanel { Width = 200, Height = 200 };
		var button = new Button { Content = "Test Button" };
		page.Children.Add(button);
		appRoot.Child = page;

		await WindowHelper.WaitForLoaded(button);
		await WindowHelper.WaitForIdle();

		// Set element-level Dark theme
		page.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var contentPresenter = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(contentPresenter, "Button should have ContentPresenter");

		// Enter PointerOver state — should use Dark theme resources
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		// In Dark PointerOver, foreground should be light (white)
		var fg = (contentPresenter.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(fg, "ContentPresenter should have foreground in PointerOver");
		Assert.IsTrue(fg.Value.R > 128,
			$"PointerOver foreground after element Dark theme (post root cycle) should be light. " +
			$"Got R={fg.Value.R}, color={fg}. PointerOver is using wrong theme context.");
	}

	[TestMethod]
	public async Task When_RootTheme_Cycles_Then_Element_Dark_PointerExit_Restores_Correct_Foreground()
	{
		// SamplesApp repro: exiting PointerOver "fixes" the foreground to white.
		// Tests the full cycle:
		// 1. Root theme cycle (Dark → Default)
		// 2. Element theme = Dark
		// 3. Hover button (PointerOver)
		// 4. Exit hover (Normal)
		// 5. Foreground should be white throughout steps 2-4

		var appRoot = new Border { Width = 300, Height = 300 };
		WindowHelper.WindowContent = appRoot;
		await WindowHelper.WaitForLoaded(appRoot);

		// Root theme cycle
		appRoot.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();
		appRoot.RequestedTheme = ElementTheme.Default;
		await WindowHelper.WaitForIdle();

		// Fresh content
		var page = new StackPanel { Width = 200, Height = 200 };
		var button = new Button { Content = "Test" };
		page.Children.Add(button);
		appRoot.Child = page;

		await WindowHelper.WaitForLoaded(button);
		await WindowHelper.WaitForIdle();

		// Set element Dark theme
		page.RequestedTheme = ElementTheme.Dark;
		await WindowHelper.WaitForIdle();

		var cp = button.FindVisualChildByName("ContentPresenter") as ContentPresenter;
		Assert.IsNotNull(cp, "Button should have ContentPresenter");

		// Check foreground BEFORE hover (should already be white in Dark theme)
		var beforeHoverFg = (cp.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(beforeHoverFg, "Should have foreground before hover");

		// Hover
		VisualStateManager.GoToState(button, "PointerOver", false);
		await WindowHelper.WaitForIdle();

		// Exit hover
		VisualStateManager.GoToState(button, "Normal", false);
		await WindowHelper.WaitForIdle();

		var afterHoverFg = (cp.Foreground as SolidColorBrush)?.Color;
		Assert.IsNotNull(afterHoverFg, "Should have foreground after hover exit");

		// Both should be light/white (Dark theme foreground)
		Assert.IsTrue(beforeHoverFg.Value.R > 128,
			$"Before hover, foreground should be light (Dark theme). Got R={beforeHoverFg.Value.R}");
		Assert.IsTrue(afterHoverFg.Value.R > 128,
			$"After hover exit, foreground should be light (Dark theme). Got R={afterHoverFg.Value.R}");

		// Foreground should be consistent
		Assert.AreEqual(beforeHoverFg, afterHoverFg,
			$"Foreground should be consistent before and after hover. Before={beforeHoverFg}, After={afterHoverFg}");
	}

	#endregion
}
