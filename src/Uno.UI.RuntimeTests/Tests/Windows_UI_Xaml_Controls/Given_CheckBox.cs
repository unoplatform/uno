using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CheckBox
{
	[TestMethod]
	public async Task When_IsChecked_Set_Via_Binding_In_ItemsRepeater()
	{
		// Repro: CheckBox inside ItemsRepeater shows wrong visual state (unchecked)
		// when IsChecked is set via binding for items in the initial viewport.
		// The visual state only updates on hover/interaction.

		var source = new List<TestItem>
		{
			new("Item1", true),
			new("Item2", false),
			new("Item3", true),
		};

		var repeater = new ItemsRepeater
		{
			ItemsSource = source,
			ItemTemplate = new DataTemplate(() =>
			{
				var cb = new CheckBox();
				cb.SetBinding(
					Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty,
					new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath("IsEnabled") });
				cb.SetBinding(
					FrameworkElement.TagProperty,
					new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath("Name") });
				return cb;
			}),
		};

		WindowHelper.WindowContent = repeater;
		await WindowHelper.WaitForLoaded(repeater);
		await WindowHelper.WaitForIdle();

		// Find the CheckBoxes in the visual tree
		var checkBoxes = GetCheckBoxes(repeater);
		Assert.AreEqual(3, checkBoxes.Count, "Expected 3 CheckBox items in the repeater");

		// Verify IsChecked property values are correct
		var cb0 = checkBoxes.First(cb => cb.Tag as string == "Item1");
		var cb1 = checkBoxes.First(cb => cb.Tag as string == "Item2");
		var cb2 = checkBoxes.First(cb => cb.Tag as string == "Item3");

		Assert.AreEqual(true, cb0.IsChecked, "Item1 should be checked");
		Assert.AreEqual(false, cb1.IsChecked, "Item2 should be unchecked");
		Assert.AreEqual(true, cb2.IsChecked, "Item3 should be checked");

		// Verify the visual state matches IsChecked.
		// This is the actual bug: IsChecked is true but visual state is "UncheckedNormal".
		var states0 = VisualStateHelper.GetCurrentVisualStateName(cb0).ToArray();
		var states1 = VisualStateHelper.GetCurrentVisualStateName(cb1).ToArray();
		var states2 = VisualStateHelper.GetCurrentVisualStateName(cb2).ToArray();

		Assert.IsTrue(
			states0.Any(s => s.Contains("Checked") && !s.Contains("Unchecked")),
			$"Item1 (IsChecked=true) should be in a Checked visual state, but states are: [{string.Join(", ", states0)}]");

		Assert.IsTrue(
			states1.Any(s => s.Contains("Unchecked")) || !states1.Any(s => s.Contains("Checked")),
			$"Item2 (IsChecked=false) should be in an Unchecked visual state, but states are: [{string.Join(", ", states1)}]");

		Assert.IsTrue(
			states2.Any(s => s.Contains("Checked") && !s.Contains("Unchecked")),
			$"Item3 (IsChecked=true) should be in a Checked visual state, but states are: [{string.Join(", ", states2)}]");
	}

	[TestMethod]
	public async Task When_IsChecked_Set_Before_Template_Applied()
	{
		// Simpler repro: a standalone CheckBox with IsChecked=true set before adding to visual tree
		var cb = new CheckBox { IsChecked = true };

		WindowHelper.WindowContent = cb;
		await WindowHelper.WaitForLoaded(cb);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(true, cb.IsChecked);

		var states = VisualStateHelper.GetCurrentVisualStateName(cb).ToArray();
		Assert.IsTrue(
			states.Any(s => s.Contains("Checked") && !s.Contains("Unchecked")),
			$"CheckBox with IsChecked=true should be in a Checked visual state, but states are: [{string.Join(", ", states)}]");
	}

	private static List<CheckBox> GetCheckBoxes(DependencyObject parent)
	{
		var result = new List<CheckBox>();
		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is CheckBox cb)
			{
				result.Add(cb);
			}
			else
			{
				result.AddRange(GetCheckBoxes(child));
			}
		}

		return result;
	}

	private sealed record TestItem(string Name, bool IsEnabled);
}
