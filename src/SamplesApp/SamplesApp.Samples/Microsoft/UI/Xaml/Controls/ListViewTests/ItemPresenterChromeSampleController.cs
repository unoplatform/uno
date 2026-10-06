#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView;

/// <summary>
/// Shared logic of the ListViewItemPresenter/GridViewItemPresenter chrome samples. The default container
/// template stays in place; each mode overrides presenter properties locally and restores the template values when turned off.
/// </summary>
/// <remarks>
/// The rounded chrome flag is read once per process in WinUI, so toggling it rebuilds the list to avoid recycled containers
/// carrying state from the other chrome mode.
/// </remarks>
internal sealed class ItemPresenterChromeSampleController
{
	internal const string DefaultEntry = "(template default)";
	private const string RoundedResourceKey = "ListViewBaseItemRoundedChromeEnabled";

	private readonly Border _host;
	private readonly Func<ListViewBase> _createList;
	private readonly ComboBox _selectionModeBox;
	private readonly ComboBox _checkModeBox;
	private readonly ComboBox _indicatorModeBox;
	private readonly CheckBox _roundedBox;
	private readonly CheckBox _disabledBox;
	private readonly CheckBox _brushesBox;
	private readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, object>> _originalValues = new();

	private ListViewBase _items = null!;
	private object? _previousRounded;
	private bool _roundedOverridden;
	private bool _ready;

	public ItemPresenterChromeSampleController(
		FrameworkElement page,
		Border host,
		Func<ListViewBase> createList,
		ComboBox selectionModeBox,
		ComboBox checkModeBox,
		ComboBox indicatorModeBox,
		CheckBox roundedBox,
		CheckBox disabledBox,
		CheckBox brushesBox)
	{
		_host = host;
		_createList = createList;
		_selectionModeBox = selectionModeBox;
		_checkModeBox = checkModeBox;
		_indicatorModeBox = indicatorModeBox;
		_roundedBox = roundedBox;
		_disabledBox = disabledBox;
		_brushesBox = brushesBox;

		_selectionModeBox.ItemsSource = Enum.GetNames<ListViewSelectionMode>();
		_checkModeBox.ItemsSource = Enum.GetNames<ListViewItemPresenterCheckMode>().Prepend(DefaultEntry).ToList();
		_indicatorModeBox.ItemsSource = Enum.GetNames<ListViewItemPresenterSelectionIndicatorMode>().Prepend(DefaultEntry).ToList();
		_selectionModeBox.SelectedItem = nameof(ListViewSelectionMode.Single);
		_checkModeBox.SelectedItem = DefaultEntry;
		_indicatorModeBox.SelectedItem = DefaultEntry;

		_selectionModeBox.SelectionChanged += (_, _) => ApplySelectionMode();
		_checkModeBox.SelectionChanged += (_, _) => ApplyToRealizedContainers();
		_indicatorModeBox.SelectionChanged += (_, _) => ApplyToRealizedContainers();
		_disabledBox.Checked += (_, _) => ApplyToRealizedContainers();
		_disabledBox.Unchecked += (_, _) => ApplyToRealizedContainers();
		_brushesBox.Checked += (_, _) => ApplyToRealizedContainers();
		_brushesBox.Unchecked += (_, _) => ApplyToRealizedContainers();
		_roundedBox.Checked += (_, _) => OnRoundedChanged();
		_roundedBox.Unchecked += (_, _) => OnRoundedChanged();

		RebuildList();

		page.Loaded += (_, _) =>
		{
			if (_roundedBox.IsChecked != true)
			{
				ApplyRounded();
				RebuildList();
			}
		};
		page.Unloaded += (_, _) => RestoreRounded();
		_ready = true;
	}

	/// <summary>
	/// Replaces the list with a fresh one, so that no container is reused.
	/// </summary>
	public void RebuildList()
	{
		_items = _createList();
		_items.ContainerContentChanging += (_, args) => ApplyToContainer(args.ItemContainer, args.ItemIndex);
		ApplySelectionMode();
		_items.ItemsSource = Enumerable.Range(0, 30).Select(i => $"Item {i}").ToList();
		_host.Child = _items;
	}

	private void ApplySelectionMode()
	{
		if (_items is not null && _selectionModeBox.SelectedItem is string name)
		{
			_items.SelectionMode = Enum.Parse<ListViewSelectionMode>(name);
		}
	}

	private void ApplyToRealizedContainers()
	{
		if (!_ready)
		{
			return;
		}

		var count = _items.Items.Count;
		for (var i = 0; i < count; i++)
		{
			if (_items.ContainerFromIndex(i) is SelectorItem container)
			{
				ApplyToContainer(container, i);
			}
		}
	}

	private void ApplyToContainer(SelectorItem? container, int index)
	{
		if (container is null)
		{
			return;
		}

		container.IsEnabled = _disabledBox.IsChecked != true || index % 3 != 2;

		container.ApplyTemplate();
		if (VisualTreeHelper.GetChildrenCount(container) == 0)
		{
			return;
		}

		switch (VisualTreeHelper.GetChild(container, 0))
		{
			case ListViewItemPresenter presenter:
				ApplyToPresenter(presenter);
				break;
			case GridViewItemPresenter legacyPresenter:
				ApplyToLegacyPresenter(legacyPresenter);
				break;
		}
	}

	private void ApplyToPresenter(ListViewItemPresenter presenter)
	{
		var brushes = _brushesBox.IsChecked == true;

		Override(presenter, ListViewItemPresenter.CheckModeProperty, ParseOrNull<ListViewItemPresenterCheckMode>(_checkModeBox));
		Override(presenter, ListViewItemPresenter.SelectionIndicatorModeProperty, ParseOrNull<ListViewItemPresenterSelectionIndicatorMode>(_indicatorModeBox));
		Override(presenter, ListViewItemPresenter.SelectedBackgroundProperty, brushes ? new SolidColorBrush(Colors.Gold) : null);
		Override(presenter, ListViewItemPresenter.SelectedPointerOverBackgroundProperty, brushes ? new SolidColorBrush(Colors.Orange) : null);
		Override(presenter, ListViewItemPresenter.SelectedPressedBackgroundProperty, brushes ? new SolidColorBrush(Colors.DarkOrange) : null);
		Override(presenter, ListViewItemPresenter.SelectedBorderBrushProperty, brushes ? new SolidColorBrush(Colors.DarkRed) : null);
		Override(presenter, ListViewItemPresenter.SelectedBorderThicknessProperty, brushes ? new Thickness(2) : null);
		Override(presenter, ListViewItemPresenter.PointerOverBackgroundProperty, brushes ? new SolidColorBrush(Colors.LightBlue) : null);
		Override(presenter, ListViewItemPresenter.PressedBackgroundProperty, brushes ? new SolidColorBrush(Colors.SteelBlue) : null);
		Override(presenter, ListViewItemPresenter.SelectionIndicatorBrushProperty, brushes ? new SolidColorBrush(Colors.Red) : null);
		Override(presenter, ListViewItemPresenter.CheckBrushProperty, brushes ? new SolidColorBrush(Colors.Green) : null);
		Override(presenter, ListViewItemPresenter.CheckBoxBrushProperty, brushes ? new SolidColorBrush(Colors.Purple) : null);
	}

	private void ApplyToLegacyPresenter(GridViewItemPresenter presenter)
	{
		var brushes = _brushesBox.IsChecked == true;

		Override(presenter, GridViewItemPresenter.SelectedBackgroundProperty, brushes ? new SolidColorBrush(Colors.Gold) : null);
		Override(presenter, GridViewItemPresenter.SelectedPointerOverBackgroundProperty, brushes ? new SolidColorBrush(Colors.Orange) : null);
		Override(presenter, GridViewItemPresenter.SelectedPointerOverBorderBrushProperty, brushes ? new SolidColorBrush(Colors.DarkRed) : null);
		Override(presenter, GridViewItemPresenter.SelectedBorderThicknessProperty, brushes ? new Thickness(2) : null);
		Override(presenter, GridViewItemPresenter.PointerOverBackgroundProperty, brushes ? new SolidColorBrush(Colors.LightBlue) : null);
		Override(presenter, GridViewItemPresenter.CheckBrushProperty, brushes ? new SolidColorBrush(Colors.Green) : null);
	}

	private static object? ParseOrNull<TEnum>(ComboBox box) where TEnum : struct, Enum
		=> box.SelectedItem is string name && name != DefaultEntry ? Enum.Parse<TEnum>(name) : null;

	// A null value restores what the template set before the first override.
	private void Override(DependencyObject target, DependencyProperty property, object? value)
	{
		var originals = _originalValues.GetOrCreateValue(target);
		if (value is not null)
		{
			if (!originals.ContainsKey(property))
			{
				originals[property] = target.ReadLocalValue(property);
			}

			target.SetValue(property, value);
		}
		else if (originals.Remove(property, out var original))
		{
			if (original == DependencyProperty.UnsetValue)
			{
				target.ClearValue(property);
			}
			else
			{
				target.SetValue(property, original);
			}
		}
	}

	private void OnRoundedChanged()
	{
		if (_ready)
		{
			ApplyRounded();
			RebuildList();
		}
	}

	private void ApplyRounded()
	{
		var resources = Application.Current.Resources;
		if (_roundedBox.IsChecked == true)
		{
			RestoreRounded();
			return;
		}

		if (!_roundedOverridden)
		{
			_roundedOverridden = true;
			_previousRounded = resources.ContainsKey(RoundedResourceKey) ? resources[RoundedResourceKey] : null;
		}

		resources[RoundedResourceKey] = false;
#if HAS_UNO
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
#endif
	}

	private void RestoreRounded()
	{
		if (!_roundedOverridden)
		{
			return;
		}

		var resources = Application.Current.Resources;
		if (_previousRounded is null)
		{
			resources.Remove(RoundedResourceKey);
		}
		else
		{
			resources[RoundedResourceKey] = _previousRounded;
		}

#if HAS_UNO
		ListViewBaseItemPresenter.ClearIsRoundedListViewBaseItemChromeEnabledCache();
#endif
		_roundedOverridden = false;
	}
}
