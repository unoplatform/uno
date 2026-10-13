#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_ListViewItemPresenter
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Theme_Changes_Realized_And_Recycled_Presenters_Follow()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var list = new ListView { Height = 400, ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray() };
		var root = new StackPanel { RequestedTheme = ElementTheme.Light, Children = { list } };

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = false;
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitForIdle();

			var light = await GetReferenceBrushes(root);
			AssertPresentersMatch(list, light, "Light");

			await SwitchWithRecycledContainers(list, () => root.RequestedTheme = ElementTheme.Dark);

			var dark = await GetReferenceBrushes(root);
			Assert.AreNotEqual(light, dark, "The compared brushes must differ between Light and Dark");
			AssertPresentersMatch(list, dark, "Dark");

			await SwitchWithRecycledContainers(list, () => Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true);

			var highContrast = await GetReferenceBrushes(root);
			Assert.AreNotEqual(dark, highContrast, "The compared brushes must differ between Dark and high contrast");
			AssertPresentersMatch(list, highContrast, "HighContrast");
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	// Shrinks the list so most containers are recycled while the theme changes, then brings them back.
	private static async Task SwitchWithRecycledContainers(ListView list, Action switchTheme)
	{
		var realized = GetPresenters(list).ToHashSet();

		list.Height = 40;
		await WindowHelper.WaitForIdle();

		var recycled = realized.Except(GetPresenters(list)).ToArray();
		Assert.IsTrue(recycled.Length > 0, "Shrinking the list should recycle containers");

		switchTheme();
		await WindowHelper.WaitForIdle();

		list.Height = 400;
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(GetPresenters(list).Intersect(recycled).Any(), "Restoring the height should bring recycled containers back");
	}

	// A list built after the switch resolves every brush once, in the current theme.
	private static async Task<string> GetReferenceBrushes(Panel root)
	{
		var reference = new ListView { Height = 60, ItemsSource = new[] { "Reference" } };
		root.Children.Add(reference);
		await WindowHelper.WaitForLoaded(reference);
		await WindowHelper.WaitForIdle();

		var brushes = DescribeBrushes(GetPresenters(reference).Single());
		root.Children.Remove(reference);
		await WindowHelper.WaitForIdle();

		return brushes;
	}

	private static void AssertPresentersMatch(ListView list, string expected, string theme)
	{
		var presenters = GetPresenters(list).ToArray();
		Assert.IsTrue(presenters.Length > 5, "The list should realize several containers");

		foreach (var presenter in presenters)
		{
			Assert.AreEqual(expected, DescribeBrushes(presenter), $"Presenter of '{((ListViewItem)presenter.GetParent()!).Content}' in {theme}");
		}
	}

	private static IEnumerable<ListViewItemPresenter> GetPresenters(ListView list)
		=> list.ItemsPanelRoot!.Children
			.OfType<ListViewItem>()
			.Where(item => item.Visibility == Visibility.Visible && item.IsLoaded)
			.Select(item => (ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0));

	private static string DescribeBrushes(ListViewItemPresenter presenter)
		=> string.Join(", ", new[]
		{
			presenter.PointerOverBackground,
			presenter.PressedBackground,
			presenter.SelectedBackground,
			presenter.SelectedPointerOverBackground,
			presenter.SelectedPressedBackground,
			presenter.PointerOverForeground,
			presenter.SelectedForeground,
			presenter.CheckBrush,
			presenter.CheckBoxBrush,
			presenter.CheckBoxBorderBrush,
			presenter.SelectionIndicatorBrush,
			presenter.FocusBorderBrush,
		}.Select(brush => brush is SolidColorBrush solid ? solid.Color.ToString() : brush?.GetType().Name ?? "null"));
}
#endif
