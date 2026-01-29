using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
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
		Assert.AreEqual(Windows.UI.Colors.Yellow, lightBrush.Color,
			$"Light region should use Yellow from Light theme dictionary, got {lightBrush.Color}");

		// Dark region should get Blue (from Dark theme dictionary)
		Assert.AreEqual(Windows.UI.Colors.Blue, darkBrush.Color,
			$"Dark region should use Blue from Dark theme dictionary, got {darkBrush.Color}");
	}

	#endregion
}
