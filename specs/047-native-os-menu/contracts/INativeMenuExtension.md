# Contract: `INativeMenuExtension` — native menu projection seam

**Feature:** 047-native-os-menu · **Status:** Draft · **Created:** 2026-06-30

This contract defines the **projection seam** between the platform-neutral core model
(`NativeMenu` / `NativeMenuItem`, in `Uno.UI`) and the per-platform Skia runtime hosts that
own the actual native menu system (AppKit `NSMenu`, UIKit `UIMenuBuilder`, DBusMenu, …).

It is the **only** boundary the core crosses to put a menu on screen. The core never references
AppKit/UIKit/D-Bus/Win32 types; it resolves a host-supplied implementation through Uno's
[ApiExtensibility](../../../doc/articles/uno-development/api-extensions.md) mechanism
(platform-targeting rule 5), exactly as `IApplicationViewExtension`, `IInputPaneExtension`,
`INativeWindowFactoryExtension` and friends already do.

Related artifacts: [spec.md](../spec.md) · [research.md](../research.md) ·
[data-model.md](../data-model.md) · [quickstart.md](../quickstart.md).

---

## 1. Purpose & placement

| Layer | Project | Type |
|-------|---------|------|
| Contract (this seam) | `src/Uno.UI` (generic `netX.0`, all Skia targets) | `INativeMenuExtension` interface + `NativeMenuScope` |
| macOS implementation | `src/Uno.UI.Runtime.Skia.MacOS` | `MacOSNativeMenuExtension` → `NSMenu` via `UnoNativeMac` ObjC |
| iPadOS/iOS implementation | `src/Uno.UI.Runtime.Skia.AppleUIKit` | `AppleUIKitNativeMenuExtension` → `UIMenuBuilder` |
| Linux implementation (post-v1) | `src/Uno.UI.Runtime.Skia.X11` | `X11NativeMenuExtension` → DBusMenu + Registrar |
| Win32 implementation | `src/Uno.UI.Runtime.Skia.Win32` | `Win32NativeMenuExtension` → **no-op**, `IsExported == false` |

**Why a seam and not `#if`:** `Uno.UI` ships as one generic `netX.0` assembly for **all** Skia
platforms (`RuntimeAssetsSelectorTask`). It cannot reference native menu APIs directly. The
concrete impl lives in the per-platform `Uno.UI.Runtime.Skia.*` host and is injected at startup.
This keeps the model testable, payload-light, and free of native references — and lets the
Toolkit `AppMenuBar` ask one question (`IsExported`) to decide whether to render in-app or
collapse.

The seam is **core-owned** and host-implemented. Direction is strictly one-way: hosts depend on
the core seam; the core never depends on a host.

---

## 2. Interface definition (proposed)

```csharp
#nullable enable

using System;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Identifies the scope a native menu is attached to: the whole application
/// (macOS NSApp.mainMenu / iPadOS app-global menu) or one specific window
/// (macOS swap-on-key, Linux per-window). Window is null for the application scope.
/// </summary>
internal readonly struct NativeMenuScope
{
	private NativeMenuScope(Microsoft.UI.Xaml.Window? window) => Window = window;

	/// <summary>The targeted window, or <c>null</c> for the application-wide menu.</summary>
	public Microsoft.UI.Xaml.Window? Window { get; }

	/// <summary>True when this scope targets the application-wide menu.</summary>
	public bool IsApplication => Window is null;

	/// <summary>The application-wide menu scope (macOS NSApp.mainMenu / iPadOS buildMenu).</summary>
	public static NativeMenuScope Application => new(null);

	/// <summary>A window-scoped menu (focused-window-wins; falls back to Application).</summary>
	public static NativeMenuScope ForWindow(Microsoft.UI.Xaml.Window window) =>
		new(window ?? throw new ArgumentNullException(nameof(window)));
}

/// <summary>
/// Projection seam between the platform-neutral <see cref="NativeMenu"/> model in Uno.UI and
/// the per-platform native menu system owned by a Skia runtime host. Resolved via
/// <c>ApiExtensibility.CreateInstance&lt;INativeMenuExtension&gt;()</c>; registered by each host.
/// All members are invoked on the UI/main thread (see §5).
/// </summary>
internal interface INativeMenuExtension
{
	/// <summary>
	/// Projects (or clears) the menu for the given scope, performing a full rebuild of the
	/// native menu tree. Passing <c>null</c> removes any native menu for that scope and, for the
	/// application scope on macOS, restores the framework-guaranteed minimal app menu (§4.1).
	/// Idempotent: calling with the same model is allowed and re-projects.
	/// </summary>
	/// <param name="scope">Application-wide or a specific window.</param>
	/// <param name="menu">The menu to project, or <c>null</c> to clear.</param>
	void SetMenu(NativeMenuScope scope, NativeMenu? menu);

	/// <summary>
	/// True when a native menu is currently <b>visible to the user</b> on this platform (macOS
	/// menu bar, the iPadOS 26 always-available bar, Linux global menu when a registrar accepted
	/// us). Supplying menu content is NOT sufficient: where the OS shows no menu surface at all
	/// (iPhone) or only a transient keyboard-gated one (iPadOS 15-25), this is <c>false</c>, so
	/// the in-app fallback stays on screen.
	/// On Win32 this is always false. May change at runtime (Linux registrar appears/disappears);
	/// see <see cref="IsExportedChanged"/>.
	/// Surfaced publicly as <c>NativeMenu.IsExported</c>, which is what the Toolkit AppMenuBar
	/// reads to decide in-app render (false) vs. collapse (true) — it lives in a different
	/// assembly and cannot see this interface.
	/// </summary>
	bool IsExported { get; }

	/// <summary>
	/// True when assigning a menu on this host right now would actually project it — i.e. the
	/// platform has a native menu system AND whatever it needs to accept a menu is in place.
	/// On Linux this means a DBusMenu registrar is present: no registrar, no support. Win32
	/// returns false; macOS/iPadOS return true.
	/// Distinct from <see cref="IsExported"/> only in that a supported host may have no menu
	/// assigned yet — support is about capability, export is about a menu being on screen.
	/// </summary>
	bool IsSupported { get; }

	/// <summary>
	/// Reports whether a given OS slot role projects to a real native slot on this platform.
	/// Lets apps adapt (e.g. hide a "Services" item where unsupported). See §6.
	/// Roles that are unsupported still render as plain labeled items where the host shows a menu.
	/// </summary>
	bool IsRoleSupported(NativeMenuItemRole role);

	/// <summary>
	/// Raised when <see cref="IsExported"/> transitions (e.g. a Linux global-menu registrar
	/// appears/disappears, or a key window with/without a menu becomes focused). Raised on the
	/// UI thread. The Toolkit AppMenuBar subscribes to re-evaluate its in-app vs. collapsed state.
	/// </summary>
	event EventHandler? IsExportedChanged;
}
```

**Notes on the surface**

- `internal` visibility matches the other extension seams (`IApplicationViewExtension`,
  `IInputPaneExtension`). Public attachment is the `NativeMenu.SetMenu(...)` static API
  ([data-model.md](../data-model.md)); this interface is the private plumbing behind it.
- `NativeMenuScope` is a value type so the application scope needs no sentinel object. `Window`
  is the WinUI/Uno `Microsoft.UI.Xaml.Window` (not a `DependencyObject`, hence the side-table in
  the core — see §5 lifecycle).
- The event uses `EventHandler` (never `event Action`), per repo convention.
- The seam intentionally exposes **no** per-item / incremental methods. Updates are a coalesced
  **full rebuild** (§5.3); native-side diffing is the host's private concern and is not modeled
  here (iPadOS and DBusMenu are rebuild-only).

---

## 3. Registration & resolution

### 3.1 Host registration

Each Skia host registers its implementation during startup, alongside its other
`ApiExtensibility.Register(...)` calls (e.g. `ExtensionsRegistrar.Register()` for AppleUIKit,
`MacOSHost` for macOS). The factory receives the calling owner object (unused here):

```csharp
// src/Uno.UI.Runtime.Skia.MacOS — host startup
ApiExtensibility.Register(
	typeof(INativeMenuExtension),
	_ => MacOSNativeMenuExtension.Instance);
```

```csharp
// src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/ExtensionsRegistrar.cs — add to Register()
ApiExtensibility.Register(
	typeof(INativeMenuExtension),
	_ => AppleUIKitNativeMenuExtension.Instance);
```

The extension is a **host singleton** (one native menu system per process), so the factory
returns a shared `Instance` rather than allocating per call — matching the
`AppleUIKitImeTextBoxExtension.Instance` / `…InputSource.Instance` style already in the
AppleUIKit registrar. Win32 and X11 register their own singletons the same way.

> Hosts that ship as packages can instead use the assembly-level
> `[ApiExtension(typeof(INativeMenuExtension), typeof(MyImpl))]` attribute; `App.InitializeComponent()`
> emits the equivalent `Register` call. In-repo Skia hosts use the explicit imperative form above.

### 3.2 Core resolution

The core resolves the extension lazily and caches it. Resolution failure (no host registered for
this platform, or a host that opted out) is **not** an error — the core simply treats native
menus as unavailable, and the Toolkit `AppMenuBar` renders fully in-app.

```csharp
// Core side-table type that backs NativeMenu.SetMenu / SetApplicationMenu.
private static INativeMenuExtension? _resolved;
private static bool _resolveAttempted;

private static INativeMenuExtension? TryGetExtension()
{
	if (!_resolveAttempted)
	{
		// A failed resolution is cached too: no native host on this platform means
		// in-app fallback only, and retrying on every call would be pure overhead.
		_resolveAttempted = true;
		ApiExtensibility.CreateInstance<INativeMenuExtension>(typeof(NativeMenu), out _resolved);
	}

	return _resolved;
}
```

The four public probes on `NativeMenu` all forward through `TryGetExtension()` and degrade to
`false` when no host is registered, so callers outside `Uno.UI` never touch this `internal`
interface:

```csharp
public static bool IsSupported => TryGetExtension()?.IsSupported ?? false;
public static bool IsRoleSupported(NativeMenuItemRole role) =>
	TryGetExtension()?.IsRoleSupported(role) ?? false;
public static bool IsExported => TryGetExtension()?.IsExported ?? false;

// Forwarded from the extension; no-ops (and never leaks a subscription) when none resolved.
public static event EventHandler? IsExportedChanged;
```

---

## 4. Per-host responsibilities

Each host translates the **same** neutral `NativeMenu` tree into its native vocabulary. The host
owns: the native object lifetime, enable/visibility pushing, role→OS-slot wiring, icon
translation, accelerator mapping, and the `Opening`/`Closed` event bridge. Shared expectations:

- `NativeMenuItem` effective-enabled = `IsEnabled && (Command?.CanExecute(CommandParameter) ?? true)`
  is computed by the **core** and pushed; the host must disable native auto-validation so the
  pushed state is authoritative (macOS `autoenablesItems = NO`).
- Activation: host invokes the item's `Click` event **and** `Command.Execute(CommandParameter)`
  on the UI thread when the native item fires.
- `NativeMenu.NeedsUpdate` fires before a submenu is shown (just-in-time population), then
  `Opening` as a content-settled notification, then `Closed` after dismissal.

### 4.1 Skia.MacOS — `NSMenu` (v1, primary)

- Builds `NSMenu`/`NSMenuItem` trees through new `UnoNativeMac` ObjC exports (e.g.
  `uno_app_set_main_menu`, `uno_window_set_main_menu`, plus build/update entry points). Replaces
  and extends the **bootstrap** `NSMenu` currently created in
  [`UNOApplication.m:156-185`](../../../src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOApplication.m)
  (Quit ⌘Q / Close Window ⌘W).
- **Framework-guaranteed app menu:** the host ALWAYS ensures the bold app-name menu exists with at
  least **Quit (⌘Q)** and **Hide**, placed first; developer top-level menus follow. A developer
  top-level item with `Role = ApplicationMenu` merges its children (About, Settings, …) into that
  app menu rather than adding a second one.
- **OS-owned roles auto-wire to AppKit selectors:** `Quit → terminate:`, `Hide → hide:`,
  `HideOthers → hideOtherApplications:`, `ShowAll → unhideAllApplications:`,
  `About → orderFrontStandardAboutPanel:`, `Services →` the Services submenu,
  `Minimize → performMiniaturize:`, `Zoom → performZoom:`,
  `EnterFullScreen → toggleFullScreen:`. Note `performMiniaturize:`, `performZoom:` and
  `toggleFullScreen:` are `NSWindow` methods reached through nil-target responder dispatch, not
  `NSApplication` ones — the menu item carries a `nil` target and AppKit routes to the key window.
- **Slot-container roles are not selectors:** `ApplicationMenu`, `Window` and `Help` mark *where*
  a menu sits (and let AppKit adopt it as the Window/Help menu); their children supply the items.
  They are listed separately from the auto-wired set in
  [data-model.md](../data-model.md#nativemenuitemrole) and behave the same way here.
  **Edit roles** (`Cut/Copy/Paste/Undo/Redo/SelectAll/Delete`) are **placement + standard label
  only** — the developer supplies `Command` and enable logic (Uno is not a native first responder;
  there is no responder-chain bridging).
- `autoenablesItems = NO`; the core pushes effective-enabled and checked state.
- **Per-window swap-on-key (v1):** on `windowDidBecomeKey`, swap `NSApp.mainMenu` to that window's
  menu (if it set one) else the Application menu; on `windowDidResignKey`/close, restore. macOS
  multi-window is supported. `IsExported` is `true` whenever a main menu is installed.
- Live updates apply incrementally on the native side from a full rebuild request; macOS supports
  live mutation.

### 4.2 Skia.AppleUIKit — `UIMenuBuilder` (v1)

- Overrides `BuildMenu(IUIMenuBuilder)` on a **`UIResponder`** in the chain. ⚠️ **Not on
  [`UnoUIApplicationDelegate`](../../../src/Uno.UI.Runtime.Skia.AppleUIKit/UnoUIApplicationDelegate.cs)**,
  which earlier drafts of this contract assumed: it derives from `UIApplicationDelegate`, an
  `NSObject`-derived class in .NET for iOS, **not** a `UIResponder`, so `BuildMenu` cannot be
  overridden there. Host the override on a `UIApplication` subclass passed as the principal class
  to `UIApplication.Main`
  ([`AppleUIKitHost.cs:44`](../../../src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/AppleUIKitHost.cs)
  currently passes `null` for it) or on
  [`RootViewController`](../../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/RootViewController.cs)
  (a `UINavigationController`, hence a `UIResponder`). The former better matches an app-global
  menu; **confirm on device which responder UIKit consults before finalising** — see
  research.md §8.5. Developers still supply their own delegate via `UseUIApplicationDelegate<T>`
  ([`AppleUIKitHostBuilder.cs`](../../../src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/AppleUIKitHostBuilder.cs)),
  which remains the hook for the scene-focus path.
  The host's `BuildMenu` reads the current model and emits `UIMenu`/`UICommand`/`UIKeyCommand`.
  The backend targets the **iPadOS 26 always-available, macOS-like system menu bar** — a
  first-class menu bar that the OS reveals via swipe-from-top (also pointer-to-top, or Globe+M)
  and that works **without a hardware keyboard**. The underlying `UIMenuBuilder` /
  `UIResponder.buildMenu(with:)` API is unchanged; iPadOS 26 simply presents the same content as
  an always-on bar. On earlier iPadOS (15–25) the identical content surfaces as the older
  transient menu shown with a hardware keyboard (⌘-hold). We supply the content; the OS presents
  it per version. iPhone shows no menu bar (`UIKeyCommand`s still work with a hardware keyboard).
- **Discoverability:** the iPadOS 26 bar is meant to display **all** app commands, including
  unavailable ones, so users can discover capabilities. The host therefore keeps disabled
  commands **visible but disabled** (does not remove them), matching our push-based enablement —
  prefer toggling `IsEnabled` over `IsVisible = false` / removal for menu commands.
- **App-wide only in v1.** Uno AppleUIKit is single-window/single-scene
  ([`NativeWindowFactoryExtension.cs:16`](../../../src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/NativeWindowFactoryExtension.cs)),
  so `SetMenu(ForWindow(w), …)` is treated as the application menu. Per-scene override is deferred
  until Uno gains multi-scene; the seam already accommodates it via `NativeMenuScope`.
- `SetMenu(...)` requests a rebuild via `UIMainMenuSystem.Shared.SetNeedsRebuild()` (the new
  `UIMenuSystem` subclass that drives the iPad menu bar on iOS/iPadOS 26), falling back to
  `UIMenuSystem.Main.SetNeedsRebuild()` on earlier OS; UIKit then calls back into `BuildMenu`.
  Rebuild-only — no incremental diffing.
- **`NeedsUpdate` granularity differs from macOS and this is observable.** AppKit has a
  per-submenu `menuNeedsUpdate:` that fires only for the menu about to open. UIKit has no
  equivalent: `buildMenu(with:)` rebuilds the whole tree, so *every* menu's `NeedsUpdate` fires on
  *every* rebuild, whether or not the user is opening that menu. A handler doing real work — the
  quickstart's Recent-Files example touches the disk — therefore runs once per submenu open on
  macOS but once per whole-tree rebuild on iPadOS. Hosts MUST document this, and handler authors
  MUST keep `NeedsUpdate` cheap and idempotent. The in-flight guarantee in FR-016 applies to hosts
  with a synchronous per-menu update callback (macOS, DBusMenu `AboutToShow`); on iPadOS the
  guarantee is satisfied trivially because the handler runs during the rebuild that consumes it.
- OS std items map to UIKit standard command identifiers where available (About/Settings via app
  menu group, Services N/A on iPadOS); edit roles map to `UIResponderStandardEditActions` only
  when the developer supplies a `Command`.
- `IsExported` reflects whether the user can actually **see** a menu bar, not merely that content
  was supplied. Content is always supplied; the surface is not. On **iPadOS 26** the bar is
  always available (OS-revealed via swipe-from-top, no hardware keyboard) → `true`. On **iPadOS
  15-25** the same content only surfaces as a transient ⌘-hold menu → `false`, because a user
  with no hardware keyboard would otherwise be left with nothing. On **iPhone** there is no menu
  bar at all → `false` (`UIKeyCommand` shortcuts still work). There is no force-show API.
  Reporting `true` on those last two would make the Toolkit `AppMenuBar` collapse its in-app bar
  and leave the app with **no menu whatsoever** — the same silent failure the Linux
  `IsSupported` rule (§6) exists to prevent.

### 4.3 Skia.X11 — DBusMenu (post-v1, designed-for)

- Exports a `com.canonical.dbusmenu` server and registers it with
  `com.canonical.AppMenu.Registrar` **keyed by the X11 window XID**. Per-window is the native model.
- **Fail-silent:** if no registrar is present (Wayland, GNOME without the extension), registration
  fails quietly, **both `IsSupported` and `IsExported` stay `false`** (§6), and the Toolkit
  `AppMenuBar` renders the real in-app `MenuBar`. The host tracks the registrar's bus name via a
  `NameOwnerChanged` subscription — **never a poll** — and raises `IsExportedChanged` when it
  appears or disappears at runtime.
- Toggle/radio map to DBusMenu `toggle-type`; icons are partial (`icon-name` / `icon-data`);
  shortcuts are panel-dependent. Updates re-export the menu (rebuild).

### 4.4 Win32 — no-op

- `IsSupported == false`, `IsExported == false`, `SetMenu` is a no-op, `IsRoleSupported` returns
  `false` for all roles. No `HMENU`. The Toolkit `AppMenuBar` always renders the real in-app
  `MenuBar` on Win32. (A native HMENU/ribbon path is explicitly out of scope; revisit post-v1.)

---

## 5. Threading, lifecycle & rebuild contract

### 5.1 Threading

- Native menu APIs are **main-thread** (AppKit/UIKit). Every `INativeMenuExtension` member is
  invoked **on the UI thread**. The core marshals model mutations (which may arrive on any thread
  via `DependencyProperty` callbacks / `INotifyCollectionChanged`) onto the dispatcher before
  calling `SetMenu`. Hosts may assume main-thread affinity and must not re-marshal.

### 5.2 Lifecycle

- **Attachment:** `NativeMenu.SetMenu(window, menu)` / `SetApplicationMenu(menu)` store the
  model in a core `ConditionalWeakTable` keyed by `Window` (Window is **not** a
  `DependencyObject`), then call `extension.SetMenu(scope, menu)`.
- **Window close → unregister:** when a window closes, the core calls
  `extension.SetMenu(ForWindow(closingWindow), null)` and drops the side-table entry, so the host
  releases the native menu and (macOS) stops swapping it on key. The `ConditionalWeakTable` lets a
  collected window's entry GC naturally; explicit close is the deterministic path.
- **App menu replace:** `SetApplicationMenu(newMenu)` replaces the previous application model and
  re-projects; `SetApplicationMenu(null)` restores the framework-guaranteed minimal menu (macOS)
  or clears (iPadOS).
- **Re-attachment** of the same `NativeMenu` instance to a different scope is allowed; the core
  detaches it from the old scope first (a menu projects to at most one scope at a time).

### 5.3 Coalesced full-rebuild contract

- The model is **observable**: `DependencyProperty` change callbacks on `NativeMenuItem` plus
  `INotifyCollectionChanged` on `NativeMenu.Items`. Any change marks the owning menu **dirty**.
- The core **coalesces** dirty notifications onto a single dispatcher post (one rebuild per frame,
  not per property), then calls `extension.SetMenu(scope, menu)` **once** with the whole tree.
- `SetMenu` performs a **full rebuild** of that scope's native menu (Avalonia-style reset). Hosts
  must make this **idempotent** and safe to call repeatedly; they may diff internally but must not
  require the core to send deltas.
- **Just-in-time population:** `NativeMenu.NeedsUpdate` is raised by the host immediately before a
  submenu is displayed (maps to `NSMenuDelegate.menuNeedsUpdate`, DBusMenu `AboutToShow`, iPadOS
  rebuild), letting handlers populate/refresh children right before they're shown. The host must
  raise it **synchronously, inside the native update callback**, and read `Items` *after* it
  returns, so handler mutations land in the build already in flight rather than scheduling a
  second one. `Opening` is raised afterwards as a notification only (maps to
  `NSMenuDelegate.menuWillOpen`) and `Closed` after dismissal; mutations from those two are
  ordinary model changes that go through the normal coalesced path.

---

## 6. Capability-probe semantics & Toolkit usage

Four public probes let both apps and the Toolkit adapt to the host:

| Probe | Question | Win32 | macOS | iPadOS 26 | iPadOS 15–25 | iPhone | Linux, registrar | Linux, none |
|---|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `IsSupported` | Would a menu assigned **right now** be projected? | ✗ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ |
| `IsExported` | Is a native menu **visible to the user right now**? | ✗ | ✓ (installed) | ✓ (always-available bar) | ✗ (⌘-hold only) | ✗ (no bar) | ✓ if accepted | ✗ |
| `IsRoleSupported(role)` | Does this OS slot project natively? | ✗ all | per role (§4.1) | per role | per role | per role | per role | ✗ all |
| `IsExportedChanged` | *(event)* fires when `IsExported` flips | — | on key-window swap | — | — | — | on registrar appear/disappear | — |

Note the deliberate split on Apple: `IsSupported` is `true` across all three Apple columns —
UIKit accepts and honours the menu content everywhere, and `UIKeyCommand` shortcuts work even on
iPhone — while `IsExported` tracks whether a **bar is actually on screen**. An app choosing
whether to *build* a menu wants the first; a control choosing whether to *hide its own* wants the
second.

**`IsSupported` is a "would it work right now" probe, not a "was this platform built with menus
in mind" probe.** Linux without a DBusMenu registrar reports `IsSupported == false`, exactly like
Win32. This is the semantics developer-facing guidance depends on: `quickstart.md §8` branches
straight on `IsSupported` to decide between assigning a native menu and showing an in-app menu
bar, so if Linux-without-registrar reported `true` that `else` branch would never run, the
`SetApplicationMenu` call would no-op against an absent registrar, and the app would end up with
**no menu at all** — a silent failure. Because a registrar can appear or disappear mid-session,
`IsSupported` can change at runtime on Linux and flips alongside `IsExportedChanged`.

`IsSupported` vs `IsExported`: the difference is only whether a menu has actually been assigned
and accepted. A supported host with no menu set reports `IsSupported == true`,
`IsExported == false`. The Toolkit decision hinges on **`IsExported`**, not `IsSupported`,
because the in-app `MenuBar` must appear exactly when nothing native is on screen.

### Toolkit `AppMenuBar` flow (Uno.Toolkit.UI)

`AppMenuBar : MenuBar` uses the seam indirectly through the public `NativeMenu.SetMenu(...)` API:

1. On load, translate its `MenuBarItem` / `MenuFlyoutItem` content (+ `AppMenu.Role` attached
   property → `NativeMenuItem.Role`) into a `NativeMenu`, and call
   `NativeMenu.SetMenu(window, translatedMenu)`.
2. Read the public `NativeMenu.IsExported` bridge (the Toolkit is a separate assembly and cannot
   see `INativeMenuExtension`):
   - **`IsExported == true`** (macOS, iPadOS 26, Linux-with-registrar): the native bar shows the
     menu — **collapse to zero in-app footprint** (don't render the `MenuBar` chrome).
   - **`IsExported == false`** (Win32, Wayland/GNOME-no-registrar, iPhone, iPadOS 15–25 with no
     hardware keyboard): **render normally** as a real in-app `MenuBar`.
3. Subscribe to `NativeMenu.IsExportedChanged` and re-evaluate step 2 (e.g. a Linux registrar
   appears, or a key window's menu changes) so the in-app bar appears/disappears without a reload.

This keeps the **core** free of any Toolkit dependency: the Toolkit consumes the seam; the seam
never knows the Toolkit exists.

---

## 7. Out of scope for this contract

- The public `NativeMenu` / `NativeMenuItem` model surface — see [data-model.md](../data-model.md).
- The Toolkit `AppMenuBar` control and `AppMenu.Role` attached property — separate Toolkit issue.
- Native-side ObjC/UIKit/D-Bus payload formats — host implementation detail behind `SetMenu`.
- System-tray / notification-area menus — the model is shaped to allow reuse later (Avalonia
  precedent) but no tray seam is defined in v1.
