# Data Model: Native OS Menu Integration

The "data model" is the **public API surface as a typed object model** — a lightweight,
observable menu tree plus an attachment side-table and a per-host projection seam. The core
types live in `Uno.UI` (namespace `Uno.UI.Xaml.Controls`); the declarative `AppMenuBar`
control lives in `Uno.Toolkit.UI` and depends on the core seam (never the reverse).

The tree is the cross-platform **source of truth**; each native host projects it to its own
menu system (macOS `NSMenu`, iPadOS `UIMenuBuilder`, Linux `DBusMenu` post-v1) or renders an
in-app fallback (Windows / Linux-no-registrar). Projection is a **full coalesced rebuild**,
not incremental diffing.

## Type hierarchy

```
DependencyObject
└── NativeMenuItemBase                      (abstract; Parent back-reference)
    ├── NativeMenu                          (a menu / submenu container)
    │     └── Items : IList<NativeMenuItemBase>   (XAML content property, observable)
    ├── NativeMenuItem                      (a leaf or submenu-owning command)
    │     └── SubMenu : NativeMenu?               (nesting → submenu)
    └── NativeMenuItemSeparator             (a divider)
```

- `NativeMenu` and `NativeMenuItem` are **not** `FrameworkElement`s — no visual tree, no
  template, no measure/arrange. They are `DependencyObject`s so properties are
  `DependencyProperty`-backed (change callbacks drive the dirty/rebuild flow) and so XAML can
  declare the tree as a resource/object graph.
- Nesting: a `NativeMenu`'s `Items` hold `NativeMenuItemBase`. A `NativeMenuItem` becomes a
  submenu parent by setting `SubMenu` to a child `NativeMenu`. `Parent` is maintained by the
  owning collection/property setter for upward dirty propagation.

## Core types — `Uno.UI.Xaml.Controls`

### `NativeMenuItemBase`

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Parent` | `NativeMenuItemBase?` | `null` | Back-reference to the owning `NativeMenu` (set by collection) or owning `NativeMenuItem` (for a `SubMenu`); used to propagate "dirty" up to the root menu being projected. |

```csharp
namespace Uno.UI.Xaml.Controls;

public abstract partial class NativeMenuItemBase : DependencyObject
{
	internal NativeMenuItemBase? Parent { get; set; }
}
```

### `NativeMenu`

A container of items; also serves as a submenu and as the root assigned to a scope.

| Member | Type | Default | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|---|---|
| `Items` | `IList<NativeMenuItemBase>` | empty observable list | Ordered child items. **XAML content property.** Implements `INotifyCollectionChanged`; add/remove/move/reset marks the menu dirty. | `NSMenu` item array | menu children for `UIMenu` | DBusMenu child layout | in-app `MenuBarItem` children |
| `Title` | `string?` | `null` | Optional title for the menu when used as a submenu/top-level header (the owning `NativeMenuItem.Text` usually supplies this; `Title` is the standalone form). | `NSMenu.title` | `UIMenu.title` | submenu label | header text |
| `Opening` | `EventHandler<NativeMenuOpeningEventArgs>` | — | Raised just before the menu is shown — the just-in-time population hook. | `NSMenuDelegate.menuNeedsUpdate:` | `buildMenu(with:)` pass | DBus `AboutToShow` | flyout `Opening` |
| `Closed` | `EventHandler<NativeMenuClosedEventArgs>` | — | Raised after the menu is dismissed. | `NSMenuDelegate.menuDidClose:` | (best-effort) | DBus closed | flyout `Closed` |

```csharp
namespace Uno.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Items))]
public partial class NativeMenu : NativeMenuItemBase
{
	public IList<NativeMenuItemBase> Items { get; } // observable; INotifyCollectionChanged

	[GeneratedDependencyProperty(DefaultValue = null)]
	public string? Title { get; set; }

	public event EventHandler<NativeMenuOpeningEventArgs>? Opening;
	public event EventHandler<NativeMenuClosedEventArgs>? Closed;
}

public sealed partial class NativeMenuOpeningEventArgs : EventArgs;
public sealed partial class NativeMenuClosedEventArgs : EventArgs;
```

**Observability:** `Items` raises `INotifyCollectionChanged`; every `DependencyProperty` on
items raises its change callback. Both feed the same dirty/coalesce/rebuild pipeline (see
[State & lifecycle](#state--lifecycle)).

### `NativeMenuItem`

A single command, a checkable item, or a submenu owner.

| Member | Type | Default | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|---|---|
| `Text` | `string` | `""` | Display label. Mnemonics per platform convention. | `NSMenuItem.title` | `UICommand`/`UIAction.title` | DBus `label` | item text |
| `Icon` | `IconSource?` | `null` | Best-effort icon. Bitmap/Image → PNG bytes; Symbol/Font → rendered glyph; optional SF-Symbol name passthrough on Apple. | `NSMenuItem.image` (`NSImage`) | `UIImage` (SF Symbol preferred) | icon-name / icon-data (partial) | in-app `IconElement` |
| `Command` | `ICommand?` | `null` | Invoked on activation. `XamlUICommand`/`StandardUICommand` auto-populate `Text`/`Icon`/`KeyboardAccelerators` via `CommandingHelpers`. | target/action invokes callback | `UIAction` handler | DBus `clicked` | `Click`/command |
| `CommandParameter` | `object?` | `null` | Passed to `Command.Execute` / `CanExecute`. | — | — | — | — |
| `KeyboardAccelerators` | `IList<KeyboardAccelerator>` | empty | Literal accelerators (modifiers reused as-is — no Ctrl→Cmd remap; see [Shortcuts](#shortcut-modifier-mapping)). | `keyEquivalent` + `keyEquivalentModifierMask` | `UIKeyCommand` | accelerator (partial) | in-app accelerator |
| `IsEnabled` | `bool` | `true` | Author-set enabled flag. Effective-enabled = `IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)` — **pushed**, authoritative. | `NSMenuItem.enabled` (`autoenablesItems=NO`) | `UIAction` `.disabled` attribute | DBus `enabled` | in-app `IsEnabled` |
| `IsChecked` | `bool` | `false` | Check/toggle state (with `ToggleType`). | `NSMenuItem.state` on/off | `.on`/`.off`/`.mixed` | toggle-state | in-app check |
| `ToggleType` | `ToggleType` | `None` | None / CheckBox / Radio (mirrors `RadioMenuFlyoutItem`). | framework-coordinated radio | inline single-selection | `toggle-type` checkmark/radio | in-app toggle |
| `GroupName` | `string?` | `null` | Radio exclusivity group (only meaningful when `ToggleType == Radio`). | framework coordinates exclusivity | inline group section | radio group | in-app group |
| `IsVisible` | `bool` | `true` | When false the item is omitted from the projected menu. | item removed on rebuild | `.hidden` attribute / omitted | omitted | collapsed |
| `Role` | `NativeMenuItemRole` | `None` | OS standard-slot marker (placement + standard label; some OS-owned, see [enum table](#nativemenuitemrole)). | maps to selector / slot | maps to system command id | placement only | label only / no-op |
| `SubMenu` | `NativeMenu?` | `null` | Child menu — makes this item a submenu. | `NSMenuItem.submenu` | nested `UIMenu` | child layout | nested `MenuFlyout` |
| `Click` | `EventHandler<NativeMenuItemClickEventArgs>` | — | Raised on activation (WinUI/Avalonia parity), in addition to `Command`. | action callback | `UIAction` handler | `clicked` | `Click` |

```csharp
namespace Uno.UI.Xaml.Controls;

public partial class NativeMenuItem : NativeMenuItemBase
{
	[GeneratedDependencyProperty(DefaultValue = "")]
	public string Text { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public IconSource? Icon { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public ICommand? Command { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public object? CommandParameter { get; set; }

	public IList<KeyboardAccelerator> KeyboardAccelerators { get; }

	[GeneratedDependencyProperty(DefaultValue = true)]
	public bool IsEnabled { get; set; }

	[GeneratedDependencyProperty(DefaultValue = false)]
	public bool IsChecked { get; set; }

	[GeneratedDependencyProperty(DefaultValue = ToggleType.None)]
	public ToggleType ToggleType { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public string? GroupName { get; set; }

	[GeneratedDependencyProperty(DefaultValue = true)]
	public bool IsVisible { get; set; }

	[GeneratedDependencyProperty(DefaultValue = NativeMenuItemRole.None)]
	public NativeMenuItemRole Role { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public NativeMenu? SubMenu { get; set; }

	public event EventHandler<NativeMenuItemClickEventArgs>? Click;
}

public sealed partial class NativeMenuItemClickEventArgs : EventArgs;
```

### `NativeMenuItemSeparator`

| Member | Type | Default | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|---|---|
| *(no public members)* | — | — | A visual divider between item groups. | `NSMenuItem.separatorItem` | inline section boundary | DBus `type=separator` | in-app separator |

```csharp
namespace Uno.UI.Xaml.Controls;

public sealed partial class NativeMenuItemSeparator : NativeMenuItemBase;
```

## Enums

### `NativeMenuItemRole`

THIN slot-markers. A role marks **where** an item sits in the OS-standard layout and supplies a
**standard label**. Some roles are **OS-owned/auto-wired** on macOS (AppKit selector is attached
automatically, no developer `Command` needed). All other roles are **placement-only**: the role
sets placement + standard label, but the **developer must supply `Command` + enabled logic** on
every platform (Uno draws its own controls and is not a native first responder — there is no
responder-chain bridging for edit roles).

| Role | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|
| `None` | ordinary item | ordinary item | ordinary item | ordinary item |
| `ApplicationMenu` | bold app-name menu; children **merge** into the app menu | app section | n/a | label-only |
| `About` | **auto-wired** `orderFrontStandardAboutPanel:` | system About | n/a | dev `Command` |
| `Settings` | **auto-wired** Preferences slot (Cmd+,) | Settings slot | n/a | dev `Command` |
| `Services` | **auto-wired** Services submenu | n/a | n/a | n/a |
| `Hide` | **auto-wired** `hide:` (Cmd+H) | n/a | n/a | n/a |
| `HideOthers` | **auto-wired** `hideOtherApplications:` | n/a | n/a | n/a |
| `ShowAll` | **auto-wired** `unhideAllApplications:` | n/a | n/a | n/a |
| `Quit` | **auto-wired** `terminate:` (Cmd+Q) | n/a | n/a | dev `Command` |
| `Window` | Window menu slot | Window section | n/a | label-only |
| `Minimize` | **auto-wired** `performMiniaturize:` (Cmd+M) | n/a | n/a | dev `Command` |
| `Zoom` | **auto-wired** `performZoom:` | n/a | n/a | dev `Command` |
| `EnterFullScreen` | **auto-wired** `toggleFullScreen:` | n/a | n/a | dev `Command` |
| `Help` | Help menu slot (search field) | Help section | n/a | label-only |
| `Undo` | placement + label "Undo" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `Redo` | placement + label "Redo" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `Cut` | placement + label "Cut" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `Copy` | placement + label "Copy" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `Paste` | placement + label "Paste" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `Delete` | placement + label "Delete" — **dev `Command`** | placement + label | placement | label / dev `Command` |
| `SelectAll` | placement + label "Select All" — **dev `Command`** | placement + label | placement | label / dev `Command` |

**Auto-wired (macOS OS-owned):** `About`, `Settings`, `Services`, `Hide`, `HideOthers`, `ShowAll`,
`Quit`, `Minimize`, `Zoom`, `EnterFullScreen`. **Placement-only (dev supplies `Command`):** the
edit roles `Undo`, `Redo`, `Cut`, `Copy`, `Paste`, `Delete`, `SelectAll`, plus `None`. **Slot
containers:** `ApplicationMenu`, `Window`, `Help` (define a menu section/slot; children supply the
actual items). On Windows/Linux in-app, OS-only roles **no-op unless a `Command` is supplied**;
others render as normal labeled items. Apps probe support via `IsRoleSupported` (see seam).

```csharp
namespace Uno.UI.Xaml.Controls;

public enum NativeMenuItemRole
{
	None,
	ApplicationMenu, About, Settings, Services, Hide, HideOthers, ShowAll, Quit,
	Window, Minimize, Zoom, EnterFullScreen, Help,
	Undo, Redo, Cut, Copy, Paste, Delete, SelectAll,
}
```

### `ToggleType`

| Value | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|
| `None` | Not checkable (default) | plain item | plain action | normal | normal |
| `CheckBox` | Independent on/off | `state` on/off checkmark | `.on`/`.off` | `toggle-type=checkmark` | check |
| `Radio` | Mutually exclusive within `GroupName` | framework-coordinated exclusivity + state | inline single-selection section | `toggle-type=radio` | grouped radio |

```csharp
namespace Uno.UI.Xaml.Controls;

public enum ToggleType
{
	None,
	CheckBox,
	Radio,
}
```

## Attachment API

`Window` and `Application` are **not** `DependencyObject`s in WinUI/Uno
([Window.cs](../../src/Uno.UI/UI/Xaml/Window/Window.cs),
[Application.cs](../../src/Uno.UI/UI/Xaml/Application.cs)) — so an Avalonia-style attached
property on `Window`/`Application` does **not** port. Attachment is therefore **code-first
setters**, with associations stored in a `ConditionalWeakTable` side-table (weak-keyed on the
`Window`, so menus are released when the window is collected).

| API | Scope | Storage | Meaning |
|---|---|---|---|
| `NativeMenu.SetMenu(Window window, NativeMenu? menu)` | window | `ConditionalWeakTable<Window, NativeMenu>` | Assign (or clear with `null`) the menu for a specific window. |
| `NativeMenu.GetMenu(Window window)` → `NativeMenu?` | window | same | Read the window's assigned menu. |
| `NativeMenu.SetApplicationMenu(NativeMenu? menu)` | app | static field | Assign (or clear) the app-wide fallback menu. |
| `NativeMenu.GetApplicationMenu()` → `NativeMenu?` | app | static field | Read the app-wide menu. |

```csharp
namespace Uno.UI.Xaml.Controls;

public partial class NativeMenu
{
	public static void SetMenu(Window window, NativeMenu? menu);
	public static NativeMenu? GetMenu(Window window);

	public static void SetApplicationMenu(NativeMenu? menu);
	public static NativeMenu? GetApplicationMenu();
}
```

The **tree itself can be declared in XAML** as a resource/object (via the `Items` content
property), then assigned in code:

```xml
<NativeMenu x:Key="MainMenu" xmlns="using:Uno.UI.Xaml.Controls">
	<NativeMenuItem Text="File">
		<NativeMenuItem.SubMenu>
			<NativeMenu>
				<NativeMenuItem Text="New" Command="{x:Bind NewCommand}" />
				<NativeMenuItemSeparator />
				<NativeMenuItem Role="Quit" />
			</NativeMenu>
		</NativeMenuItem.SubMenu>
	</NativeMenuItem>
</NativeMenu>
```

```csharp
var menu = (NativeMenu)Resources["MainMenu"];
NativeMenu.SetMenu(MainWindow, menu);   // window-scoped
// or NativeMenu.SetApplicationMenu(menu); // app-wide
```

## Toolkit layer — `Uno.Toolkit.UI`

A separate follow-up deliverable in `unoplatform/uno.toolkit.ui` providing the declarative
control. It **depends on** the core seam; the core never depends on it.

### `AppMenuBar : MenuBar`

| Aspect | Behavior |
|---|---|
| Base | `MenuBar` (reuses existing `MenuBarItem` / `MenuFlyoutItem` markup and Skia rendering). |
| Windows / Linux-no-native | Renders as a **real in-app `MenuBar`** (normal visual footprint). |
| Apple (macOS / iPadOS) | Reads its `MenuBarItem` / `MenuFlyout*` content, **translates** to a `NativeMenu`, calls `NativeMenu.SetMenu(window, translatedMenu)` (or `SetApplicationMenu`), then **collapses to zero in-app footprint**. |
| Role declaration | OS role via the Toolkit attached property `AppMenu.Role` → mapped to `NativeMenuItem.Role`. |
| Sync | Re-translates and re-projects when its declarative content changes (same dirty/rebuild flow). |

### `AppMenu` attached property (Toolkit)

| API | Type | Applies to | Meaning |
|---|---|---|---|
| `AppMenu.SetRole(DependencyObject, NativeMenuItemRole)` | `NativeMenuItemRole` | `MenuBarItem` / `MenuFlyoutItem` | Marks the OS standard-slot role; copied to the translated `NativeMenuItem.Role`. |
| `AppMenu.GetRole(DependencyObject)` → `NativeMenuItemRole` | — | — | Read the role. |

### `MenuFlyoutItem` → `NativeMenuItem` translation map

| Source (`MenuBar` / `MenuFlyout*`) | Target (`NativeMenu*`) |
|---|---|
| `MenuBarItem` | `NativeMenuItem` with a `SubMenu` (`Text` ← `Title`) |
| `MenuFlyoutSubItem` | `NativeMenuItem` with a `SubMenu` |
| `MenuFlyoutItem` | `NativeMenuItem` (leaf) |
| `ToggleMenuFlyoutItem` | `NativeMenuItem`, `ToggleType=CheckBox`, `IsChecked` ← `IsChecked` |
| `RadioMenuFlyoutItem` | `NativeMenuItem`, `ToggleType=Radio`, `GroupName` ← `GroupName`, `IsChecked` ← `IsChecked` |
| `MenuFlyoutSeparator` | `NativeMenuItemSeparator` |
| `.Text` | `.Text` |
| `.Icon` (`IconElement`) | `.Icon` (`IconSource`, best-effort) |
| `.Command` / `.CommandParameter` | `.Command` / `.CommandParameter` |
| `.KeyboardAccelerators` | `.KeyboardAccelerators` (literal, no remap) |
| `.IsEnabled` | `.IsEnabled` |
| `Visibility` | `.IsVisible` (`Visible` → `true`) |
| `AppMenu.Role` (attached) | `.Role` |

## Projection seam

`INativeMenuExtension` is the per-host projection contract (core in `Uno.UI`, resolved via
`ApiExtensibility.CreateInstance<INativeMenuExtension>()`, registered by each Skia host). It
exposes `SetMenu(scope, NativeMenu?)`, an `IsExported` indicator (is a native menu actually
shown?), and the capability probes `IsSupported` / `IsRoleSupported`. Full contract, host
implementations (Skia.MacOS / Skia.AppleUIKit / Skia.X11-post-v1 / Win32-noop), and the
`NativeMenu.IsSupported` / `IsRoleSupported` public probe surface are in
[contracts/INativeMenuExtension.md](./contracts/INativeMenuExtension.md).

## Shortcut modifier mapping

Literal `KeyboardAccelerator.Modifiers` reused as-is (no menu-only Ctrl→Cmd remap — it would be
incoherent with live key events). This matches Uno's existing macOS input mapping
([UNOWindow.m:878-889](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOWindow.m)).

| `VirtualKeyModifiers` | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|
| `Windows` | Command (⌘) | Command (⌘) | Super | Win |
| `Control` | Control (⌃) | Control (⌃) | Ctrl | Ctrl |
| `Menu` | Option/Alt (⌥) | Option/Alt (⌥) | Alt | Alt |
| `Shift` | Shift (⇧) | Shift (⇧) | Shift | Shift |

Cross-platform "Cmd-on-Mac / Ctrl-on-Win" is achieved via per-platform markup, or an optional
future `Primary`-modifier sugar (non-core follow-up; **not v1**).

## State & lifecycle

### Dirty → coalesce → full rebuild

```
DP change callback  ─┐
INotifyCollectionChanged on Items ─┤→ mark affected root menu DIRTY
Command.CanExecuteChanged ─┘            (propagate via Parent to the projected root)
                                     │
                                     ▼
                          coalesce on the UI-thread dispatcher
                          (one rebuild per frame, dedup multiple changes)
                                     │
                                     ▼
                          INativeMenuExtension.SetMenu(scope, root)
                          → FULL rebuild of that menu on the native main thread
                          (no incremental native diffing — iPadOS & Linux are rebuild-only)
```

- **Threading:** native menu APIs are main-thread; all mutations marshal to the UI thread and
  the coalesced rebuild posts to the dispatcher.
- **Reset strategy:** any collection change (including `Reset`) triggers a full re-projection of
  the affected menu (Avalonia-style), keeping all four backends on one simple code path.

### `Opening` — lazy / just-in-time population

`NativeMenu.Opening` fires immediately before the menu/submenu is shown, letting authors build
or refresh children on demand. Maps to `NSMenuDelegate.menuNeedsUpdate:` (macOS), DBus
`AboutToShow` (Linux), and a `buildMenu(with:)` rebuild pass (iPadOS). `Closed` fires on
dismissal. x:Bind / MVVM flows naturally through the observable model.

### Enablement (pushed, authoritative)

```
EffectiveEnabled = IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)
```

The model **observes `CanExecuteChanged`** and re-pushes. macOS sets `NSMenu.autoenablesItems =
NO` so the pushed state is authoritative (no AppKit auto-validation). iPadOS uses the `.disabled`
attribute; Linux/Windows push the enabled flag directly.

### Scope state — focused-window-wins + Application fallback

```
effective menu for the system  =  GetMenu(focused/key window)  ??  GetApplicationMenu()
```

| Platform | Mechanism |
|---|---|
| macOS | Swap `NSApp.mainMenu` on `windowDidBecomeKey`, restore on resign (per-window supported v1; multi-window exists). |
| iPadOS | `UIMenuSystem.main.setNeedsRebuild()` on scene-focus change; `buildMenu(with:)` reads the focused scene menu. **v1 = app-wide only** (Uno AppleUIKit is single-scene — [NativeWindowFactoryExtension.cs:16](../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/NativeWindowFactoryExtension.cs)); per-scene override deferred until multi-scene exists. |
| Linux | Per-window is the native DBusMenu model. |
| Windows | Per-window == per in-app control. |

### macOS framework-guaranteed App menu

The framework **always** ensures the bold app-name menu exists with at least **Quit (Cmd+Q)** and
**Hide**; developer top-level menus (File/Edit/…) follow it. This replaces/extends the existing
bootstrap `NSMenu` ([UNOApplication.m:95-124](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m),
Quit Cmd+Q / Close Window Cmd+W).

To customize, the developer declares a top-level `NativeMenuItem` with `Role=ApplicationMenu`;
its children (**About**, **Settings**, etc.) **merge** into the app menu. No auto-injection of
`Services` / `HideOthers` unless explicitly declared (consistent with thin roles).

### Tray-readiness (not built v1)

`NativeMenu` is intentionally scope-agnostic (a plain item tree, not bound to a window's visual
tree), so a future system-tray / notification-area menu can reuse the same model and projection
seam without API changes.
