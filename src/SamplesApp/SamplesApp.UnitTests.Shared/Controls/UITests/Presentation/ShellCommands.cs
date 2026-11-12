#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Entities;
using Windows.System;

namespace SampleControl.Presentation;

/// <summary>A shell action with its shortcut; the single source for accelerators, tooltips and Help.</summary>
internal sealed class ShellCommand
{
	public ShellCommand(
		string id,
		string label,
		VirtualKey key,
		VirtualKeyModifiers modifiers,
		Action<SampleChooserViewModel, SampleChooserControl> execute,
		bool alwaysEnabled = false,
		bool wasmUnsafe = false,
		string? aliasOf = null)
	{
		Id = id;
		Label = label;
		Key = key;
		Modifiers = modifiers;
		Execute = execute;
		AlwaysEnabled = alwaysEnabled;
		WasmUnsafe = wasmUnsafe;
		AliasOf = aliasOf;
	}

	public string Id { get; }

	public string Label { get; }

	public VirtualKey Key { get; }

	public VirtualKeyModifiers Modifiers { get; }

	public Action<SampleChooserViewModel, SampleChooserControl> Execute { get; }

	/// <summary>Runs even when the current sample disables keyboard shortcuts.</summary>
	public bool AlwaysEnabled { get; }

	/// <summary>The shortcut collides with a browser shortcut on WebAssembly.</summary>
	public bool WasmUnsafe { get; }

	/// <summary>The id of the entry whose action this second shortcut repeats; Help lists it on that entry's row.</summary>
	public string? AliasOf { get; }

	public bool IsAlias => AliasOf is not null;

	public string Shortcut => ShellCommands.Describe(Key, Modifiers);

	public override string ToString() => $"{Label} ({Shortcut})";
}

internal static class ShellCommands
{
	public const string FocusSearch = nameof(FocusSearch);
	public const string FocusSearchAlias = nameof(FocusSearchAlias);
	public const string ReloadSample = nameof(ReloadSample);
	public const string PreviousSample = nameof(PreviousSample);
	public const string NextSample = nameof(NextSample);
	public const string ShowFavorites = nameof(ShowFavorites);
	public const string ShowRecents = nameof(ShowRecents);
	public const string ShowRecentsAlias = nameof(ShowRecentsAlias);
	public const string ShowLibrary = nameof(ShowLibrary);
	public const string ToggleBrowser = nameof(ToggleBrowser);
	public const string ToggleFavorite = nameof(ToggleFavorite);
	public const string ShowSampleInfo = nameof(ShowSampleInfo);
	public const string CopySampleLink = nameof(CopySampleLink);
	public const string OpenRuntimeTests = nameof(OpenRuntimeTests);
	public const string OpenPlayground = nameof(OpenPlayground);
	public const string OpenHelp = nameof(OpenHelp);
	public const string ShowSettings = nameof(ShowSettings);
	public const string ShowHome = nameof(ShowHome);
	public const string ToggleFocusMode = nameof(ToggleFocusMode);
	public const string FocusNextRegion = nameof(FocusNextRegion);
	public const string FocusPreviousRegion = nameof(FocusPreviousRegion);

	/// <summary>Focuses search outside text input: a typed character, so it is not a catalogue accelerator.</summary>
	public const char SearchCharacter = '/';

	// Windows.System.VirtualKey has no named member for the OEM comma key.
	private const VirtualKey CommaKey = (VirtualKey)188;

	private const VirtualKeyModifiers Ctrl = VirtualKeyModifiers.Control;
	private const VirtualKeyModifiers Alt = VirtualKeyModifiers.Menu;
	private const VirtualKeyModifiers Shift = VirtualKeyModifiers.Shift;

	public static IReadOnlyList<ShellCommand> All { get; } = new ShellCommand[]
	{
		new(FocusSearch, "Search samples", VirtualKey.F, Ctrl, (vm, control) => _ = control.FocusSearchAsync(vm)),
		new(FocusSearchAlias, "Search samples", VirtualKey.K, Ctrl, (vm, control) => _ = control.FocusSearchAsync(vm), wasmUnsafe: true, aliasOf: FocusSearch),
		new(ReloadSample, "Reload sample", VirtualKey.F5, VirtualKeyModifiers.None, (vm, _) => Run(vm.ReloadCurrentTestCommand), wasmUnsafe: true),
		new(PreviousSample, "Previous sample", VirtualKey.Left, Alt, (vm, _) => Run(vm.LoadPreviousTestCommand), wasmUnsafe: true),
		new(NextSample, "Next sample", VirtualKey.Right, Alt, (vm, _) => Run(vm.LoadNextTestCommand), wasmUnsafe: true),
		new(ShowFavorites, "Favorites", VirtualKey.F, Ctrl | Shift, (vm, _) => vm.ShowBrowserSection(Section.Favorites)),
		new(ShowRecents, "Recent", VirtualKey.H, Ctrl, (vm, _) => vm.ShowBrowserSection(Section.Recents), wasmUnsafe: true),
		new(ShowRecentsAlias, "Recent", VirtualKey.R, Alt, (vm, _) => vm.ShowBrowserSection(Section.Recents), aliasOf: ShowRecents),
		new(ShowLibrary, "Library", VirtualKey.E, Ctrl | Shift, (vm, _) => vm.ShowBrowserSection(Section.Library)),
		new(ToggleBrowser, "Sample browser", VirtualKey.B, Ctrl, (vm, _) =>
		{
			if (CanShowBrowser(vm))
			{
				// In focus mode the pane is hidden, so leaving it always opens the browser.
				vm.IsSplitVisible = !vm.IsShellChromeVisible || !vm.IsSplitVisible;
				vm.IsShellChromeVisible = true;
			}
		}),
		new(ToggleFavorite, "Toggle favorite", VirtualKey.D, Ctrl | Shift, (vm, _) => vm.ToggleFavoriteCommand.Execute(vm.CurrentSelectedSample)),
		new(ShowSampleInfo, "Sample info", VirtualKey.I, Ctrl, (_, control) => control.ShowSampleInfo()),
		new(CopySampleLink, "Copy deep link", VirtualKey.L, Ctrl | Shift, (vm, _) => Run(vm.CopySampleLinkCommand)),
		new(OpenRuntimeTests, "Runtime tests", VirtualKey.T, Ctrl, (vm, _) => Run(vm.OpenRuntimeTestsCommand), wasmUnsafe: true),
		new(OpenPlayground, "Playground", VirtualKey.P, Ctrl, (vm, _) => Run(vm.OpenPlaygroundCommand), wasmUnsafe: true),
		new(OpenHelp, "Help", VirtualKey.F1, VirtualKeyModifiers.None, (vm, _) => Run(vm.OpenHelpCommand)),
		new(ShowSettings, "Settings", CommaKey, Ctrl, (vm, _) => Run(vm.ShowSettingsCommand)),
		new(ShowHome, "Home", VirtualKey.H, Alt | Shift, (vm, _) => Run(vm.ShowHomeCommand)),
		new(ToggleFocusMode, "Focus mode", VirtualKey.F11, VirtualKeyModifiers.None, (vm, _) => Run(vm.ToggleFocusModeCommand), alwaysEnabled: true, wasmUnsafe: true),
		new(FocusNextRegion, "Next region", VirtualKey.F6, VirtualKeyModifiers.None, (_, control) => control.MoveFocusRegion(backward: false), wasmUnsafe: true),
		new(FocusPreviousRegion, "Previous region", VirtualKey.F6, Shift, (_, control) => control.MoveFocusRegion(backward: true), wasmUnsafe: true),
	};

	/// <summary>
	/// Whether a shortcut may run: samples that disable shortcuts (the runtime-test runner) only get the
	/// always-enabled ones, and those stay off under automation so they never interfere with a CI run.
	/// </summary>
	public static bool IsEnabled(ShellCommand command, bool keyboardShortcutsEnabled, bool isAutomationRun)
		=> command.AlwaysEnabled ? !isAutomationRun : keyboardShortcutsEnabled;

	/// <summary>Shortcuts never open the sample browser while screenshots are recorded; callers leave focus mode first.</summary>
	public static bool CanShowBrowser(SampleChooserViewModel vm) => !vm.IsRecordAllTests;

	/// <summary>True when typing goes to the element, so plain-character shortcuts like "/" must not fire.</summary>
	public static bool IsTextInput(object? focused)
		=> focused is TextBox or PasswordBox or RichEditBox or AutoSuggestBox;

	/// <summary>The region indices to try, in order, when moving focus away from <paramref name="current"/> (-1 if none).</summary>
	public static IEnumerable<int> GetRegionCycle(int current, int count, bool backward)
	{
		for (var step = 1; step <= count; step++)
		{
			if (current < 0)
			{
				yield return backward ? count - step : step - 1;
			}
			else if (step < count)
			{
				yield return ((backward ? current - step : current + step) % count + count) % count;
			}
		}
	}

	public static ShellCommand? Find(string id) => All.FirstOrDefault(c => c.Id == id);

	/// <summary>The shortcut text for a command, e.g. "Ctrl+Shift+D"; empty for an unknown id.</summary>
	public static string Describe(string id) => Find(id)?.Shortcut ?? "";

	public static string Describe(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		StringBuilder builder = new();
		if (modifiers.HasFlag(VirtualKeyModifiers.Control))
		{
			builder.Append("Ctrl+");
		}

		if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
		{
			builder.Append("Alt+");
		}

		if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
		{
			builder.Append("Shift+");
		}

		builder.Append(key == CommaKey ? "," : key.ToString());
		return builder.ToString();
	}

	private static void Run(System.Windows.Input.ICommand command)
	{
		if (command.CanExecute(null))
		{
			command.Execute(null);
		}
	}
}
