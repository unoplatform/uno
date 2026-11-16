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
├── NativeMenu                              (a menu / submenu container — NOT an item)
│     ├── Items : IList<NativeMenuItemBase> (XAML content property, observable)
│     └── Parent : NativeMenuItem?          (the item that owns this as its SubMenu)
└── NativeMenuItemBase                      (abstract; Parent back-reference)
      ├── NativeMenuItem                    (a leaf or submenu-owning command)
      │     └── SubMenu : NativeMenu?       (nesting → submenu)
      └── NativeMenuItemSeparator           (a divider)
```

- `NativeMenu` and `NativeMenuItem` are **not** `FrameworkElement`s — no visual tree, no
  template, no measure/arrange. They are `DependencyObject`s so properties are
  `DependencyProperty`-backed (change callbacks drive the dirty/rebuild flow) and so XAML can
  declare the tree as a resource/object graph.
- Nesting: a `NativeMenu`'s `Items` hold `NativeMenuItemBase`. A `NativeMenuItem` becomes a
  submenu parent by setting `SubMenu` to a child `NativeMenu`. `Parent` is maintained by the
  owning collection/property setter for upward dirty propagation.

### `NativeMenu` is deliberately **not** a `NativeMenuItemBase`

A menu is a *container*, not a thing that can sit in a list of items. Keeping the two branches
separate is a load-bearing decision, not an accident of layout:

```csharp
parentMenu.Items.Add(new NativeMenu());   // ✗ does not compile — NativeMenu is not a
                                          //   NativeMenuItemBase. Nest via NativeMenuItem.SubMenu.
```

Were `NativeMenu` an item, that line would compile and produce an item with no label, no
command and no defined projection — each host would then invent its own answer (drop it? splice
its children inline? render an empty row?) and the four backends would diverge. The type system
removes the question instead of documenting an answer to it. This mirrors Avalonia, whose
`NativeMenu` likewise derives from `AvaloniaObject` rather than `NativeMenuItemBase`
([`NativeMenu.cs`](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Controls/NativeMenu.cs)).

**Grouping is a separate feature, not an emergent one.** If a future version wants
inline/sectioned groups (a run of items fenced by separators, as in AppKit inline sections,
`UIMenu.Options.displayInline` on Apple, or Flutter's dedicated `PlatformMenuItemGroup`), that
arrives as an explicit `NativeMenuItemGroup : NativeMenuItemBase` with its own platform-support
table — never as an inferred meaning for a bare container. Adding a new sealed item subtype is
additive and non-breaking, so deferring it costs nothing.

### One parent per item — and no cycles

An item belongs to exactly one menu. Adding an item that already has a `Parent` to a second
`NativeMenu` (or assigning one `NativeMenu` as the `SubMenu` of two different items) throws
`InvalidOperationException` at mutation time rather than silently corrupting the back-references
that dirty propagation walks. Re-parent by removing from the first owner first. Avalonia enforces
the same invariant through a collection validator, having found that silent aliasing produces
menus that update in one place and not the other.

**Single-parent is not sufficient on its own — cycles must be rejected separately.** This
compiles, gives every node exactly one parent, and so passes the aliasing check:

```csharp
menuA.Items.Add(itemA);
itemA.SubMenu = menuA;      // ✗ must throw: menuA is now its own descendant
```

Nothing is aliased, yet the upward walk described above — "until it reaches a menu with no
`Parent`" — never terminates, and projection recurses until the UI thread dies. The same
mutation point that enforces single-parent MUST therefore also walk `Parent` from the prospective
new owner and throw `InvalidOperationException` if the item being attached is already an ancestor
of it. The walk is bounded by the existing depth, so the check is cheap.

### `Items` is a `DependencyObjectCollection`

`NativeMenu.Items` MUST be backed by
[`DependencyObjectCollection<NativeMenuItemBase>`](../../src/Uno.UI/UI/Xaml/DependencyObjectCollection.cs),
not a hand-rolled observable list. That type already calls `SetParent` on every element it holds
([`DependencyObjectCollection.cs:105-116`](../../src/Uno.UI/UI/Xaml/DependencyObjectCollection.cs))
precisely to keep **inheritance context** flowing.

This is load-bearing, not a convenience: without a real parent chain a `DependencyObject` receives
no inherited `DataContext`, so `{Binding}` on a `NativeMenuItem.Command` silently never resolves —
which would quietly break the MVVM story this model is sold on, and the XAML sample below with it.
The `Parent` back-references described above are the *menu-tree* relationships used for dirty
propagation; they are additional to, and not a replacement for, the framework parent that
`DependencyObjectCollection` maintains.

## Core types — `Uno.UI.Xaml.Controls`

### `NativeMenuItemBase`

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Parent` | `NativeMenu?` | `null` | Back-reference to the owning `NativeMenu`, set by that menu's `Items` collection; used to propagate "dirty" up to the root menu being projected. Non-`null` exactly while the item is in a menu — see [One parent per item](#one-parent-per-item). |

```csharp
namespace Uno.UI.Xaml.Controls;

public abstract partial class NativeMenuItemBase : DependencyObject
{
	// Internal ctor: the hierarchy is closed. A third-party subtype would reach every host as
	// an unknown node — exactly the divergence the NativeMenu/NativeMenuItemBase split removes —
	// and would void the "adding a sealed subtype later is additive" guarantee above.
	internal NativeMenuItemBase() { }

	internal NativeMenu? Parent { get; set; }
}
```

> **Every `[GeneratedDependencyProperty]` below is shown with its
> `public static DependencyProperty XProperty { get; } = CreateXProperty();` assignment.** Per
> `.claude/rules/dependency-properties.md` that assignment is mandatory — omitting it mis-generates
> silently — and defaults are written as `default(T)` rather than bare literals. Implementers copy
> these blocks, so they are written in the form that compiles.

The upward walk therefore alternates between the two branches — item → owning `NativeMenu`
(`NativeMenuItemBase.Parent`) → owning `NativeMenuItem` (`NativeMenu.Parent`) → … — until it
reaches a menu with no `Parent`, which is the root that was handed to a scope.

### `NativeMenu`

A container of items; also serves as a submenu and as the root assigned to a scope.

| Member | Type | Default | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|---|---|
| `Items` | `IList<NativeMenuItemBase>` | empty observable list | Ordered child items. **XAML content property.** Implements `INotifyCollectionChanged`; add/remove/move/reset marks the menu dirty. | `NSMenu` item array | menu children for `UIMenu` | DBusMenu child layout | in-app `MenuBarItem` children |
| `Parent` | `NativeMenuItem?` | `null` | Back-reference to the `NativeMenuItem` whose `SubMenu` this is; `null` for a root menu handed to a scope. |
| `Title` | `string?` | `null` | Optional title for the menu when used as a submenu/top-level header (the owning `NativeMenuItem.Text` usually supplies this; `Title` is the standalone form). | `NSMenu.title` | `UIMenu.title` | submenu label | header text |
| `NeedsUpdate` | `EventHandler<NativeMenuNeedsUpdateEventArgs>` | — | **The just-in-time population hook.** Raised before the menu is shown, at the point where mutating `Items` is still safe and will be picked up by the in-flight build. | `NSMenuDelegate.menuNeedsUpdate:` | `buildMenu(with:)` pass | DBus `AboutToShow` | before flyout opens |
| `Opening` | `EventHandler<NativeMenuOpeningEventArgs>` | — | Notification that the menu is about to appear, after its content is settled. **Do not mutate the menu here** — use `NeedsUpdate`. | `NSMenuDelegate.menuWillOpen:` | (best-effort) | DBus opened | flyout `Opening` |
| `Closed` | `EventHandler<NativeMenuClosedEventArgs>` | — | Raised after the menu is dismissed. | `NSMenuDelegate.menuDidClose:` | (best-effort) | DBus closed | flyout `Closed` |

```csharp
namespace Uno.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Items))]
public partial class NativeMenu : DependencyObject
{
	public IList<NativeMenuItemBase> Items { get; } // observable; INotifyCollectionChanged

	internal NativeMenuItem? Parent { get; set; }

	[GeneratedDependencyProperty(DefaultValue = null)]
	public string? Title { get; set; }
	public static DependencyProperty TitleProperty { get; } = CreateTitleProperty();

	public event EventHandler<NativeMenuNeedsUpdateEventArgs>? NeedsUpdate; // mutate here
	public event EventHandler<NativeMenuOpeningEventArgs>? Opening;         // notify only
	public event EventHandler<NativeMenuClosedEventArgs>? Closed;
}

public sealed partial class NativeMenuNeedsUpdateEventArgs : EventArgs;
public sealed partial class NativeMenuOpeningEventArgs : EventArgs;
public sealed partial class NativeMenuClosedEventArgs : EventArgs;
```

**Why `NeedsUpdate` and `Opening` are separate events.** They fire at different points in the
native open sequence and only the first one can safely change the menu. AppKit draws the
distinction directly (`menuNeedsUpdate:` is the documented place to add or remove items;
`menuWillOpen:` runs once layout is committed), and DBusMenu's `AboutToShow` is a request for
content that expects a "did anything change?" answer. Collapsing both into one event invites
authors to repopulate `Items` from the notification-only phase, which re-enters the
dirty→coalesce→rebuild pipeline (see [State & lifecycle](#state--lifecycle)) from inside the
platform's menu-tracking run loop. Avalonia ships the same three-event split — `NeedsUpdate`,
`Opening`, `Closed` — with the same "do not update the menu in `Opening`" guidance.

**Observability:** `Items` raises `INotifyCollectionChanged`; every `DependencyProperty` on
items raises its change callback. Both feed the same dirty/coalesce/rebuild pipeline (see
[State & lifecycle](#state--lifecycle)).

### `NativeMenuItem`

A single command, a checkable item, or a submenu owner.

| Member | Type | Default | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|---|---|
| `Text` | `string` | `""` | Display label. Mnemonics per platform convention. | `NSMenuItem.title` | `UICommand`/`UIAction.title` | DBus `label` | item text |
| `Icon` | `IconSource?` | `null` | Best-effort icon. Bitmap/Image → PNG bytes; Symbol/Font → rendered glyph; optional SF-Symbol name passthrough on Apple. **Translation result is cached on (`IconSource`, scale) and computed when `Icon` changes, never per rebuild** — re-encoding every icon in the tree on each coalesced tick would make an `IsEnabled` toggle arbitrarily expensive. | `NSMenuItem.image` (`NSImage`) | `UIImage` (SF Symbol preferred) | icon-name / icon-data (partial) | in-app `IconElement` |
| `Command` | `ICommand?` | `null` | Invoked on activation. `XamlUICommand`/`StandardUICommand` auto-populate `Text`/`Icon`/`KeyboardAccelerators` via `CommandingHelpers`. | target/action invokes callback | `UIAction` handler | DBus `clicked` | `Click`/command |
| `CommandParameter` | `object?` | `null` | Passed to `Command.Execute` / `CanExecute`. | — | — | — | — |
| `KeyboardAccelerators` | `IList<KeyboardAccelerator>` | empty | Literal accelerators (modifiers reused as-is — no Ctrl→Cmd remap; see [Shortcuts](#shortcut-modifier-mapping)). | `keyEquivalent` + `keyEquivalentModifierMask` | `UIKeyCommand` | accelerator (partial) | in-app accelerator |
| `IsEnabled` | `bool` | `true` | Author-set enabled flag. Effective-enabled = `IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)` — **pushed**, authoritative. | `NSMenuItem.enabled` (`autoenablesItems=NO`) | `UIAction` `.disabled` attribute | DBus `enabled` | in-app `IsEnabled` |
| `IsChecked` | `bool` | `false` | Check/toggle state (with `ToggleType`). | `NSMenuItem.state` on/off | `.on`/`.off`/`.mixed` | toggle-state | in-app check |
| `ToggleType` | `NativeMenuItemToggleType` | `None` | None / CheckBox / Radio (mirrors `RadioMenuFlyoutItem`). | framework-coordinated radio | inline single-selection | `toggle-type` checkmark/radio | in-app toggle |
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

	[GeneratedDependencyProperty(DefaultValue = NativeMenuItemToggleType.None)]
	public NativeMenuItemToggleType ToggleType { get; set; }
	public static DependencyProperty ToggleTypeProperty { get; } = CreateToggleTypeProperty();

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
| `Settings` | Preferences slot + standard label (Cmd+,) — **dev `Command`** | Settings slot | n/a | dev `Command` |
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

**Auto-wired (macOS OS-owned):** `About`, `Services`, `Hide`, `HideOthers`, `ShowAll`,
`Quit`, `Minimize`, `Zoom`, `EnterFullScreen`. **Placement-only (dev supplies `Command`):** the
edit roles `Undo`, `Redo`, `Cut`, `Copy`, `Paste`, `Delete`, `SelectAll`, plus `Settings` and
`None`. `Settings` is placement-only because AppKit ships no default implementation of
`orderFrontPreferencesPanel:` — the role reserves the Preferences slot, its standard label and
Cmd+, but the app must supply the `Command`. **Slot containers:** `ApplicationMenu`, `Window`,
`Help` (define a menu section/slot; children supply the actual items). On Windows/Linux in-app, OS-only roles **no-op unless a `Command` is supplied**;
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

### `NativeMenuItemToggleType`

| Value | Meaning | macOS | iPadOS | Linux | Windows |
|---|---|---|---|---|---|
| `None` | Not checkable (default) | plain item | plain action | normal | normal |
| `CheckBox` | Independent on/off | `state` on/off checkmark | `.on`/`.off` | `toggle-type=checkmark` | check |
| `Radio` | Mutually exclusive within `GroupName` | framework-coordinated exclusivity + state | inline single-selection section | `toggle-type=radio` | grouped radio |

```csharp
namespace Uno.UI.Xaml.Controls;

public enum NativeMenuItemToggleType
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

### Public capability surface

`INativeMenuExtension` is `internal`, matching the other extension seams. Everything a caller
outside `Uno.UI` needs is therefore re-exposed as **public statics on `NativeMenu`**, which
forward to the resolved extension and degrade to `false` when no host is registered:

| API | Type | Meaning |
|---|---|---|
| `NativeMenu.IsSupported` | `bool` | Will a menu assigned right now actually be projected on this platform? See [capability semantics](./contracts/INativeMenuExtension.md#6-capability-semantics). |
| `NativeMenu.IsRoleSupported(NativeMenuItemRole role)` | `bool` | Does this role map to a real OS slot here? |
| `NativeMenu.IsExported` | `bool` | Is a native menu currently on screen for the app-wide scope? |
| `NativeMenu.IsExported(Window)` | `bool` | Same question for one window. Required because export is genuinely per-window: on Linux the DBusMenu registrar is keyed by X11 window XID, and on macOS the bar reflects the key window. A process-global answer is wrong as soon as two windows differ — and v1 supports macOS multi-window. |
| `NativeMenu.IsExportedChanged` | `EventHandler<NativeMenuExportedChangedEventArgs>?` | Raised when export state flips; the args carry the affected `Window` (`null` = app scope). |

```csharp
namespace Uno.UI.Xaml.Controls;

public partial class NativeMenu
{
	public static void SetMenu(Window window, NativeMenu? menu);
	public static NativeMenu? GetMenu(Window window);

	public static void SetApplicationMenu(NativeMenu? menu);
	public static NativeMenu? GetApplicationMenu();

	public static bool IsSupported { get; }
	public static bool IsRoleSupported(NativeMenuItemRole role);

	public static bool IsExported { get; }
	public static bool IsExported(Window window);

	// Explicit add/remove, NOT a field-like event: a field-like declaration would compile while
	// forwarding to nothing (never raised), and would root every subscriber for the process
	// lifetime. The accessors attach to / detach from the resolved extension and hold subscribers
	// weakly, so an unloaded AppMenuBar is collectable.
	public static event EventHandler<NativeMenuExportedChangedEventArgs>? IsExportedChanged;
}

public sealed partial class NativeMenuExportedChangedEventArgs : EventArgs
{
	/// <summary>The affected window, or null when the application-wide scope changed.</summary>
	public Window? Window { get; }
}
```

`IsExported` and `IsExportedChanged` are **required public surface, not conveniences.** The
Toolkit `AppMenuBar` decides between rendering in-app and collapsing to zero footprint purely on
`IsExported`, and it ships from `Uno.Toolkit.UI` — a different assembly in a different
repository, which cannot see an `internal` interface. The alternatives are an
`[InternalsVisibleTo]` coupling (which the `ApiExtensibility` design exists specifically to
avoid) or polling on a timer. Both are worse, so the bridge is part of the v1 contract.

The **tree itself can be declared in XAML** as a resource/object (via the `Items` content
property), then assigned in code:

```xml
<NativeMenu x:Key="MainMenu" xmlns="using:Uno.UI.Xaml.Controls">
	<NativeMenuItem Text="File">
		<NativeMenuItem.SubMenu>
			<NativeMenu>
				<NativeMenuItem Text="New" Command="{Binding New, Source={StaticResource Commands}}" />
				<NativeMenuItemSeparator />
				<NativeMenuItem Role="Quit" />
			</NativeMenu>
		</NativeMenuItem.SubMenu>
	</NativeMenuItem>
</NativeMenu>
```

> **No `{x:Bind}` and no `x:Name` in a resource dictionary.** `x:Bind` compiles against a root
> element's code-behind, which a `ResourceDictionary` does not have, and `x:Name` emits no field
> there. Reference commands through `{StaticResource}`/`{Binding Source=...}` as above, or attach
> them in code after pulling the resource. Plain `{Binding}` against an inherited `DataContext`
> works once `Items` is a `DependencyObjectCollection`.
>
> **A menu declared this way is a shared instance.** `{StaticResource}` hands back the same object
> every time, and the one-parent invariant means the second attach throws. Assign a resource menu
> to exactly one scope, or declare it `x:Shared="False"` to get a fresh instance per lookup.

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

`INativeMenuExtension` is the `internal` per-host projection contract (core in `Uno.UI`, resolved
via `ApiExtensibility.CreateInstance<INativeMenuExtension>()`, registered by each Skia host).
The public face of it is the four statics in
[Public capability surface](#public-capability-surface) above.

The seam's own members, its host implementations (Skia.MacOS / Skia.AppleUIKit / Skia.X11-post-v1
/ Win32-noop) and the capability semantics are specified once, in
[contracts/INativeMenuExtension.md](./contracts/INativeMenuExtension.md) — deliberately not
restated here, because the previous restatement of the member list drifted out of date.

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
- **Reset strategy:** any *structural* change (including `Reset`) triggers a full re-projection of
  the affected menu, keeping all four backends on one simple code path.

#### Structure-dirty vs state-dirty

Not every change deserves a rebuild, and treating them alike is the difference between a menu that
costs nothing and one that pegs the UI thread:

| Dirt | Trigger | Projection |
|---|---|---|
| **Structure** | `Items` add/remove/move/reset, `SubMenu` assigned, `IsVisible`, `Text`, `Icon`, `Role`, accelerators | full coalesced rebuild of the affected menu |
| **State** | `IsEnabled`, `IsChecked`, and every `Command.CanExecuteChanged` | in-place push to the existing native items — **never** a rebuild |

`CanExecuteChanged` is the reason this split is mandatory rather than an optimisation. A
`RelayCommand` that re-raises it on every keystroke or selection change is entirely ordinary, and
routing that into the structural path means a whole-tree walk plus a full native reconstruction
**every frame, indefinitely, on the UI thread** — for a menu the user is not even looking at.
State-only dirt maps to `NSMenuItem.enabled`/`.state` on macOS, `setNeedsRevalidate()` on iPadOS
and a property update on DBusMenu, all of which exist precisely for this.

Two further requirements on the pipeline: a change that does not alter the effective value MUST
short-circuit before marking anything dirty, and dirt MUST be scoped to the affected menu rather
than always escalating to the projected root.

### `NeedsUpdate` — lazy / just-in-time population

`NativeMenu.NeedsUpdate` fires immediately before the menu/submenu is shown, letting authors
build or refresh children on demand. Maps to `NSMenuDelegate.menuNeedsUpdate:` (macOS), DBus
`AboutToShow` (Linux), and a `buildMenu(with:)` rebuild pass (iPadOS). `Opening` then fires as a
notification once the content is settled, and `Closed` on dismissal. x:Bind / MVVM flows
naturally through the observable model.

Mutations made from a `NeedsUpdate` handler are applied **synchronously to the build already in
flight** — they do not schedule a second coalesced rebuild, so populating a submenu here costs
one projection, not two. Mutations from `Opening` or `Closed` are ordinary model changes: they
go through the normal dirty→coalesce path and therefore land on the *next* rebuild, which is
why they must not be used for population.

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
| iPadOS | `UIMainMenuSystem` (iOS/iPadOS 26) `setNeedsRebuild()` on scene-focus change, falling back to `UIMenuSystem.main` on earlier OS; `buildMenu(with:)` reads the focused scene menu. This drives the **always-available iPadOS 26 menu bar** (swipe from top; earlier OS = transient hardware-keyboard menu). **v1 = app-wide only** (Uno AppleUIKit is single-scene — [NativeWindowFactoryExtension.cs:16](../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/NativeWindowFactoryExtension.cs)); per-scene override deferred until multi-scene exists. |
| Linux | Per-window is the native DBusMenu model. |
| Windows | Per-window == per in-app control. |

### macOS framework-guaranteed App menu

The framework **always** ensures the bold app-name menu exists with at least **Quit (Cmd+Q)** and
**Hide**; developer top-level menus (File/Edit/…) follow it. This replaces/extends the existing
bootstrap `NSMenu` ([UNOApplication.m:156-185](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m),
Quit Cmd+Q / Close Window Cmd+W).

To customize, the developer declares a top-level `NativeMenuItem` with `Role=ApplicationMenu`;
its children (**About**, **Settings**, etc.) **merge** into the app menu. No auto-injection of
`Services` / `HideOthers` unless explicitly declared (consistent with thin roles).

### Tray-readiness (not built v1)

`NativeMenu` is intentionally scope-agnostic (a plain item tree, not bound to a window's visual
tree), so a future system-tray / notification-area menu can reuse the same model and projection
seam without API changes.
