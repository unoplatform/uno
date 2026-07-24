# Quickstart: Native OS Menu Integration

Practical, copy-pasteable recipes for defining application- and window-scoped menus that
project to each platform's native menu system (macOS `NSMenu`, iPadOS `UIMenuBuilder`),
with an in-app fallback where there is none (Windows, Linux-no-registrar).

The core model lives in namespace **`Uno.UI.Xaml.Controls`** ([NativeMenu](../../src/Uno.UI),
[NativeMenuItem](../../src/Uno.UI), etc.). The declarative `AppMenuBar` control lives in the
separate **`Uno.Toolkit.UI`** package (see [section 7](#7-the-toolkit-appmenubar-control)).

> **Skia targets only.** v1 ships macOS + iPadOS. Linux (DBusMenu) and Windows are
> designed-for extension points. Where there is no native menu, the model is inert
> (`NativeMenu.IsSupported == false`) and you fall back to the in-app `AppMenuBar`.

Quick mental model:

- **The tree is just data.** `NativeMenu` / `NativeMenuItem` are lightweight
  `DependencyObject`s (not `FrameworkElement`s). You build the tree, then *attach* it.
- **Attachment is code-first.** `Window` and `Application` are **not** `DependencyObject`s
  in WinUI/Uno, so there is no attached property on them. You call
  `NativeMenu.SetApplicationMenu(...)` or `NativeMenu.SetMenu(window, ...)`.
- **The model is observable.** Mutate `Items` or any property and the native menu
  re-projects automatically (coalesced full rebuild).

---

## 1. App-wide menu, code-first

Build a `NativeMenu` tree in code and attach it app-wide. The framework **always**
guarantees the bold app-name menu with at least **Quit (Cmd+Q)** and **Hide** on macOS, so
you only declare your own top-level menus (File, Edit, ...).

```csharp
using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Uno.UI.Xaml.Controls;

public sealed partial class App : Application
{
	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		// ... window bootstrap ...
		InstallMenu();
	}

	private void InstallMenu()
	{
		NativeMenu menu = new();

		// --- Optional: customize the macOS application menu (the bold app-name slot). ---
		// Children of a Role=ApplicationMenu item MERGE into the OS app menu. Quit is
		// already guaranteed; here we add About + Settings. OS-owned roles (About) auto-wire
		// to the AppKit selector; Settings is yours to handle via Command.
		NativeMenuItem appMenu = new() { Role = NativeMenuItemRole.ApplicationMenu };
		appMenu.SubMenu = new NativeMenu
		{
			Items =
			{
				new NativeMenuItem { Text = "About MyApp", Role = NativeMenuItemRole.About },
				new NativeMenuItemSeparator(),
				new NativeMenuItem
				{
					Text = "Settings...",
					Role = NativeMenuItemRole.Settings,
					Command = ShowSettingsCommand,
				},
			},
		};
		menu.Items.Add(appMenu);

		// --- File menu ---
		NativeMenuItem file = new() { Text = "File" };
		file.SubMenu = new NativeMenu
		{
			Items =
			{
				new NativeMenuItem { Text = "New", Command = NewCommand },
				new NativeMenuItem { Text = "Open...", Command = OpenCommand },
				new NativeMenuItemSeparator(),
				new NativeMenuItem { Text = "Close", Command = CloseCommand },
			},
		};
		menu.Items.Add(file);

		// --- Edit menu (role-placed; YOU supply the Command on every platform — Uno draws
		// its own controls and is not a native first responder, so there is no responder
		// chain to bridge). ---
		NativeMenuItem edit = new() { Text = "Edit" };
		edit.SubMenu = new NativeMenu
		{
			Items =
			{
				new NativeMenuItem { Role = NativeMenuItemRole.Undo, Command = UndoCommand },
				new NativeMenuItem { Role = NativeMenuItemRole.Redo, Command = RedoCommand },
				new NativeMenuItemSeparator(),
				new NativeMenuItem { Role = NativeMenuItemRole.Cut, Command = CutCommand },
				new NativeMenuItem { Role = NativeMenuItemRole.Copy, Command = CopyCommand },
				new NativeMenuItem { Role = NativeMenuItemRole.Paste, Command = PasteCommand },
			},
		};
		menu.Items.Add(edit);

		// Attach app-wide. Quit (Cmd+Q) is guaranteed even if you never declared it.
		NativeMenu.SetApplicationMenu(menu);
	}

	// ... your ICommand fields (see section 3) ...
}
```

> `Role` items that you do not give a `Text` fall back to the **standard OS label** for
> that role (e.g. `Cut`/`Copy`/`Paste`). Set `Text` to override. OS-only roles with no
> `Command` no-op on Windows/Linux in-app.

---

## 2. App-wide menu, declared in XAML as a resource, assigned in code

The tree itself **can** be declared in XAML (its content property is `Items`), then
assigned via the code-first setter. This keeps the menu shape readable and `x:Bind`-able.
Declare it as a resource so it is not added to the visual tree.

```xml
<!-- App.xaml -->
<Application
	x:Class="MyApp.App"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:menu="using:Uno.UI.Xaml.Controls">
	<Application.Resources>
		<menu:NativeMenu x:Key="AppMenu">
			<menu:NativeMenuItem Role="ApplicationMenu">
				<menu:NativeMenuItem.SubMenu>
					<menu:NativeMenu>
						<menu:NativeMenuItem Text="About MyApp" Role="About" />
						<menu:NativeMenuItemSeparator />
						<menu:NativeMenuItem Text="Settings..." Role="Settings" Command="{x:Bind ShowSettingsCommand}" />
					</menu:NativeMenu>
				</menu:NativeMenuItem.SubMenu>
			</menu:NativeMenuItem>

			<menu:NativeMenuItem Text="File">
				<menu:NativeMenuItem.SubMenu>
					<menu:NativeMenu>
						<menu:NativeMenuItem Text="New" Command="{x:Bind NewCommand}" />
						<menu:NativeMenuItem Text="Open..." Command="{x:Bind OpenCommand}" />
						<menu:NativeMenuItemSeparator />
						<menu:NativeMenuItem Text="Close" Command="{x:Bind CloseCommand}" />
					</menu:NativeMenu>
				</menu:NativeMenuItem.SubMenu>
			</menu:NativeMenuItem>
		</menu:NativeMenu>
	</Application.Resources>
</Application>
```

```csharp
// App.xaml.cs — pull the resource and attach it.
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
	// ... window bootstrap ...

	NativeMenu menu = (NativeMenu)Resources["AppMenu"];
	NativeMenu.SetApplicationMenu(menu);
}
```

> The menu tree is data, so declaring it in `Application.Resources` (or a page's resources)
> never renders it into the visual tree — it is only realized when projected to the native
> menu by the setter.

---

## 3. Commands & shortcuts

Items execute via **`Command` (ICommand) + `CommandParameter`** *and* a **`Click` event**
(use whichever you prefer; both are supported for WinUI/Avalonia parity). Reusing a
`XamlUICommand` / `StandardUICommand` auto-populates `Text`, `Icon`, and the accelerator.

```csharp
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Uno.UI.Xaml.Controls;

// (a) Plain ICommand + an explicit accelerator.
NativeMenuItem save = new()
{
	Text = "Save",
	Command = SaveCommand,
};
// Cmd+S on macOS, Ctrl+S on Windows — see the literal-modifier note below.
save.KeyboardAccelerators.Add(new KeyboardAccelerator
{
	Key = VirtualKey.S,
	Modifiers = VirtualKeyModifiers.Windows, // Command on Mac (see note)
});

// (b) StandardUICommand — Label / Icon / accelerator come for free.
NativeMenuItem paste = new()
{
	Command = new StandardUICommand(StandardUICommandKind.Paste),
};

// (c) Click event instead of (or in addition to) a Command.
NativeMenuItem ping = new() { Text = "Ping" };
ping.Click += OnPingClicked;

void OnPingClicked(object? sender, NativeMenuItemClickEventArgs e) => Debug.WriteLine("pong");
```

### Literal modifiers — no Control→Command remap

Shortcuts reuse `KeyboardAccelerator` modifiers **literally**, matching Uno's live macOS key
mapping ([UNOWindow.m:878-889](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOWindow.m)).
There is deliberately **no** menu-only remap (that would be incoherent with the key events
your app already receives):

| `VirtualKeyModifiers` | macOS | Windows / Linux |
|-----------------------|-------|-----------------|
| `Windows`             | **Command (⌘)** | Windows key |
| `Control`             | Control (⌃) | **Ctrl** |
| `Menu`                | Option/Alt (⌥) | Alt |
| `Shift`               | Shift (⇧) | Shift |

So a cross-platform "primary" shortcut (Cmd on Mac, Ctrl on Win) needs **per-platform
markup** today:

```csharp
var primary = OperatingSystem.IsMacOS()
	? VirtualKeyModifiers.Windows   // ⌘
	: VirtualKeyModifiers.Control;  // Ctrl

save.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = primary });
```

> An optional `Primary`-modifier sugar (write once, maps per platform) is a possible future
> follow-up — it is **not** part of v1.

---

## 4. Checkable + radio items

Use `ToggleType` (`None` / `CheckBox` / `Radio`) with `IsChecked`, and `GroupName` for radio
exclusivity (mirrors `RadioMenuFlyoutItem`). On macOS the framework coordinates radio
exclusivity itself and pushes the state; Linux uses native `toggle-type=radio`; iPadOS uses
inline single-selection.

```csharp
using Uno.UI.Xaml.Controls;

NativeMenu view = new();

// Checkable toggle.
NativeMenuItem statusBar = new()
{
	Text = "Show Status Bar",
	ToggleType = ToggleType.CheckBox,
	IsChecked = true,
	Command = ToggleStatusBarCommand,
};
view.Items.Add(statusBar);

view.Items.Add(new NativeMenuItemSeparator());

// Radio group — exactly one checked across the shared GroupName.
view.Items.Add(new NativeMenuItem
{
	Text = "Small",
	ToggleType = ToggleType.Radio,
	GroupName = "TextSize",
	Command = SetSizeCommand,
	CommandParameter = "small",
});
view.Items.Add(new NativeMenuItem
{
	Text = "Medium",
	ToggleType = ToggleType.Radio,
	GroupName = "TextSize",
	IsChecked = true,
	Command = SetSizeCommand,
	CommandParameter = "medium",
});
view.Items.Add(new NativeMenuItem
{
	Text = "Large",
	ToggleType = ToggleType.Radio,
	GroupName = "TextSize",
	Command = SetSizeCommand,
	CommandParameter = "large",
});
```

> Effective-enabled is **pushed**: `IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)`.
> macOS sets `NSMenu.autoenablesItems = NO` so your pushed enabled/checked state is
> authoritative — toggle `IsChecked` / `IsEnabled` on the model and it reflects natively.

---

## 5. Per-window / per-document menu

Attach a menu to a specific `Window` with `NativeMenu.SetMenu(window, menu)`. This is ideal
for document windows that need a different File menu per open document.

```csharp
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

void OpenDocumentWindow(Document doc)
{
	Window window = new();
	// ... set window content ...

	NativeMenu menu = BuildMenuFor(doc);
	NativeMenu.SetMenu(window, menu);   // window-scoped

	window.Activate();
}

// Read it back later:
NativeMenu? current = NativeMenu.GetMenu(window);
```

**Key-window-wins.** The system menu reflects the **focused (key) window's** menu if it set
one; otherwise it falls back to the **Application-level** menu from
`SetApplicationMenu(...)`. On macOS the backend swaps `NSApp.mainMenu` on
`windowDidBecomeKey` and restores on resign, so activating different windows swaps the menu
automatically.

> Window↔menu associations are stored in a `ConditionalWeakTable` side-table (because
> `Window` is not a `DependencyObject`), so they do not keep windows alive.

> **iPadOS v1 = app-wide only.** On iPadOS 26 the same `NativeMenu` model surfaces in the
> always-available, macOS-like system menu bar (swipe down from the top of the screen — no
> hardware keyboard required), built via `UIMenuBuilder`/`UIMainMenuSystem`. Uno AppleUIKit is
> single-scene today
> ([NativeWindowFactoryExtension.cs](../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/NativeWindowFactoryExtension.cs)),
> so `SetMenu(window, ...)` resolves to the app-global menu there. Per-scene override is
> designed-for and lands when Uno gains multi-scene. On Windows, per-window == per-control
> in-app.

---

## 6. Dynamic updates + lazy submenu population

The model is observable: mutate any property or the `Items` collection
(`INotifyCollectionChanged`) and the affected menu is marked dirty, coalesced on the
dispatcher, and re-projected via a **full rebuild** of that menu. `x:Bind`/MVVM flows
naturally.

```csharp
// Just mutate — the native menu re-projects on the next dispatcher tick.
saveItem.IsEnabled = document.HasUnsavedChanges;
recentMenu.Items.Add(new NativeMenuItem { Text = path, Command = OpenRecentCommand, CommandParameter = path });
recentMenu.Items.Clear();
```

For **just-in-time** population (e.g. a Recent Files list you do not want to rebuild on every
change), handle the **`Opening`** event on the submenu's `NativeMenu`. It maps to
`NSMenuDelegate.menuNeedsUpdate` on macOS, DBus `AboutToShow` on Linux, and the rebuild pass
on iPadOS.

```csharp
using System;
using Uno.UI.Xaml.Controls;

NativeMenuItem recent = new() { Text = "Open Recent" };
NativeMenu recentMenu = new();
recent.SubMenu = recentMenu;

// Populate fresh each time the submenu is about to open.
recentMenu.Opening += OnRecentOpening;

void OnRecentOpening(object? sender, NativeMenuOpeningEventArgs e)
{
	recentMenu.Items.Clear();

	foreach (string path in _recentFilesService.GetRecent())
	{
		recentMenu.Items.Add(new NativeMenuItem
		{
			Text = System.IO.Path.GetFileName(path),
			Command = OpenRecentCommand,
			CommandParameter = path,
		});
	}

	if (recentMenu.Items.Count == 0)
	{
		recentMenu.Items.Add(new NativeMenuItem { Text = "No Recent Files", IsEnabled = false });
	}
}
```

> There is also a `Closed` event on `NativeMenu` if you need teardown. Native menu APIs are
> main-thread; the coalesced rebuild posts to the UI dispatcher for you, so you can mutate
> the model from the UI thread freely.

---

## 7. The Toolkit `AppMenuBar` control

If you prefer a **declarative XAML control** over the code-first setters, use
**`AppMenuBar : MenuBar`** from the **`Uno.Toolkit.UI`** package (namespace
`Uno.Toolkit.UI`). It is the same `MenuBar`/`MenuFlyoutItem` vocabulary you already know:

- On **Windows / Linux-no-native**: it renders as a **real in-app `MenuBar`**.
- On **Apple (macOS / iPadOS)**: it reads its `MenuBarItem` / `MenuFlyoutItem` content,
  translates to a `NativeMenu`, projects to the native menu, and **collapses to a zero-size
  in-app footprint**.

Map OS standard slots with the attached property **`AppMenu.Role`** (Toolkit), which maps to
`NativeMenuItem.Role`.

```xml
<!-- MainPage.xaml — requires the Uno.Toolkit.UI NuGet package -->
<Page
	x:Class="MyApp.MainPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:utu="using:Uno.Toolkit.UI">

	<Grid>
		<utu:AppMenuBar>
			<MenuBarItem Title="File">
				<MenuFlyoutItem Text="New" Command="{x:Bind NewCommand}" />
				<MenuFlyoutItem Text="Open..." Command="{x:Bind OpenCommand}" />
				<MenuFlyoutSeparator />
				<MenuFlyoutItem Text="Close" Command="{x:Bind CloseCommand}" />
			</MenuBarItem>

			<MenuBarItem Title="Edit">
				<!-- Role marks the OS slot + standard label; YOU supply the Command. -->
				<MenuFlyoutItem utu:AppMenu.Role="Undo" Command="{x:Bind UndoCommand}" />
				<MenuFlyoutItem utu:AppMenu.Role="Redo" Command="{x:Bind RedoCommand}" />
				<MenuFlyoutSeparator />
				<MenuFlyoutItem utu:AppMenu.Role="Cut" Command="{x:Bind CutCommand}" />
				<MenuFlyoutItem utu:AppMenu.Role="Copy" Command="{x:Bind CopyCommand}" />
				<MenuFlyoutItem utu:AppMenu.Role="Paste" Command="{x:Bind PasteCommand}" />
			</MenuBarItem>
		</utu:AppMenuBar>

		<!-- ... rest of your page ... -->
	</Grid>
</Page>
```

> Under the hood `AppMenuBar` calls the core seam `NativeMenu.SetMenu(window, translatedMenu)`
> for you. The dependency direction is one-way: the Toolkit control depends on the core seam,
> never the reverse.

---

## 8. Capability probe

Adapt your UI to what the current platform actually supports. `NativeMenu.IsSupported` tells
you whether a native menu exists here at all; `IsRoleSupported(role)` probes a specific OS
slot (Flutter-borrowed).

```csharp
using Uno.UI.Xaml.Controls;

if (NativeMenu.IsSupported)
{
	// macOS / iPadOS: project to the native menu bar.
	NativeMenu.SetApplicationMenu(BuildNativeMenu());
}
else
{
	// Windows / Linux-no-registrar: show the in-app AppMenuBar (or your own MenuBar).
	ShowInAppMenuBar();
}

// Only offer "Quit" in your own UI where the OS does NOT own a Quit slot.
bool osHasQuit = NativeMenu.IsRoleSupported(NativeMenuItemRole.Quit);
```

> On Win32 the native extension is a no-op (`IsExported == false`); the in-app `AppMenuBar`
> renders the real `MenuBar`. On Linux, native projection is available only when a DBusMenu
> registrar is present (else fall back in-app).

---

## 9. Platform behavior cheat-sheet

What shows where, at a glance:

| Capability | macOS | iPadOS | Linux (DBusMenu) | Windows |
|------------|-------|--------|------------------|---------|
| App-wide menu | Yes (`NSApp.mainMenu`) | Yes — always-available bar on iPadOS 26 (`UIMenuBuilder`/`UIMainMenuSystem`) | No (per-window only) | No native (in-app) |
| Per-window menu | Yes (swap on key) | No in v1 (single-scene) | Yes (the only model) | Per-control (in-app) |
| Submenu / separator / checkable | Yes | Yes | Yes | Yes (in-app) |
| Radio | Framework-coordinated | Inline single-selection | `toggle-type=radio` | In-app |
| Icon | Yes | Yes | Partial (icon-name/data) | In-app |
| Shortcut | Yes | Yes | Partial (panel-dependent) | In-app accelerators |
| Enable/disable + visibility | Yes (pushed) | Yes (pushed) | Yes (pushed) | Yes (in-app) |
| Dynamic update | Live | Rebuild-only | Re-export | In-app |
| OS std items (About/Quit/Services) | Yes | Yes | No | No |

> **Rule of thumb:** build one `NativeMenu` tree, attach it once, and probe
> `IsSupported` / `IsRoleSupported` only where you need to adapt the surrounding UI. The
> projection seam handles the rest per host.

> **iPadOS discoverability tip:** on the iPadOS 26 menu bar, keep unavailable commands
> **visible but disabled** (toggle `IsEnabled`, don't remove them) so users can discover
> what the app can do.
