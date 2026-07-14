# Research: Native OS Menu Integration

**Date**: 2026-06-30
**Branch**: `047-native-os-menu`
**Issue**: unoplatform/uno#22172 (core model + projection seam)
**Scope**: Apple-first, design-for-all. v1 = macOS (NSMenu) + iPadOS (UIMenuBuilder); Linux (DBusMenu) + Windows are post-v1 extension points. Skia targets only.

> **Evidence level.** Native-platform sections (macOS / iPadOS / Linux / Windows) are
> grounded in the platform vendor documentation and the prior-art codebases
> (Avalonia, Flutter) named below; treat them as *design research*, not Uno runtime
> validation. The **Uno inventory (§8)** is the load-bearing part for implementation:
> every integration point there was traced in the live source and is cited with a
> relative `../../src/...` link and a line number. No native menu code exists in Uno
> today, so there is nothing to runtime-validate yet.

This document consolidates the investigation behind the [final design decisions](./spec.md):
the cross-platform asymmetries that force the shape of the model, the two prior-art
frameworks that solved the same problem (Avalonia and Flutter), the four target
platforms' native menu systems, and the existing Uno surfaces the design reuses.

---

## 1. Executive summary — the load-bearing asymmetries

A "native menu" means something structurally different on each OS. Four asymmetries
dominate every design decision; everything in the spec is a response to one of them.

| # | Asymmetry | macOS | iPadOS | Linux (DBusMenu) | Windows |
|---|-----------|-------|--------|------------------|---------|
| **A1** | **Scope: app vs window** | One **app-wide** menu (`NSApp.mainMenu`), swapped per key window | One **app-global** menu (`buildMenu`), single-scene | **Per-window only** (keyed by window XID) | Per-control (in-app), no OS global menu |
| **A2** | **Validation: pull vs push** | Pull (`autoenablesItems` + `validateMenuItem:`) — *we override to push* | Pull (per-responder `validate(_:)`) — *we override to push* | Push (exporter sets enabled/visible) | Push (in-app control state) |
| **A3** | **Update: live vs rebuild** | **Live** (mutate `NSMenuItem` in place; `NSMenuDelegate` JIT) | **Rebuild-only** (`setNeedsRebuild` → full `buildMenu`) | **Re-export** (push whole layout) | Live (in-app DP changes) |
| **A4** | **OS standard items** | **Yes** (About/Quit/Services/Hide auto-wire to AppKit selectors) | **Yes** (system-provided `UICommand`/identifiers) | **No** | **No** |

The design's responses, one-to-one:

- **A1 → both-scope attachment + focused-window-wins fallback.** The API exposes
  *both* `SetMenu(Window, …)` (window-scoped) and `SetApplicationMenu(…)` (app-wide);
  the system menu reflects the focused window's menu if it set one, else the
  Application menu (spec decision 6). macOS honors per-window by swapping
  `NSApp.mainMenu` on `windowDidBecomeKey`; iPadOS is single-scene today, so v1 is
  app-wide only, with per-scene deferred until Uno gains multi-scene.
- **A2 → push-only enablement.** Because pull-validation (the responder chain) assumes
  the menu's target is a native first responder, and **Uno draws its own controls and
  is never a first responder**, the design pushes effective-enabled state
  (`IsEnabled && (Command?.CanExecute(p) ?? true)`) and disables native auto-validation
  (`NSMenu.autoenablesItems = NO`). This is the single most important correctness
  constraint and it is why `Role` does *not* bridge edit commands to the responder chain.
- **A3 → observable model + coalesced full rebuild.** The model is observable (DP change
  callbacks + `INotifyCollectionChanged` on `Items`); any change marks the menu dirty,
  coalesces on the dispatcher, and re-projects via a **full rebuild** (the lowest common
  denominator: iPadOS and Linux are rebuild-only). A `NativeMenu.Opening` event covers
  JIT submenu population where the platform supports it.
- **A4 → thin slot-marker `Role` enum.** Only Apple platforms have OS-owned items, so
  `Role` is a *placement marker*, not a behavior bridge. On macOS the framework wires the
  OS-owned slots (About/Quit/Services/Hide) to AppKit selectors; everywhere else a role
  sets placement + a standard label only, and the developer always supplies the
  `Command`. On Windows/Linux in-app, OS-only roles no-op unless a `Command` is given.

A fifth, smaller asymmetry — **shortcut modifier semantics** — is resolved by *not*
remapping: KeyboardAccelerator modifiers are reused literally, matching Uno's existing
live-key mapping (§8.3), so a menu accelerator and a live key event agree.

---

## 2. Avalonia analysis (primary prior art)

Avalonia ships the closest analog to what Uno needs: a lightweight, code-first native
menu model with an in-app fallback control, projecting to macOS / Linux and storing-only
on Windows. The Uno design borrows Avalonia's *shape* wholesale and diverges only where
Uno's type system (Window/Application not being DependencyObjects) forces it.

### 2.1 The model

- **`NativeMenu` / `NativeMenuItem` / `NativeMenuItemSeparator`**, all derived from
  `AvaloniaObject` (Avalonia's DependencyObject equivalent) — **not** `Control`/visual
  elements. They are a pure data model, exactly the "lightweight DO, not FrameworkElement"
  choice in Uno spec decision 1.
- `NativeMenuItem` carries `Header`, `Icon`, `Command`, `CommandParameter`, `Gesture`
  (the accelerator), `IsEnabled`, `IsChecked`, `ToggleType` (`{None, CheckBox, Radio}`),
  and a nested `Menu` (submenu). This is a near-exact superset match to Uno's
  `NativeMenuItem`; Uno adds `Role`, `GroupName`, `IsVisible`, and uses the existing
  `IconSource`/`KeyboardAccelerator` vocabulary instead of Avalonia's own.
- Items are an **observable collection** (`NativeMenuItemsCollection : AvaloniaList`),
  which is what makes the live-update strategy possible. Uno mirrors this with
  `INotifyCollectionChanged` on `NativeMenu.Items`.

### 2.2 Both-scope attachment

Avalonia attaches the menu via a **`NativeMenu.Menu` attached property** set on
**both** a `TopLevel` (window-scoped) **and** the `Application` (app-wide) — because in
Avalonia *both are AvaloniaObjects*. The exporter reads whichever applies, with the
window menu taking precedence. **This is the one place Uno cannot copy Avalonia
verbatim**: in WinUI/Uno, `Window` and `Application` are **not** DependencyObjects
(verified §8.1–8.2), so an attached-property-on-Window/Application does not port. Uno
keeps the *both-scope* semantics but switches the mechanism to **code-first static
setters** backed by a `ConditionalWeakTable` side-table (spec decision 3). The menu
*tree* can still be authored in XAML as a resource and then assigned in code.

### 2.3 The exporter seam and `MenuTarget`

Avalonia projects through an `ITopLevelNativeMenuExporter` / per-backend exporter, with a
`NativeMenuExporterDefaultRoots`/`MenuTarget` enum of **`Application | Window | TrayIcon |
Dock`**. This validates two Uno decisions:

- The **projection seam** abstraction (Uno's `INativeMenuExtension` with a `scope`
  argument) is the right altitude — Avalonia proved a single exporter interface can serve
  app, window, tray, and dock targets.
- **Tray-ready shaping.** Because the *same* `NativeMenu` model feeds a `TrayIcon` in
  Avalonia, Uno deliberately shapes `NativeMenu` so a future system-tray / notification-area
  menu can reuse it (spec "tray-ready", not built v1).

### 2.4 Per-backend behavior Uno mirrors

- **macOS** (`AvaloniaNativeMenuExporter` → `IAvnMenu`): distinguishes
  `SetAppMenu` (the app-wide bold-name menu) from per-window `SetMainMenu`, and
  `PopulateStandardOSXMenuItems` **auto-injects** a baseline app menu (About/Hide/Quit
  wired to native actions) when the developer doesn't supply one. Uno adopts the same
  "framework-guaranteed minimal app menu with at least Quit + Hide" rule (spec decision 8),
  and the same App-menu-customization-by-role approach (declare a top-level item with
  `Role=ApplicationMenu` whose children merge into the app menu).
- **Linux** (`DBusMenuExporter`): exports `com.canonical.dbusmenu` and registers with
  `com.canonical.AppMenu.Registrar`, **failing silently** when no registrar is present
  (Wayland/GNOME). Uno copies the fail-silent contract and the in-app fallback elsewhere
  (spec decision Linux backend, post-v1).
- **Windows**: the menu is **store-only / no-op** (no HMENU); the in-app `NativeMenuBar`
  renders instead. Uno's Win32 impl is identically a no-op with `IsExported = false`.

### 2.5 The in-app fallback control

Avalonia's **`NativeMenuBar`** is a real templated control that, on platforms with a
native exporter, **collapses to zero size** (it has exported its content) and on
platforms without one **renders an in-app menu bar**. This is exactly the
**`AppMenuBar : MenuBar`** behavior in the Uno **Toolkit** (separate repo/issue): render
normally where there's no native menu, translate-and-collapse on Apple. The split — model
+ seam in core, declarative control in Toolkit — keeps the dependency one-way.

### 2.6 Full-rebuild update strategy

Avalonia's exporters do **not** incrementally diff; a model change triggers a
`QueueReset` that re-exports the whole (sub)menu. Uno adopts the identical
coalesced-full-rebuild strategy (spec decision 7), because the rebuild-only platforms
(iPadOS, Linux) make incremental native diffing pointless as a common path.

**What Uno borrows from Avalonia:** the model + in-app-control pairing; both-scope
attachment semantics (mechanism changed to setters); the single exporter seam with a
scope/target argument; `ICommand` + `CanExecute` execution; tray-ready model shaping;
fail-silent registrar on Linux; framework-guaranteed minimal macOS app menu; and the
coalesced full-rebuild update model.

---

## 3. Flutter analysis (secondary prior art)

Flutter solved a narrower version of the same problem and contributes two specific ideas:
the **portable role enum** and the **capability probe**.

### 3.1 The model

Flutter's `PlatformMenuBar` widget takes a tree of:

- **`PlatformMenu`** — a submenu with a label and children.
- **`PlatformMenuItem`** — a leaf with `label`, `onSelected` callback, and
  `shortcut` (a `MenuSerializableShortcut`).
- **`PlatformMenuItemGroup`** — a group rendered with separators around it (the
  separator concept, expressed as grouping rather than an explicit separator item).
- **`PlatformProvidedMenuItem`** — an **OS-provided** item selected from a fixed enum
  (`PlatformProvidedMenuItemType`: `about`, `quit`, `servicesSubmenu`, `hide`,
  `hideOtherApplications`, `showAllApplications`, `startSpeaking`, `toggleFullScreen`,
  `minimizeWindow`, `zoomWindow`, `arrangeWindowsInFront`, …).

`PlatformProvidedMenuItem` is the direct ancestor of Uno's **`NativeMenuItemRole`** enum:
a portable, OS-agnostic *marker* for a slot the OS owns, where the framework supplies the
wiring. Uno generalizes it from "provided item type" to a `Role` *property* on a normal
`NativeMenuItem` (so a role item can still carry a developer `Command` on non-Apple
platforms), but the enum membership is essentially the Flutter set plus the edit roles.

### 3.2 Channel + platform reach

- Projection goes over the **`flutter/menu` platform channel**; the menu tree is
  serialized to a channel message and the platform side builds the native menu.
- **macOS is the only platform with a real native menu** in Flutter. On every other
  platform `PlatformMenuBar` **silently degrades** (renders nothing / no-ops) — there is
  no in-app fallback in the framework itself. Uno improves on this by pairing the model
  with the Toolkit `AppMenuBar` in-app fallback so non-native platforms still get a menu.

### 3.3 Capability probe

Flutter exposes `PlatformProvidedMenuItem.hasMenu` and per-item availability so an app can
ask "does this platform provide this item?" and adapt its tree. Uno borrows this as
**`NativeMenu.IsSupported`** (is there *any* native menu here?) and
**`IsRoleSupported(NativeMenuItemRole)`** (is *this* OS slot available?), so apps can
branch their menu construction (spec "capability probe").

**What Uno borrows from Flutter:** the portable role-enum concept (`PlatformProvidedMenuItem`
→ `NativeMenuItemRole`) and the capability-probe API. Uno explicitly *rejects* Flutter's
"silent degrade, no fallback" choice in favor of Avalonia's in-app fallback.

---

## 4. macOS — NSMenu / NSMenuItem (primary v1 target)

### 4.1 App-wide structure

macOS has exactly one menu bar at the top of the screen, owned by the application:
`NSApplication.mainMenu` (`NSApp.mainMenu`). Its structure is rigid:

- `mainMenu` is an `NSMenu` whose **items are the top-level bar entries** (the bold
  app-name menu, then File, Edit, View, … Help).
- **Each** top-level `NSMenuItem` has **no action of its own**; it carries a `submenu`
  (`NSMenuItem.submenu`, an `NSMenu`) that holds the actual commands.
- The **first** top-level item is special: it is the **application menu** (rendered with
  the app's name in bold), conventionally About / Settings… / Services / Hide / Quit.
- macOS auto-positions a few menus by convention (the Services submenu, the Window menu
  via `NSApp.windowsMenu`, the Help search menu) when they are tagged appropriately.

### 4.2 Items, shortcuts, state, image

- `NSMenuItem.title` — the label. `NSMenuItem.separatorItem` — the separator.
- **Shortcut** = `keyEquivalent` (a string, e.g. `"q"`) + `keyEquivalentModifierMask`
  (an `NSEventModifierFlags` bitmask: `.command`, `.option`, `.control`, `.shift`). This
  maps cleanly from a Uno `KeyboardAccelerator` (`Key` + `Modifiers`), and — critically —
  the modifier mapping is the **same direction** Uno already uses for live key events
  (§8.3): `VirtualKeyModifiers.Windows` ⇄ `.command`.
- **Checked/mixed** = `NSMenuItem.state` (`.on` / `.off` / `.mixed`). There is **no
  native radio-group type**; radio exclusivity is coordinated by the app — the framework
  sets `.on` on the selected item and `.off` on its `GroupName` peers. (Avalonia does the
  same.)
- **Icon** = `NSMenuItem.image` (an `NSImage`). Uno translates `IconSource` best-effort to
  PNG bytes → `NSImage`, with an optional SF-Symbol-name passthrough for Apple.
- **Submenu/enabled/visibility** = `submenu`, `isEnabled`, `isHidden`.

### 4.3 Validation — why Uno pushes state (A2)

By default `NSMenu.autoenablesItems == YES`: AppKit **pulls** each item's enabled state by
walking the responder chain and asking the target whether it responds to the item's
`action` (and consulting `validateMenuItem:` / `NSMenuItemValidation`). This presupposes
the menu's target is a live native responder. **Uno renders its own controls and is not a
native first responder**, so pull-validation would disable everything. The design therefore
sets **`NSMenu.autoenablesItems = NO`** and **pushes** the effective-enabled state computed
in managed code (`IsEnabled && CanExecute`), observing `CanExecuteChanged`. This is the
macOS embodiment of asymmetry A2 and the reason `Role` does not bridge edit commands.

### 4.4 OS-owned items (A4)

A handful of standard items are wired to AppKit selectors on `NSApp` / first responder:
`terminate:` (Quit), `hide:` (Hide), `hideOtherApplications:`, `unhideAllApplications:`
(Show All), `orderFrontStandardAboutPanel:` (About), and the Services submenu
(`NSApp.servicesMenu`). For these, Uno's framework auto-wires the selector when a role is
present (no developer Command needed). The existing bootstrap menu
(`UNOApplication.m:95-124`) already does this for Quit (`terminate:`) and Close
(`performClose:`); the seam **replaces/extends** that menu rather than coexisting with it.

### 4.5 Dynamic updates

macOS supports both **live mutation** (change `title`/`state`/`isEnabled` on an existing
`NSMenuItem` and it reflects immediately) and **JIT population** via `NSMenuDelegate`:
`menuNeedsUpdate:` (called before a menu is displayed) and `numberOfItems` /
`menu:updateItem:atIndex:shouldCancel:`. Uno maps `NativeMenu.Opening` to
`menuNeedsUpdate:` for just-in-time submenu population. Despite macOS supporting live
mutation, Uno still uses the coalesced full-rebuild as the *common* path (A3) and reserves
live-mutate as a possible macOS optimization.

### 4.6 Limitations

- One menu bar per app — per-window is emulated by **swapping** `NSApp.mainMenu` on
  `windowDidBecomeKey:` and restoring on resign (multi-window is real on macOS, so this is
  a v1 requirement, not a future).
- Custom-view menu items (`NSMenuItem.view`) exist but are **out of scope**.
- The app menu's bold name comes from the bundle, not the menu — the first item's title is
  effectively ignored for display.

---

## 5. iPadOS / Mac Catalyst — UIMenuBuilder / UIMenu / UICommand

### 5.1 App-global build model

UIKit's menu system is **app-global and declarative-by-rebuild**. The app delegate (a
`UIResponder`) overrides **`buildMenu(with: UIMenuBuilder)`**; UIKit calls it to construct
the entire menu tree. The builder lets you **insert / replace / remove** `UIMenu`s and
`UICommand`s relative to **system identifiers** (`UIMenu.Identifier`,
`UICommand`/`UIKeyCommand`), e.g. insert a child menu `before:`/`after:` `.file`, or remove
`.format`. The app supplies content; UIKit owns layout and the system slots.

- **`UIMenu`** — a submenu (title, image, children, options like `.displayInline` for
  separator-style grouping and single-selection inline groups).
- **`UICommand`** — a leaf (title, image, `action:` selector, `propertyList`); **`UIKeyCommand`**
  is a `UICommand` subclass that adds `input` + `modifierFlags` (the shortcut).
- **System-provided items** come from standard identifiers (About, Services, Hide, Quit on
  Catalyst; edit commands) — the A4 "OS standard items" for this platform.

### 5.2 Rebuild-only updates (A3)

There is **no live mutation**. To change the menu you request a rebuild on the menu system
(`setNeedsRebuild()`, or `setNeedsRevalidate()` for enabled-state-only), and UIKit calls
`buildMenu(with:)` again to reconstruct the tree. iOS/iPadOS 26 adds **`UIMainMenuSystem`**
— a new `UIMenuSystem` subclass that represents the iPad menu bar — so the Uno backend
requests the rebuild on **`UIMainMenuSystem.shared`** where available, falling back to
**`UIMenuSystem.main`** on earlier OS. This is the canonical **rebuild-only** platform and
the reason Uno's common update path is a full rebuild. Uno's observable model marks dirty →
coalesces → requests the rebuild, and the `buildMenu` override reads the current
`NativeMenu` to emit `UIMenu`/`UICommand`s. The `UIMenuBuilder` API itself is **unchanged**
and remains the correct seam (iOS/iPadOS 26 only enhanced it with new convenience methods,
faster construction, and better diagnostics).

### 5.3 Validation

Per-responder validation via `validate(_ command: UICommand)` / `canPerformAction(_:withSender:)`
along the responder chain — the same pull model as macOS, and Uno overrides it the same
way (push effective state into the emitted `UICommand`s during the rebuild). `setNeedsRevalidate()`
is the enabled-only fast path.

### 5.4 When the bar appears

- **iPadOS 26 brings the macOS menu bar to iPad as an always-available, first-class system
  menu bar** — it does **not** require a hardware keyboard. Users reveal it by **swiping
  down from the top of the screen** (also by moving the pointer to the top edge, or via
  Globe+M), and it works on any iPad regardless of input device. This is a real macOS-like
  menu bar, not a transient Cmd-hold HUD. **Bar visibility is OS-controlled** — there is
  deliberately **no force-show API** in the Uno design; the app contributes *content* only.
- **Version nuance.** The same `UIMenuBuilder` content surfaces differently per OS version:
  on **iPadOS 26+** it is the always-available menu bar above; on **earlier iPadOS (15–25)**
  the identical content is presented as the older **transient menu shown with a hardware
  keyboard** (Cmd-hold). We supply the content once; the OS presents it per version.
- On **iPhone** there is no menu bar at all (`UIKeyCommand`s still work with a hardware
  keyboard, but the menu system does not surface a bar).
- **Discoverability convention.** Because the iPadOS 26 menu bar is meant to advertise an
  app's full capability surface, it should display **all** app commands — **including
  currently-unavailable ones**. Keep disabled commands **visible but disabled** (do not
  remove them) so users discover what the app can do. This aligns cleanly with Uno's
  push-based enablement (A2): prefer toggling `IsEnabled` over `IsVisible=false`/removal for
  menu commands.

### 5.5 Catalyst unification & Uno reach

Mac Catalyst uses the same `UIMenuBuilder` API but renders into a real macOS `NSMenu`,
unifying the iPad and Mac-Catalyst code path. For Uno, the seam lives in
**`UnoUIApplicationDelegate.BuildMenu(IUIMenuBuilder)`** — the delegate is already a
`UIResponder` that can override `BuildMenu`, and devs already override the delegate via
`UseUIApplicationDelegate<T>` (§8.4–8.5).

### 5.6 Limitations

- **App-global only in Uno v1**: Uno AppleUIKit is single-window/single-scene today
  (§8.6), so per-scene menu override is deferred until Uno gains multi-scene. The model
  accommodates it (scope argument) but the backend projects app-wide.
- Rebuild-only — no in-place item mutation; every change is a full reconstruct.
- Icons map to `UIImage` (incl. SF Symbols by name); shortcuts to `UIKeyCommand`.

---

## 6. Linux — DBusMenu (com.canonical.dbusmenu) [post-v1]

### 6.1 The native models

Linux has **no single OS menu API**; the de-facto "global menu" is a desktop-shell
convention exported over D-Bus:

- **`com.canonical.dbusmenu`** (the Unity/Ubuntu-era "dbusmenu" protocol, still used by KDE
  Plasma's global menu and the appmenu-gtk module) — the app exports a menu layout object;
  the shell renders it. Registration is via **`com.canonical.AppMenu.Registrar`**, keyed by
  the **X11 window XID**, so it is fundamentally **per-window** (asymmetry A1: Linux is the
  one platform whose *only* model is per-window).
- **`org.gtk.Menus` + `org.gtk.Actions`** — GTK's own exported-menu/action protocol, an
  alternative used by some GNOME apps. (Avalonia uses dbusmenu; Uno's recommended strategy
  follows suit for breadth of shell support.)

### 6.2 The fragmentation problem

- **GNOME removed the global app menu** (the old top-bar app menu was deprecated and
  dropped); GNOME Shell does not render a dbusmenu global menu by default.
- **KDE Plasma supports it** (Global Menu widget) via dbusmenu.
- **Wayland gap**: registration is XID-keyed and X11-centric; under Wayland there is no
  stable surface→XID, so the dbusmenu/registrar path is effectively **X11-only**.

### 6.3 Capabilities

Submenu / separator / checkable / **radio (native `toggle-type=radio`)** are all
supported; icons are **partial** (icon-name or icon-data, no rich rendering); shortcuts are
**panel-dependent** (the shell may or may not render the accelerator). Updates are
**re-export** (push the whole layout; `AboutToShow` provides JIT population — the Linux
analog of `NativeMenu.Opening`). Enable/disable + visibility are **push** (A2: Linux pushes).
**No OS-standard items** (A4: Linux has no About/Quit/Services slots).

### 6.4 Recommended strategy

Export **DBusMenu on X11**, register with `com.canonical.AppMenu.Registrar`, and **fail
silently** when no registrar is present (Wayland, GNOME, no panel) — exactly Avalonia's
contract. Where there is no registrar, fall back to the **in-app `AppMenuBar`**. This is
**post-v1**: the seam (`INativeMenuExtension`) is designed so a `Skia.X11` impl can be
added without touching the core model.

---

## 7. Windows / WinUI

### 7.1 No OS global menu

Windows has **no application-global menu bar**. The legacy native construct is the Win32
**`HMENU`** attached to a top-level `HWND` (`SetMenu`) — strictly **per-window**, drawn in
the non-client area, and stylistically inconsistent with modern Fluent apps. **WinUI does
not use HMENU**; it provides the **in-app `MenuBar`** control (a normal templated control in
the client area).

### 7.2 The command/menu surface Uno already has

Uno already implements the full WinUI in-app menu + commanding stack that the design
reuses (verified §8.7):

- **`MenuBar` / `MenuBarItem` / `MenuFlyout` / `MenuFlyoutItem` / `MenuFlyoutSubItem` /
  `ToggleMenuFlyoutItem` / `RadioMenuFlyoutItem` / `MenuFlyoutSeparator`** — Skia-rendered,
  the in-app rendering target for `AppMenuBar` on Windows/Linux-no-native.
- **`XamlUICommand` / `StandardUICommand` (+ `StandardUICommandKind`)** — the commanding
  vocabulary, with `CommandingHelpers` auto-populating Label/Icon/KeyboardAccelerator from
  a command onto a menu item.
- **`KeyboardAccelerator`** (`Key` + `VirtualKeyModifiers`) — the accelerator type reused
  literally (no Ctrl→Cmd remap).
- **`IconSource` / `IconElement`** family (`BitmapIconSource`, `ImageIconSource`,
  `SymbolIconSource`, `FontIconSource`, `PathIconSource`) — the icon vocabulary, translated
  best-effort to native images on Apple/Linux.

### 7.3 Recommendation

Win32 impl is a **no-op** with `IsExported = false`; the in-app `AppMenuBar` renders the
real `MenuBar`. **No HMENU** — it would be per-window, non-Fluent, and a styling dead-end.
Windows is a designed-for extension point (the seam exists) but ships as in-app only.

---

## 8. Uno inventory (verified integration points)

Every claim here was traced in the live source. No native-menu integration exists today —
this is greenfield on top of the surfaces below.

### 8.1 `Window` is **not** a DependencyObject

[`Window.cs`](../../src/Uno.UI/UI/Xaml/Window/Window.cs) — `Window` does not derive from
`DependencyObject`. Consequence: Avalonia's `NativeMenu.Menu` attached-property-on-window
**does not port**; window-scoped attachment must be a static setter
(`NativeMenu.SetMenu(Window, …)`) backed by a `ConditionalWeakTable<Window, NativeMenu>`.

### 8.2 `Application` is **not** a DependencyObject

[`Application.cs`](../../src/Uno.UI/UI/Xaml/Application.cs) — likewise. App-wide attachment
is `NativeMenu.SetApplicationMenu(…)` into a static side-table, not an attached property.

### 8.3 Existing bootstrap NSMenu (macOS)

[`UNOApplication.m:95-124`](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m) —
on launch, if `app.mainMenu == nil`, Uno builds a minimal menu: an app menu with **Quit
(`terminate:`, Cmd+Q)** and a File menu with **Close Window (`performClose:`, Cmd+W)**. The
projection seam **replaces/extends** `app.mainMenu`; this confirms the "framework-guaranteed
minimal app menu" baseline already exists and just needs to be driven by the model.

### 8.4 macOS modifier mapping (shortcut direction)

[`UNOWindow.m:874-890`](../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOWindow.m) —
`get_modifiers` maps live `NSEventModifierFlags` to `VirtualKeyModifiers`:
`Command → Windows`, `Control → Control`, `Option → Menu` (Alt), `Shift → Shift`. This is the
**same direction** the menu accelerator translation must use; a menu-only remap would be
incoherent with live key events. This is the evidence behind spec decision 5 (reuse
KeyboardAccelerator modifiers literally; `VirtualKeyModifiers.Windows` = Cmd).

### 8.5 iPadOS `BuildMenu` seam

[`UnoUIApplicationDelegate.cs:11-12`](../../src/Uno.UI.Runtime.Skia.AppleUIKit/UnoUIApplicationDelegate.cs) —
`UnoUIApplicationDelegate : UIApplicationDelegate` is a `UIResponder`, so it can override
**`BuildMenu(IUIMenuBuilder)`**. It already observes `UIScene` activation notifications
(lines 27-43), which the per-scene-focus path would hook when multi-scene lands. This is the
iPadOS projection seam.

### 8.6 `UseUIApplicationDelegate<T>` override hook

[`AppleUIKitHostBuilder.cs:22-28`](../../src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/AppleUIKitHostBuilder.cs) —
devs supply a custom delegate type via `UseUIApplicationDelegate<T>()` (where
`T : UnoUIApplicationDelegate`), so the framework's `BuildMenu` override composes with
app-supplied delegates.

### 8.7 Single-window TODO (multi-scene gap)

[`NativeWindowFactoryExtension.cs:16-17`](../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/NativeWindowFactoryExtension.cs) —
`SupportsMultipleWindows => false` with an explicit TODO: *"While supported by the OS, we
currently only support single window. Later switch to … SupportsMultipleScenes."* This is
the concrete reason iPadOS v1 is **app-wide menu only**; per-scene override is deferred but
the design accommodates it.

### 8.8 ApiExtensibility registration pattern

The projection seam follows the established `ApiExtensibility` pattern (platform-targeting
rule 5): the core resolves `ApiExtensibility.CreateInstance<INativeMenuExtension>()`; each
host registers a concrete impl at startup.

- AppleUIKit registers in
  [`ExtensionsRegistrar.cs:31-38`](../../src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/ExtensionsRegistrar.cs)
  via `ApiExtensibility.Register(typeof(IXxxExtension), o => new …Extension())`.
- macOS registers in
  [`MacOSWindowHost.cs:196-204`](../../src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs)
  (and `MacSkiaHost.cs:66`) the same way. The macOS `NSMenu` impl additionally needs new
  native exports in the UnoNativeMac ObjC layer (e.g. `uno_window_set_main_menu` /
  `uno_app_set_main_menu` + build/update entry points).

### 8.9 Reusable commanding / menu / icon types

[`CommandingHelpers.cs`](../../src/Uno.UI/UI/Xaml/Controls/MenuFlyout/CommandingHelpers.cs)
already bridges `XamlUICommand`/`StandardUICommand` → menu-item Label/Icon/KeyboardAccelerator
(the same auto-populate the native model wants). `StandardUICommand` /
[`StandardUICommandKind`](../../src/Uno.UI/UI/Xaml/Input/StandardUICommandKind.cs),
`XamlUICommand`, `KeyboardAccelerator`, the `MenuBar`/`MenuFlyout*` family, and the
`IconSource`/`IconElement` family are all implemented and reusable — the design adds **no new
commanding or icon vocabulary**, only the `NativeMenu`/`NativeMenuItem` data model and the
`Role`/`ToggleType` enums.

---

## 9. Cross-platform capability matrix

Rows are capabilities; cells are the native behavior. "in-app" = handled by the in-app
`AppMenuBar`/`MenuBar` fallback, not a native menu.

| Capability | macOS | iPadOS | Linux (DBusMenu) | Windows |
|------------|-------|--------|------------------|---------|
| **App-wide menu** | Yes (`NSApp.mainMenu`) | Yes (`buildMenu`) | No (per-window only) | No native |
| **Per-window menu** | Yes (swap on key) | No (single-scene v1) | Yes (the only model) | per-control (in-app) |
| **Submenu** | Yes | Yes | Yes | in-app |
| **Separator** | Yes | Yes (inline group) | Yes | in-app |
| **Checkable** | Yes (`state`) | Yes (on/off/mixed) | Yes | in-app |
| **Radio** | Framework-coordinated | Inline single-selection | `toggle-type=radio` | in-app |
| **Icon** | Yes (`NSImage`) | Yes (`UIImage`/SF) | Partial (name/data) | in-app |
| **Shortcut** | Yes (`keyEquivalent`) | Yes (`UIKeyCommand`) | Partial (panel-dependent) | in-app accelerators |
| **Enable / disable** | Yes (push) | Yes (push) | Yes (push) | Yes (push) |
| **Visibility** | Yes (`isHidden`) | Yes | Yes | Yes |
| **Dynamic update** | Live + JIT | Rebuild-only | Re-export | Live (in-app) |
| **OS std items** | Yes | Yes | No | No |
| **Custom-view items** | Yes (out of scope) | No | No | No |
| **`IsExported`** | true | true | true (X11+registrar) / false | **false** |

---

## 10. Design implications — findings → decisions

| Finding | Design decision (spec §) |
|---------|--------------------------|
| Model must be a portable data tree, not visuals; Avalonia & Flutter both use non-visual models | **Lightweight `NativeMenu`/`NativeMenuItem` : DependencyObject** (decision 1), reusing existing `XamlUICommand`/`KeyboardAccelerator`/`IconSource` vocabulary (§7.2, §8.9) |
| Window & Application are **not** DOs (§8.1–8.2), so Avalonia's attached-property-on-window does not port | **Code-first static setters** `SetMenu(Window,…)`/`SetApplicationMenu(…)` over a `ConditionalWeakTable` (decision 3) |
| A1: app-scope (macOS/iPadOS) vs window-scope (Linux/macOS-multiwindow) | **Both-scope API + focused-window-wins fallback** (decision 6); macOS swaps `NSApp.mainMenu` on key; iPadOS app-wide v1 (§5.6, §8.7) |
| A2: pull-validation assumes native first responder, which Uno never is (§4.3, §5.3) | **Push effective-enabled** (`IsEnabled && CanExecute`), `autoenablesItems=NO`; `Role` does **not** bridge edit commands to the responder chain (decision 4, BANKED enablement) |
| A4: only Apple has OS-owned items (§4.4, §5.1); Flutter's `PlatformProvidedMenuItem` enum | **Thin slot-marker `NativeMenuItemRole` enum** (decision 4); macOS auto-wires AppKit selectors, others render role as a labeled item + dev `Command` |
| A3: iPadOS & Linux are rebuild-only (§5.2, §6.3); Avalonia uses `QueueReset` | **Observable model + coalesced full rebuild** + `NativeMenu.Opening` JIT (decision 7) |
| macOS bootstrap menu already guarantees Quit/Close (§8.3); Avalonia `PopulateStandardOSXMenuItems` | **Framework-guaranteed minimal app menu** (Quit+Hide); customize via `Role=ApplicationMenu` child merge (decision 8) |
| Modifier mapping is already `Command→Windows` for live keys (§8.4) | **Reuse `KeyboardAccelerator` modifiers literally**, no Ctrl→Cmd remap (decision 5) |
| Seam must serve macOS/iPadOS now, Linux/Windows later; Avalonia's single exporter + `MenuTarget` | **`INativeMenuExtension`** over `ApiExtensibility` with a `scope` arg + `IsSupported`/`IsRoleSupported` probe (§8.8); Win32 no-op `IsExported=false`, Linux fail-silent post-v1 |
| Flutter silently degrades with no fallback; Avalonia's `NativeMenuBar` collapses/renders | **Toolkit `AppMenuBar : MenuBar`** renders in-app where no native menu, translates+collapses on Apple (decision 2) — separate repo, one-way dependency on the core seam |
| Avalonia reuses the model for `TrayIcon`/`Dock` (§2.3) | **Tray-ready model shaping** (BANKED), not built v1 |

---

## 11. Assumptions

- **A-1**: Uno AppleUIKit remains single-scene through v1; multi-scene per-window menu
  override is deferred (§8.7). The model's scope argument is forward-compatible.
- **A-2**: macOS multi-window is in scope for v1 (swap-on-key is a requirement, not a
  future), since macOS multi-window already exists in Uno.
- **A-3**: Linux and Windows native backends are **post-v1**; only the seam and the in-app
  fallback path are designed (not shipped) for them.
- **A-4**: Icon translation is **documented best-effort** — bitmap/image sources translate
  cleanly; symbol/font sources are rendered-glyph best-effort with an SF-Symbol passthrough
  on Apple. Apps should not assume pixel-fidelity icons in native menus.
- **A-5**: Native menu APIs are main-thread; all model mutations marshal to the UI thread
  before the coalesced rebuild posts to the dispatcher.
- **A-6**: Accessibility (VoiceOver), dark-mode appearance, and RTL mirroring are inherited
  from the OS for free as a benefit of projecting to native menus — no Uno work required.
