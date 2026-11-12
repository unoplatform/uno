#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SampleControl.Presentation;

namespace Uno.UI.Samples.Controls;

partial class SampleChooserControl
{
	// F6 cycles through these in order; names that do not exist yet are skipped.
	private static readonly string[] _focusRegionNames = { "ShellRail", "ShellHeader", "ShellBrowserPane", "ShellHostLayer" };

	private readonly Dictionary<KeyboardAccelerator, ShellCommand> _shellAccelerators = new();
	private SampleChooserViewModel? _shortcutsViewModel;

	private void InitializeShortcuts()
	{
		// The mode is inherited: hide the root's accelerator tooltip, but give hosted samples and tests the default back.
		KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
		ShellHostLayer.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Auto;

		foreach (var command in ShellCommands.All)
		{
			KeyboardAccelerator accelerator = new()
			{
				Key = command.Key,
				Modifiers = command.Modifiers,
			};
			_shellAccelerators.Add(accelerator, command);
			accelerator.Invoked += OnShellAccelerator;
			KeyboardAccelerators.Add(accelerator);
		}

		CharacterReceived += OnShellCharacterReceived;
		DataContextChanged += OnShortcutsDataContextChanged;
	}

	private void OnShortcutsDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (_shortcutsViewModel is not null)
		{
			_shortcutsViewModel.PropertyChanged -= OnShortcutsViewModelPropertyChanged;
		}

		_shortcutsViewModel = DataContext as SampleChooserViewModel;

		if (_shortcutsViewModel is not null)
		{
			_shortcutsViewModel.PropertyChanged += OnShortcutsViewModelPropertyChanged;
		}

		UpdateAcceleratorsEnabled();
	}

	private void OnShortcutsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(SampleChooserViewModel.KeyboardShortcutsEnabled) or nameof(SampleChooserViewModel.IsAutomationRun))
		{
			UpdateAcceleratorsEnabled();
		}
	}

	// A disabled accelerator is skipped by the lookup, so a sample's own accelerator on the same keys still runs.
	private void UpdateAcceleratorsEnabled()
	{
		foreach (var (accelerator, command) in _shellAccelerators)
		{
			accelerator.IsEnabled = IsShellCommandEnabled(command);
		}
	}

	private bool IsShellCommandEnabled(ShellCommand command)
		=> _shortcutsViewModel is { } vm && ShellCommands.IsEnabled(command, vm.KeyboardShortcutsEnabled, vm.IsAutomationRun);

	private void OnShellAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		if (!_shellAccelerators.TryGetValue(sender, out var command)
			|| _shortcutsViewModel is not { } vm
			|| !IsShellCommandEnabled(command))
		{
			return;
		}

		command.Execute(vm, this);
		args.Handled = true;
	}

	private void OnShellCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
	{
		if (args.Handled
			|| args.Character != ShellCommands.SearchCharacter
			|| _shortcutsViewModel is not { KeyboardShortcutsEnabled: true } vm
			|| !ShellCommands.CanShowBrowser(vm)
			|| XamlRoot is null
			|| ShellCommands.IsTextInput(FocusManager.GetFocusedElement(XamlRoot)))
		{
			return;
		}

		_ = FocusSearchAsync(vm);
		args.Handled = true;
	}

	internal async Task FocusSearchAsync(SampleChooserViewModel vm)
	{
		if (!ShellCommands.CanShowBrowser(vm))
		{
			return;
		}

		vm.IsShellChromeVisible = true;

		// Narrow windows trade the header box for one at the top of the browser pane.
		var target = SearchBox;
		if (SearchBox.Visibility == Visibility.Collapsed)
		{
			vm.BrowserView = BrowserView.Samples;
			SplitView.IsPaneOpen = true;
			target = ShellPaneSearchBox;
		}

		// A pane or header that is still being shown is not focusable yet, so retry for a few frames.
		for (var attempt = 0; attempt < 20 && !target.Focus(FocusState.Keyboard); attempt++)
		{
			await Task.Delay(16);
		}
	}

	internal void ShowSampleInfo()
	{
		if (_isNarrow)
		{
			ShellInfoFlyout.ShowAt(ShellHeader, new FlyoutShowOptions { Placement = FlyoutPlacementMode.Bottom });
		}
		else
		{
			ShellInfoFlyout.ShowAt(GetCommandAnchor(InfoButton));
		}
	}

	internal bool MoveFocusRegion(bool backward)
	{
		var regions = _focusRegionNames
			.Select(name => FindName(name) as FrameworkElement)
			.ToList();

		return MoveFocusRegion(regions, SplitView, ShellBrowserPane, backward);
	}

	internal static bool MoveFocusRegion(IReadOnlyList<FrameworkElement?> regions, SplitView splitView, FrameworkElement browserPane, bool backward)
	{
		if (splitView.XamlRoot is not { } xamlRoot)
		{
			return false;
		}

		var current = IndexOfRegionContaining(regions, FocusManager.GetFocusedElement(xamlRoot) as DependencyObject);

		foreach (var index in ShellCommands.GetRegionCycle(current, regions.Count, backward))
		{
			if (regions[index] is { } region
				&& IsShownRegion(region, splitView, browserPane)
				&& FocusManager.FindFirstFocusableElement(region) is UIElement target)
			{
				// A light-dismiss pane would stay on top of the target, and closing it later restores the old focus.
				if (!ReferenceEquals(region, browserPane) && IsLightDismissPaneOpen(splitView))
				{
					splitView.IsPaneOpen = false;
				}

				if (target.Focus(FocusState.Keyboard))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsLightDismissPaneOpen(SplitView splitView)
		=> splitView.IsPaneOpen && splitView.DisplayMode is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.CompactOverlay;

	private static int IndexOfRegionContaining(IReadOnlyList<FrameworkElement?> regions, DependencyObject? element)
	{
		for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			for (var i = 0; i < regions.Count; i++)
			{
				if (ReferenceEquals(regions[i], current))
				{
					return i;
				}
			}
		}

		return -1;
	}

	private static bool IsShownRegion(FrameworkElement region, SplitView splitView, FrameworkElement browserPane)
	{
		if (ReferenceEquals(region, browserPane) && !splitView.IsPaneOpen)
		{
			return false;
		}

		if (region.ActualWidth <= 0 || region.ActualHeight <= 0)
		{
			return false;
		}

		for (DependencyObject? current = region; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (current is UIElement { Visibility: Visibility.Collapsed })
			{
				return false;
			}
		}

		return true;
	}
}
