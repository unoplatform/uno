# Feature Specification: Cross-Platform Native OS Menu Integration

**Feature Branch**: `047-native-os-menu`
**Created**: 2026-06-30
**Status**: Draft
**Input**: "Cross-platform apps should feel native. Desktop and tablet operating systems
expose a system menu (the macOS top-of-screen menu bar, the always-available iPadOS 26
menu bar revealed by swiping from the top of the screen, the Linux global menu on some
desktop environments). Uno Platform has no native OS
menu integration today. Provide a unified API to define application- and window-scoped
menus that project to each platform's native menu system, with an in-app fallback control
where there is none." (issue intent, paraphrased neutrally)

## Context

Applications built with Uno Platform draw their own UI on every target, but on desktop and
tablet operating systems the *menu* is special: it lives outside the app's render surface
(the macOS bar at the top of the screen, the always-available iPadOS 26 bar revealed by
swiping down from the top of the screen, the Linux global menu on supporting desktop
environments). To feel native, a cross-platform
app must populate that OS-owned menu, not draw its own bar in the window. Uno Platform offers
no way to do this today. This feature introduces a lightweight, code-first menu model
(`NativeMenu` / `NativeMenuItem`) and a projection seam that maps it onto each platform's
native menu system, falling back to an in-app menu where no native menu exists. The work is
**Apple-first, design-for-all**: v1 ships macOS (`NSMenu`) and iPadOS (`UIMenuBuilder`,
app-wide) on the Skia targets only, with Linux (DBusMenu) and Windows as designed-for
extension points delivered post-v1. The verified Uno integration points and the
cross-framework comparison that informed these decisions are recorded in
[research.md](./research.md); the model shape is in [data-model.md](./data-model.md) and the
projection contract in [contracts/INativeMenuExtension.md](./contracts/INativeMenuExtension.md).

The work is split across **two repositories**, with a strict one-way dependency:

- **Core** (this feature) — the non-UI `NativeMenu` model, the code-first attachment setters,
  the `INativeMenuExtension` projection seam, and the per-host native implementations
  (Skia.MacOS, Skia.AppleUIKit). Namespace `Uno.UI.Xaml.Controls`.
- **Toolkit** (a separate follow-up feature) — the declarative `AppMenuBar : MenuBar`
  control. It consumes the core seam; the core never depends on the Toolkit. Namespace
  `Uno.Toolkit.UI`.

## Terminology

- **Native menu** — a menu owned and rendered by the operating system (macOS `NSMenu`,
  iPadOS `UIMenu` tree, Linux DBusMenu), as opposed to a control Uno draws itself.
- **App-wide menu** — a single menu that represents the whole application (macOS
  `NSApp.mainMenu`, iPadOS `buildMenu`). Set via `SetApplicationMenu`.
- **Window-scoped menu** — a menu associated with one window/scene that the OS shows when
  that window is focused. Set via `SetMenu(window, …)`.
- **Projection seam** — the `INativeMenuExtension` abstraction (resolved per Skia host via
  `ApiExtensibility`) that translates the `NativeMenu` model into the host's native menu
  system. Each host registers one concrete implementation.
- **Role** — a thin slot-marker (`NativeMenuItemRole`) that tells the OS *where* a standard
  item belongs (e.g. the App menu's Quit slot, the Edit menu's Copy slot) and supplies a
  standard label. OS-owned roles auto-wire to native selectors; all other roles only set
  placement and label — the developer still supplies the `Command`.
- **In-app fallback** — on a target with no native menu (Windows, Wayland/GNOME with no
  global-menu registrar), the declarative `AppMenuBar` renders as a real in-app `MenuBar`
  instead of projecting; the core seam reports `IsExported = false` and the setters no-op.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - An app-wide menu that feels native on macOS (Priority: P1)

A developer building a macOS app declares an application menu with the usual top-level
entries (File, Edit, View, Help) and assigns it once at startup. The familiar bold
app-name menu (with Quit ⌘Q and Hide) appears automatically to the left of the
developer's menus, and the whole bar lives in the macOS menu bar at the top of the screen —
not inside the window.

**Why this priority**: This is the headline scenario and the reason the feature exists —
an app that does not populate the macOS menu bar does not feel like a Mac app. It is the
smallest end-to-end slice that proves the model, the setter, and the macOS projection seam
together.

**Independent Test**: Build a macOS Skia app, construct a `NativeMenu` with File/Edit/Help
top-level items, call `NativeMenu.SetApplicationMenu(menu)`, run the app, and assert (via
the macOS automation/inspection layer) that `NSApp.mainMenu` contains a leading bold
app-name menu with Quit (⌘Q) and Hide, followed by the three developer menus in order.

**Acceptance Scenarios**:

1. **Given** no menu has been set, **When** a macOS Skia app launches, **Then** the
   framework-guaranteed app menu still exists with at least Quit (⌘Q) and Hide, and Quit
   terminates the app via the AppKit selector.
2. **Given** a `NativeMenu` with File/Edit/Help top-level items, **When**
   `SetApplicationMenu(menu)` is called, **Then** the macOS menu bar shows the bold app-name
   menu followed by File, Edit, Help in declaration order.
3. **Given** a top-level `NativeMenuItem` with `Role=ApplicationMenu` and child items
   (About, Settings…), **When** the menu is set, **Then** those children merge into the
   bold app-name menu rather than producing a separate top-level menu.
4. **Given** an item with `Role=Quit` or `Role=Hide` and no `Command`, **When** it is
   activated, **Then** the OS performs the standard action (`terminate:` / `hide:`) with no
   developer code.

---

### User Story 2 - Commands and shortcuts wired through existing Uno vocabulary (Priority: P1)

A developer attaches behavior to menu items using the command/shortcut types they already
use elsewhere in Uno: an `ICommand` (or `XamlUICommand` / `StandardUICommand`) for the
action and `KeyboardAccelerator`s for the shortcut. Selecting the item — or pressing its
shortcut — runs the command, and the item enables/disables itself based on
`CanExecute`.

**Why this priority**: A menu that cannot run anything is a label. Reusing the existing
command and accelerator vocabulary is what makes the menu useful without inventing a new
execution model, and the literal-modifier shortcut rule must be locked in v1 to stay
coherent with Uno's live key-event mapping.

**Independent Test**: Create a `NativeMenuItem` with a `Command` whose `CanExecute` is
toggled by a flag and a `KeyboardAccelerator` (e.g. `S` + `Windows` modifier). Project it,
then assert the native item shows ⌘S on macOS, runs the command on activation and on the
shortcut, and greys out when `CanExecute` returns false.

**Acceptance Scenarios**:

1. **Given** a `NativeMenuItem` with `Command` set, **When** the item is activated (click or
   shortcut), **Then** `Command.Execute(CommandParameter)` runs.
2. **Given** a `NativeMenuItem` with a `Click` handler instead of a command, **When** it is
   activated, **Then** the `Click` event fires (WinUI/Avalonia parity).
3. **Given** a `KeyboardAccelerator` of `Key=S, Modifiers=Windows`, **When** the menu is
   projected on macOS, **Then** the item displays and responds to ⌘S; on a future Windows
   in-app bar the same literal modifiers render as Ctrl+S — no Control→Command remap is
   applied anywhere.
4. **Given** an item whose `Command.CanExecute(CommandParameter)` is `false` (or
   `IsEnabled=false`), **When** the menu is shown, **Then** the native item is disabled, and
   it re-enables when `CanExecuteChanged` next reports `true`.
5. **Given** an item populated from a `StandardUICommand`, **When** projected, **Then** its
   label, icon, and accelerator come from the command (via the existing commanding helpers)
   without the developer setting them explicitly.

---

### User Story 3 - Per-window / per-document menu on macOS (key-window-wins) (Priority: P2)

A multi-window macOS app gives each window (e.g. each open document) its own menu. As the
user switches between windows, the macOS menu bar updates to reflect the focused window's
menu; a window that sets no menu inherits the application-level menu.

**Why this priority**: macOS genuinely supports per-window menus and document-style apps
expect them, but it builds on US1 (the app-wide path must exist first) and is only
meaningful once more than one window is open, so it ranks below the core app-wide slice.

**Independent Test**: Open two macOS windows, call `SetMenu(windowA, menuA)` and leave
windowB without a window menu while an application menu is set. Focus each window in turn
and assert the menu bar reflects `menuA` for windowA and the application menu for windowB.

**Acceptance Scenarios**:

1. **Given** `SetMenu(window, menu)` on a focused window, **When** the window is key,
   **Then** `NSApp.mainMenu` reflects that window's menu.
2. **Given** windowA has a window menu and windowB does not, **When** focus moves from A to
   B, **Then** the bar swaps to windowB's effective menu — the Application-level menu — and
   swaps back when A regains focus.
3. **Given** a window menu is cleared with `SetMenu(window, null)`, **When** that window is
   focused, **Then** the bar falls back to the Application-level menu.

---

### User Story 4 - Declarative `AppMenuBar` that is native on Apple, in-app elsewhere (Priority: P2)

A developer authors a single declarative `AppMenuBar` in XAML (a `MenuBar` subclass in the
Toolkit) with `MenuBarItem`/`MenuFlyoutItem` content. On macOS/iPadOS it projects to the
native menu and leaves no in-app footprint; on Windows (and Linux without a global menu) the
same markup renders as a real in-app `MenuBar`. OS slots are marked with the Toolkit
`AppMenu.Role` attached property.

**Why this priority**: This is the ergonomic, markup-first surface most developers will
actually use, and it exercises the whole core seam end-to-end (translation + projection +
collapse). It ranks P2 because it lives in the Toolkit and depends on the core model and
seam shipping first; the core is independently valuable without it.

**Independent Test**: Author one `AppMenuBar` XAML with File/Edit items and an item carrying
`AppMenu.Role="Quit"`. Run it on macOS and assert it projects to `NSApp.mainMenu` with zero
in-app bar height; run it on Windows and assert a real `MenuBar` renders and the Quit item
behaves as a normal labeled item (no-op unless a command is supplied).

**Acceptance Scenarios**:

1. **Given** an `AppMenuBar` on macOS/iPadOS, **When** the page loads, **Then** its content
   is translated to a `NativeMenu`, projected via `NativeMenu.SetMenu(window, …)`, and the
   control collapses to zero in-app footprint.
2. **Given** the same `AppMenuBar` on Windows, **When** the page loads, **Then** it renders
   as a normal in-app `MenuBar` with the declared items.
3. **Given** an item with `AppMenu.Role="Quit"`, **When** projected on macOS, **Then** it
   maps to `NativeMenuItem.Role=Quit` and auto-wires to terminate; on Windows it renders as
   a normal labeled item that no-ops unless a `Command` is supplied.
4. **Given** an `AppMenuBar` on a Linux desktop with no global-menu registrar, **When** the
   page loads, **Then** it falls back to the in-app `MenuBar`.

---

### User Story 5 - Dynamic updates and lazy submenu population (Priority: P3)

A developer mutates the menu at runtime — adds or removes items, toggles `IsEnabled`,
flips `IsChecked`, changes `Text` — and the native menu reflects the change. For
expensive or context-dependent submenus (e.g. a Recent Files list), the developer fills the
submenu just-in-time in response to an `Opening` event.

**Why this priority**: Live menus and just-in-time population matter for real apps but are an
enhancement over a correct static menu; they ride on the observable model that the earlier
stories already establish.

**Independent Test**: Bind a `NativeMenuItem.IsChecked` to a view-model property, toggle it,
and assert the native item's check state updates after one coalesced rebuild. Separately,
subscribe to `Opening`, populate `SubMenu.Items` in the handler, open the submenu, and assert
the freshly added items appear.

**Acceptance Scenarios**:

1. **Given** a projected menu, **When** any model property changes or an item is added/removed
   from `Items`, **Then** the affected menu is marked dirty, coalesced on the dispatcher, and
   re-projected via a full rebuild of that menu.
2. **Given** a submenu with an `Opening` handler, **When** the user opens it, **Then**
   `Opening` fires (mapped to `menuNeedsUpdate` / iPadOS rebuild) before the submenu is shown,
   and items added in the handler appear.
3. **Given** several rapid mutations within one dispatcher tick, **When** they are processed,
   **Then** they coalesce into a single native rebuild (no per-change native churn).
4. **Given** a radio group (`ToggleType=Radio` + `GroupName`), **When** one item is checked,
   **Then** the others in the group uncheck (framework-coordinated on macOS).

---

### User Story 6 - First-class app-wide menu on the always-available iPadOS 26 menu bar (Priority: P2)

A user on iPadOS 26 swipes down from the top of the screen (or moves the pointer to the top,
or presses Globe+M) and sees the app's menu in the always-available, macOS-like iPadOS menu
bar — no hardware keyboard required — with the developer's items and working keyboard
shortcuts. The bar lists all of the app's commands, including those that are currently
unavailable, so the user can discover what the app can do.

**Why this priority**: iPadOS 26 brings the macOS menu bar to iPad as a first-class,
always-available surface, so this is a first-class Apple target rather than a niche one. It
is app-wide-only in v1 (Uno AppleUIKit is single-scene today), and it ranks just below macOS
only because macOS is the canonical/reference desktop menu — not because iPadOS is niche.

**Independent Test**: Run an iPadOS 26 Skia app with an application menu set, reveal the
always-available menu bar by swiping down from the top of the screen (no hardware keyboard
needed), and assert the developer's top-level items and shortcuts are present and invoke
their commands. Verify `IsSupported` reports `true` on iPadOS.

**Acceptance Scenarios**:

1. **Given** an application menu set via `SetApplicationMenu`, **When** the iPadOS app's
   `BuildMenu(IUIMenuBuilder)` runs, **Then** the menu reflects the model's items.
2. **Given** an iPadOS 26 device, **When** the user reveals the menu bar by swiping down from
   the top of the screen (no hardware keyboard), **Then** the developer's items and their
   shortcuts appear — including currently-unavailable commands shown visible-but-disabled —
   and activating an enabled one runs its command.
3. **Given** a model change, **When** it is coalesced, **Then** the app requests a menu
   rebuild on `UIMainMenuSystem` where available (falling back to `UIMenuSystem.main`) via
   `setNeedsRebuild`, and the next `BuildMenu` reflects the change.
4. **Given** no menu has been set, **When** the app runs, **Then** no developer items appear
   and `IsSupported` still reports `true` (a native menu system is present).

---

### Edge Cases

- **No native menu present (Windows, Wayland/GNOME without a registrar)** → the core setters
  no-op, `IsExported`/`IsSupported` report `false`, and the declarative `AppMenuBar` renders
  as a real in-app `MenuBar`. Nothing is silently lost.
- **Multiple windows on macOS** → the bar always reflects the key window's menu, falling back
  to the application menu when the focused window set none; closing the key window re-resolves
  the bar for the next key window.
- **Menu set before vs. after launch** → a menu assigned before the native host is ready is
  applied once the host initializes; a menu assigned after launch projects immediately (next
  coalesced tick). Either ordering yields the same final native menu.
- **Unsupported role on a platform** → `IsRoleSupported(role)` reports `false`; the item still
  renders as a normal labeled item where it can, and OS-only roles (About/Quit/Services/Hide)
  no-op on platforms without that slot unless a `Command` is supplied.
- **Icon that cannot be translated** → icon translation is best-effort; an icon that cannot be
  rendered to the native form is dropped and the item shows text only (never an error).
- **Rapid mutation** → many model changes within one dispatcher tick coalesce into a single
  full rebuild of the affected menu; no incremental native diffing is attempted.
- **Radio group with none checked** → all items in the group render unchecked; checking one
  unchecks the rest.
- **`Opening` handler throws / is slow** → submenu population is best-effort; a failed handler
  leaves the previously-projected submenu content rather than crashing the menu.
- **iPadOS in full-screen / no hardware keyboard** → on iPadOS 26 the always-available menu
  bar is still reachable by swiping down from the top of the screen even with no hardware
  keyboard attached; its visibility remains OS-controlled (the app exposes content only and
  offers no force-show API).

## Requirements *(mandatory)*

### Functional Requirements

**Core model (P1)**

- **FR-001**: The system MUST provide a lightweight menu model in namespace
  `Uno.UI.Xaml.Controls`, rooted at `NativeMenuItemBase : DependencyObject` with a `Parent`
  reference, comprising `NativeMenu`, `NativeMenuItem`, and `NativeMenuItemSeparator`. These
  are `DependencyObject`s, **not** `FrameworkElement`s.
- **FR-002**: `NativeMenu` MUST expose an observable `Items` collection (`IList<NativeMenuItemBase>`,
  the XAML content property) raising `INotifyCollectionChanged`, plus `Opening` and `Closed`
  events (typed `EventHandler` / `EventHandler<T>`, never `Action`).
- **FR-003**: `NativeMenuItem` MUST expose `Text` (string), `Icon` (`IconSource`),
  `Command` (`ICommand`), `CommandParameter` (object), a `KeyboardAccelerators` collection,
  `IsEnabled` (bool), `IsChecked` (bool), `ToggleType` (`ToggleType { None, CheckBox, Radio }`),
  `GroupName` (string), `IsVisible` (bool), `Role` (`NativeMenuItemRole`), `SubMenu`
  (`NativeMenu`), and a `Click` event.
- **FR-004**: The system MUST reuse the existing Uno command/icon/shortcut vocabulary —
  `XamlUICommand` / `StandardUICommand` auto-populate label, icon, and accelerator via the
  existing commanding helpers; `IconSource` and `KeyboardAccelerator` are reused as-is.
- **FR-005**: The model MUST be declarable in XAML as a resource/object (via the content
  property) so that the tree itself can be authored in markup even though attachment is
  code-first.

**Attachment setters (P1)**

- **FR-006**: The system MUST provide code-first attachment APIs — `NativeMenu.SetMenu(Window, NativeMenu?)`
  / `NativeMenu.GetMenu(Window)` for window-scoped menus and
  `NativeMenu.SetApplicationMenu(NativeMenu?)` / `NativeMenu.GetApplicationMenu()` for the
  app-wide menu — because `Window` and `Application` are not `DependencyObject`s and an
  attached-property-on-Window design does not port.
- **FR-007**: Window-scoped associations MUST be stored in a side table keyed by `Window`
  (a `ConditionalWeakTable`) so the menu does not leak the window and is released with it.

**Role behavior (P1/P2)**

- **FR-008**: `NativeMenuItemRole` MUST be a thin slot-marker enum
  (`None, ApplicationMenu, About, Settings, Services, Hide, HideOthers, ShowAll, Quit, Window,
  Minimize, Zoom, EnterFullScreen, Help, Undo, Redo, Cut, Copy, Paste, Delete, SelectAll`);
  a role marks an OS slot and supplies a standard label, it does not itself carry behavior.
- **FR-009**: On macOS, OS-owned roles (About, Quit, Services, Hide, HideOthers, ShowAll,
  Minimize, Zoom, EnterFullScreen) MUST auto-wire to the corresponding AppKit selector
  (`terminate:`, `hide:`, `orderFrontStandardAboutPanel:`, the Services submenu, etc.) with
  no developer command.
- **FR-010**: Edit-family and all non-OS-owned roles (Cut, Copy, Paste, Delete, SelectAll,
  Undo, Redo, …) MUST set placement and a standard label only; the **developer MUST supply
  the `Command` and enabled logic** on every platform — the system MUST NOT bridge to the
  native responder chain (Uno draws its own controls and is not a native first responder).
- **FR-011**: On in-app targets (Windows, Linux fallback), role items MUST render as normal
  labeled items; OS-only roles MUST no-op unless a `Command` is supplied.

**Shortcuts (P1)**

- **FR-012**: Shortcuts MUST use literal `KeyboardAccelerator` modifiers with **no**
  Control→Command remap: `VirtualKeyModifiers.Windows` = Command (⌘), `Control` = Ctrl,
  `Menu` = Option/Alt, `Shift` = Shift — matching Uno's existing macOS live-input modifier
  mapping. A menu-only remap is explicitly rejected as incoherent with live key events. A
  cross-platform "Primary" modifier sugar is a non-core future follow-up, not v1.

**Scope rule (P2)**

- **FR-013**: The effective system menu MUST follow a focused-window/scene-wins rule with an
  Application-level fallback: the OS menu reflects the key/focused window's menu if it set one,
  otherwise the Application-level menu. macOS MUST swap `NSApp.mainMenu` on
  `windowDidBecomeKey` and restore on resign (per-window supported in v1). iPadOS MUST request
  a rebuild on the always-available macOS-like menu bar (`UIMainMenuSystem.setNeedsRebuild()`
  on iOS/iPadOS 26 where available, falling back to `UIMenuSystem.main.setNeedsRebuild()` on
  earlier OS) on scene-focus change, but because Uno AppleUIKit is single-scene today,
  **iPadOS v1 is app-wide only** — per-scene override is deferred until Uno gains multi-scene
  (the design accommodates it).

**Observable updates (P2/P3)**

- **FR-014**: The model MUST be observable end-to-end: `DependencyProperty` change callbacks
  plus `INotifyCollectionChanged` on `Items`. Any change MUST mark the affected menu dirty.
- **FR-015**: Dirty menus MUST be coalesced on the dispatcher and re-projected via a **full
  rebuild** of that menu (reset strategy). The system MUST NOT attempt incremental native
  diffing (iPadOS and Linux are rebuild-only).
- **FR-016**: `NativeMenu` MUST raise `Opening` before a (sub)menu is shown, enabling
  just-in-time population — mapped to `NSMenuDelegate.menuNeedsUpdate` on macOS, DBus
  `AboutToShow` on Linux, and the iPadOS rebuild — and `Closed` when it dismisses.
- **FR-017**: Effective-enabled state MUST be pushed (not pulled): effective-enabled =
  `IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)`, observing
  `CanExecuteChanged`. macOS MUST set `NSMenu.autoenablesItems = NO` so the pushed state is
  authoritative.
- **FR-018**: Toggle/radio state MUST mirror `RadioMenuFlyoutItem` semantics: `ToggleType`
  + `IsChecked` + `GroupName`. macOS MUST coordinate radio exclusivity itself (no native
  radio group) and set the check state; Linux MUST use native `toggle-type=radio`; iPadOS MUST
  use on/off/mixed with inline single-selection.

**Framework-guaranteed macOS App menu (P1)**

- **FR-019**: The framework MUST always ensure the bold app-name menu exists with at least
  Quit (⌘Q) and Hide, placed before the developer's top-level menus. To customize it, the
  developer declares a top-level `NativeMenuItem` with `Role=ApplicationMenu` whose children
  (About, Settings, …) merge into the app menu. Services/HideOthers MUST NOT be auto-injected
  unless requested (consistent with thin roles). This replaces/extends the existing bootstrap
  `NSMenu` (Quit ⌘Q / Close Window ⌘W).

**Projection seam (P1)**

- **FR-020**: The system MUST define `INativeMenuExtension` in `Uno.UI` exposing
  `SetMenu(scope, NativeMenu?)`, a `bool IsExported` probe (is a native menu actually shown?),
  and capability probes `IsSupported` / `IsRoleSupported(NativeMenuItemRole)`. It MUST be
  registered per Skia host and resolved via
  `ApiExtensibility.CreateInstance<INativeMenuExtension>()`.
- **FR-021**: The Skia.MacOS implementation MUST project to `NSMenu` through the UnoNativeMac
  ObjC layer via new native exports (e.g. `uno_window_set_main_menu` / `uno_app_set_main_menu`
  plus menu build/update entry points), and MUST swap the menu on window-key changes for
  multi-window support.
- **FR-022**: The Skia.AppleUIKit implementation MUST project to `UIMenuBuilder` by overriding
  `UnoUIApplicationDelegate.BuildMenu(IUIMenuBuilder)` (the delegate is a `UIResponder` and is
  overridable by developers via `UseUIApplicationDelegate<T>`), populating the always-available
  macOS-like iPadOS 26 menu bar, app-wide in v1. Rebuilds MUST be driven on `UIMainMenuSystem`
  (the iOS/iPadOS 26 `UIMenuSystem` subclass for the iPad menu bar) where available, falling
  back to `UIMenuSystem.main` on earlier OS. Per the single-scene caveat above, iPadOS v1 is
  app-wide only.
- **FR-022a**: For discoverability on the iPadOS menu bar, the system MUST keep an app's
  commands **visible but disabled** when they are currently unavailable rather than removing
  them, so users can discover the app's capabilities — aligning with the push-based enablement
  rule (prefer toggling `IsEnabled` over `IsVisible=false`/removal for menu commands). The
  always-available macOS-like menu bar requires **iPadOS 26+**; on earlier iPadOS (15–25) the
  same `UIMenuBuilder` content surfaces as the older transient menu shown with a hardware
  keyboard (Cmd-hold), and on iPhone no menu bar is shown (the same `UIKeyCommand` shortcuts
  still work with a hardware keyboard). Uno supplies the content; the OS presents it per
  version.
- **FR-023**: The Win32 implementation MUST be a no-op with `IsExported = false` (no HMENU);
  the in-app `AppMenuBar` renders the real `MenuBar` there.
- **FR-024**: A Skia.X11 implementation projecting to a DBusMenu server
  (`com.canonical.dbusmenu`) registered with `com.canonical.AppMenu.Registrar` keyed by X11
  window XID, failing silently with the in-app fallback when no registrar is present, is a
  **post-v1** designed-for extension point and is **out of scope for v1**.

**Toolkit `AppMenuBar` control (P2)**

- **FR-025**: The declarative `AppMenuBar : MenuBar` control (Toolkit, namespace
  `Uno.Toolkit.UI`, separate follow-up feature) MUST render as a real in-app `MenuBar` on
  Windows / Linux-without-native, and on Apple MUST read its `MenuBarItem` / `MenuFlyoutItem`
  content, translate it to a `NativeMenu`, project via the core `NativeMenu.SetMenu(window, …)`,
  and collapse to zero in-app footprint. OS slots MUST be expressible via a Toolkit
  `AppMenu.Role` attached property that maps to `NativeMenuItem.Role`. The Toolkit MUST depend
  on the core seam and the core MUST NOT depend on the Toolkit.

**Capability probe & fallback (P2)**

- **FR-026**: The system MUST expose `NativeMenu.IsSupported` (is any native menu available
  here?) and `IsRoleSupported(NativeMenuItemRole)` so apps can adapt their UI per platform.
- **FR-027**: On a target with no native menu, the core setters MUST no-op and report
  `IsExported = false`; no exception is thrown and the model is retained so a later
  Toolkit/in-app consumer can still render it.

**Icons (P2)**

- **FR-028**: Icon translation MUST be best-effort and documented as such: bitmap/image
  `IconSource` → PNG bytes → native image; symbol/font `IconSource` → rendered glyph image
  best-effort; with an optional SF-Symbol-name passthrough hatch for Apple. An icon that cannot
  be translated MUST be dropped (text-only item), never surfaced as an error.

**Threading (P1)**

- **FR-029**: Native menu APIs are main-thread-only; all model mutations and projection work
  MUST marshal to the UI thread, and the coalesced rebuild MUST be posted to the dispatcher.

**Forward-shaping (P3)**

- **FR-030**: The model SHOULD be shaped so a future system-tray / notification-area menu can
  reuse `NativeMenu` unchanged. The tray surface itself is **not** built in v1.

### Key Entities

See [data-model.md](./data-model.md) for full property tables and relationships.

- **`NativeMenuItemBase`** — abstract base (`DependencyObject`) with a `Parent` back-reference.
- **`NativeMenu`** — a menu/submenu: observable `Items`, `Opening`/`Closed` events.
- **`NativeMenuItem`** — a single command/checkable/submenu item (the entity carrying `Text`,
  `Command`, `Role`, accelerators, toggle state, `SubMenu`).
- **`NativeMenuItemSeparator`** — a visual divider.
- **`NativeMenuItemRole` / `ToggleType`** — the thin slot-marker and toggle enums.
- **`INativeMenuExtension`** — the projection seam (see
  [contracts/INativeMenuExtension.md](./contracts/INativeMenuExtension.md)).
- **Attachment side-table** — the `ConditionalWeakTable<Window, NativeMenu>` plus the
  application-menu slot backing the setters.

## Cross-Platform Capability Matrix

| Capability | macOS | iPadOS | Linux (DBusMenu) | Windows |
|---|---|---|---|---|
| App-wide menu | Yes (`NSApp.mainMenu`) | Yes (`buildMenu`) | No (per-window only) | No native |
| Per-window menu | Yes (swap on key) | No in v1 (single scene) | Yes (the only model) | Per-control (in-app) |
| Submenu / separator / checkable | Yes | Yes | Yes | Yes (in-app) |
| Radio | Framework-coordinated | Inline single-selection | `toggle-type=radio` | In-app |
| Icon | Yes | Yes | Partial (icon-name / icon-data) | In-app |
| Shortcut | Yes | Yes | Partial (panel-dependent) | In-app accelerators |
| Enable/disable + visibility | Yes (pull-validated, we push) | Yes (pull-validated, we push) | Yes (push) | Yes (push) |
| Dynamic update | Live | Rebuild-only | Re-export | In-app |
| OS standard items (About/Quit/Services) | Yes | Yes | No | No |
| Custom-view items | Yes (out of scope) | No | No | No |

## Scope & Phasing

**v1 (this feature + its Toolkit follow-up)** — the core `NativeMenu` model, the code-first
setters, the `INativeMenuExtension` seam, the **macOS** (`NSMenu`) and **iPadOS** (app-wide
`UIMenuBuilder`) implementations, the Win32 no-op (in-app fallback), and the declarative
`AppMenuBar` control in the Toolkit. **Skia targets only** (Uno's Skia-first policy); legacy
native iOS/macOS targets are maintenance-only and out of scope.

**Post-v1 (designed-for extension points)** — the Linux DBusMenu implementation
(`com.canonical.dbusmenu` + AppMenu registrar, X11-keyed, fail-silent with in-app fallback on
Wayland/GNOME), Windows polish, iPadOS multi-scene per-scene override (gated on Uno gaining
multi-scene support), and the system-tray / notification-area menu reuse of `NativeMenu`.

**Two-repo coordination** — the core (unoplatform/uno) ships the model and seam first; the
Toolkit `AppMenuBar` (unoplatform/uno.toolkit.ui) is a separate follow-up that depends on the
shipped core seam. The dependency is strictly one-way (Toolkit → core).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A macOS Skia app that sets an application menu shows that menu in the OS menu
  bar at the top of the screen, with the framework-guaranteed app-name menu (Quit ⌘Q, Hide)
  leading — verified by an inspection-level test.
- **SC-002**: With no menu set, a freshly launched macOS app still has a working Quit (⌘Q)
  and Hide in the app menu.
- **SC-003**: A menu item with a `Command` runs that command on click and on its
  `KeyboardAccelerator`, and disables itself when `CanExecute` is false — verified by a test,
  with literal modifiers ( `Windows` → ⌘ on macOS) and no remap.
- **SC-004**: On a focused-window switch on macOS, the menu bar reflects the key window's menu
  within one focus cycle, falling back to the application menu for windows with none.
- **SC-005**: A model change (add/remove item, toggle check, change text/enabled) is reflected
  in the native menu after a single coalesced rebuild, and N rapid changes within one
  dispatcher tick produce exactly one native rebuild.
- **SC-006**: An `Opening` handler that populates a submenu results in the new items appearing
  the first time the submenu is opened.
- **SC-007**: On a target with no native menu (Windows), the core setters no-op,
  `IsSupported`/`IsExported` report `false`, and the declarative `AppMenuBar` renders a real
  in-app `MenuBar` with the same items.
- **SC-008**: The same `AppMenuBar` markup yields a native menu (zero in-app footprint) on
  macOS/iPadOS and an in-app `MenuBar` on Windows, with no per-platform markup change.
- **SC-009**: On the always-available iPadOS 26 menu bar (revealed by swiping from the top of
  the screen, no hardware keyboard required), the app's items and shortcuts appear in the OS
  menu bar and invoke their commands, and `IsSupported` reports `true`.
- **SC-010**: `IsRoleSupported` correctly reports which roles a given platform honors, so an
  app can hide or relabel items it cannot project.
- **SC-011**: No regression to existing Uno menu controls (`MenuBar`, `MenuFlyout*`) or to the
  existing macOS bootstrap behavior beyond the intended app-menu replacement.

## Assumptions

- The existing Uno types are reused as the foundation: `MenuBar`/`MenuBarItem`/`MenuFlyout*`,
  `XamlUICommand`/`StandardUICommand`, `KeyboardAccelerator`, and the `IconSource`/`IconElement`
  family are all implemented and available.
- `Window` and `Application` are **not** `DependencyObject`s in Uno/WinUI (verified), which is
  why attachment is code-first via setters rather than attached properties.
- Uno's macOS live-input modifier mapping (Command→Windows, Control→Control, Option→Menu,
  Shift→Shift) is the contract that the literal-modifier shortcut rule must stay coherent with.
- iPadOS 26 brings the always-available, macOS-like system menu bar to iPad (revealed by
  swiping down from the top of the screen, pointer-to-top, or Globe+M, with no hardware
  keyboard required); on earlier iPadOS (15–25) the same `UIMenuBuilder` content instead
  surfaces as the older transient menu shown with a hardware keyboard. Uno AppleUIKit is
  single-window/single-scene today; iPadOS per-scene override is deferred until multi-scene
  support lands, and the model is shaped to accommodate it without breaking changes (iPadOS v1
  is app-wide only).
- The `ApiExtensibility` per-host registration pattern is the supported way to resolve a
  platform implementation from `Uno.UI`.
- Icon translation fidelity is best-effort by design; SF-Symbol passthrough is the highest-
  fidelity Apple path.
- WinUI/AppKit/Avalonia references may be consulted for native menu semantics (roles, radio
  coordination, lazy population), with WinUI naming preferred where applicable.

## Open Questions / Risks

- **[NEEDS CLARIFICATION]** Linux global-menu availability is desktop-environment-dependent
  (KDE/Unity expose a registrar; GNOME/Wayland generally do not). The post-v1 Linux path must
  fail silently to the in-app fallback, but the exact detection heuristic (registrar presence
  on the bus) needs confirming when that phase begins.
- **Icon translation fidelity** — symbol/font glyph rendering to a native image is best-effort
  and may not match the native look exactly; the SF-Symbol passthrough mitigates this on Apple
  but has no Windows/Linux analogue.
- **iPadOS multi-scene timing** — per-scene menus depend on Uno gaining multi-scene support;
  the v1 design is app-wide-only and must not bake in single-scene assumptions that block the
  later override.
- **Optional "Primary" modifier sugar** — a cross-platform Cmd-on-Mac / Ctrl-on-Windows
  convenience modifier is deliberately deferred to a non-core follow-up; v1 stays on literal
  modifiers to remain coherent with live key events. Whether/when to add it is open.
- **Custom-view menu items** (arbitrary views inside a macOS menu item) are explicitly out of
  scope for v1; reconfirm there is no early demand before the model would need to accommodate
  them.
