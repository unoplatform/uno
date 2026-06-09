# Soft Input Keyboard Handling for Uno Skia Targets

## Context

**Issues**: [#22264](https://github.com/unoplatform/uno/issues/22264) (WASM Skia soft keyboard content cutoff), [#5407](https://github.com/unoplatform/uno/issues/5407) (RootScrollViewer feature request), uno-private#1154 (Make InputPane per-window)

**Problem**: When a soft keyboard appears on mobile devices running Uno Skia apps, focused elements (TextBox, PasswordBox, etc.) are not properly scrolled into view, and content behind the keyboard becomes inaccessible. Additionally, on WASM Skia, the keyboard triggers `window.resize` events that cause flyouts to reposition/dismiss, making text input in flyouts impossible.

**Root causes**:
1. **No RootScrollViewer** - WinUI wraps all app content in a special ScrollViewer that enables whole-page scrolling when the keyboard appears. Uno has this stubbed (`VisualTree.RootScrollViewer` property exists, `ContentManager` has a `// TODO: Add RootScrollViewer everywhere` comment) but never implemented.
2. **WASM Skia has no keyboard detection** - No `IInputPaneExtension` registered, no `visualViewport` API usage. `InputPane.OccludedRect` is never set. The browser's viewport resize from keyboard appearance is indistinguishable from a real window resize.
3. **Current `Pad` mechanism is insufficient alone** - The `ScrollContentPresenter.Pad()` approach only works when a ScrollViewer ancestor exists. Pages without a ScrollViewer have no way to scroll content into view.

**Goal**: Match WinUI behavior where the keyboard is treated as an overlay, a RootScrollViewer enables whole-page scrolling, and focused elements are automatically brought into view.

---

## WinUI Reference Behavior (from C++ source analysis)

### Visual Tree Structure (WinUI3 Island-Based Architecture)

WinUI3 uses an island-based architecture. Each `CXamlIslandRoot` owns its own `CContentRoot` with an independent `VisualTree`. The `ContentManager` creates the RSV per island/window:

**Per Island/Window VisualTree:**
```
CRootVisual (hidden root, per-island)
  +-- RootScrollViewer (CScrollContentControl)
  |     +-- ScrollContentPresenter
  |           +-- Border (sized to window bounds)
  |                 +-- [PublicRootVisual - Frame/Page/app content]
  +-- PopupRoot (sibling, unaffected by RSV)
  +-- TransitionRoot
  +-- FullWindowMediaRoot
  +-- ConnectedAnimationRoot
  +-- VisualDiagnosticsRoot
```

**Main tree additionally has:**
```
CRootVisual (main tree root)
  +-- XamlIslandRootCollection (Panel, main tree only)
  |     +-- CXamlIslandRoot (each island, each with own VisualTree above)
  +-- PopupRoot (main tree popups)
  +-- ... other overlay roots
```

**Key points:**
- `CXamlIslandRoot` is a `CPanel` in the main tree but owns a **separate** VisualTree
- Each island's RSV lives in **that island's** hidden CRootVisual, not in the main tree
- `ContentManager::SetContent()` creates the RSV, then `XamlIslandRoot::SetPublicRootVisual()` delegates to `VisualTree::SetPublicRootVisual()` which places RSV in the island's tree via `AddVisualRootToRootScrollViewer()` + `AddRootScrollViewer()`
- RSV is stored in `VisualTree::m_rootScrollViewer` (per-island)

**Uno's parallel structure:**
- Main window: `RootVisual` is the `RootElement`, children added via `AddRoot()`
- XamlIslandRoot: The island Panel itself IS the `RootElement`
- Both use `ContentManager` -> `VisualTree.SetPublicRootVisual()`
- RSV property exists but is always null today

### RootScrollViewer Behavior
- All scrolling **disabled** by default (VerticalScrollMode=Disabled, HorizontalScrollMode=Disabled, hidden scroll bars)
- Sized to window bounds, no visual template, not focusable
- Suppresses pointer/keyboard/focus events when SIP is NOT showing
- Only becomes interactive when soft keyboard is present

### When SIP (Soft Input Panel) Shows
1. `CInputPaneHandler::Showing()` receives occluded rectangle from OS
2. **Shrinks RootScrollViewer Height** to `sipTop - windowTop` (visible area above keyboard)
3. **Enables scrolling** on RSV (VerticalScrollMode=Enabled), saves pre-SIP scroll offsets
4. `CBringIntoViewHandler::EnsureFocusedElementBringIntoView()`:
   - Gets focused element, adjusts for caret position (75% threshold for large elements)
   - Adds 20px logical padding around element
   - Calls `focusedElement.BringIntoView(rect, forceIntoView=true)`
5. `BringIntoViewRequested` routed event bubbles up through ScrollViewers
6. Each ScrollViewer in the chain calls `MakeVisible()` -> `ScrollContentPresenter.MakeVisibleImpl()` which computes minimal scroll offset

### When SIP Hides
1. Restores RSV Height to original
2. Disables scrolling, restores pre-SIP scroll offsets

### When Focus Changes While SIP Is Showing
- `InputPaneProcessor::NotifyFocusChanged()` forces `bringIntoView=true` for text-editable controls
- Re-runs `EnsureFocusedElementBringIntoView()`

---

## Current Uno State

### What Works
| Platform | Keyboard Detection | OccludedRect | BringIntoView | Window Resize |
|----------|-------------------|--------------|---------------|---------------|
| Android Skia | LayoutProvider (dual PopupWindow) | Set correctly | Pad + StartBringIntoView | No (AdjustPan) |
| iOS Skia | UIKeyboard.Notifications | Set correctly | Pad + StartBringIntoView | No (overlay) |
| WASM Skia | **None** | **Never set** | Only TextBox.OnGotFocus | **Yes (broken)** |

### What's Missing
1. **RootScrollViewer**: `VisualTree.RootScrollViewer` is always `null`. `ContentManager.SetContent()` passes `rootScrollViewer: null`. `VisualTree.SetPublicRootVisual()` has the RSV insertion code commented out.
2. **WASM keyboard detection**: No `IInputPaneExtension`, no `visualViewport` API, no way to distinguish keyboard resize from real resize.
3. **Focus change re-scroll**: No mechanism to re-BringIntoView when focus moves between text controls while keyboard is showing.

### Key Existing Code
- `InputPane.cs` (line 92-99): After firing Showing/Hiding events, dispatches `EnsureFocusedElementInViewPartial()` if app didn't handle it
- `InputPane.skia.cs` (line 36-78): Finds focused element's parent SCP, calls `Pad()`, dispatches `StartBringIntoView()`
- `ScrollContentPresenter.OccludedPadding.cs`: `Pad()` computes intersection of SCP viewport with keyboard, adds bottom padding
- `ScrollContentPresenter.mux.cs` (line 102, 195, 237): `_occludedRectPadding` reduces effective viewport height in BringIntoView calculations
- `ContentDialog.Visuals.cs` (line 285-357): Independent keyboard adjustment via storyboard animations
- `InputManager.cs` (line 76-86): `NotifyFocusChanged()` calls `StartBringIntoView()` when focus changes with keyboard focus state

---

## Implementation Plan

### Phase 1: WASM Skia Keyboard Detection

**Goal**: Detect soft keyboard on WASM Skia using `visualViewport` API, set `InputPane.OccludedRect`, and prevent keyboard-triggered window resize from affecting layout.

#### 1.1 Add visualViewport handling in TypeScript

**File**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/WebAssemblyWindowWrapper.ts`

**Detection approach: Dual-signal (no pixel threshold)**
- Signal 1: Our invisible text `<input>`/`<textarea>` is focused (`document.activeElement` matches)
- Signal 2: `visualViewport.height` decreased compared to `window.innerHeight`
- When BOTH signals are true: keyboard is showing. Occluded rect height = `window.innerHeight - visualViewport.height`
- This eliminates false positives from browser chrome changes (address bar collapse) since those happen without our text input having focus

**Changes:**
- Add fields: `private _isKeyboardShowing: boolean`, `private _lastLayoutWidth: number`, `private _lastLayoutHeight: number`
- In `build()`: Subscribe to `window.visualViewport.resize` event (with feature check for `window.visualViewport`)
- Add `onVisualViewportResize()` method:
  - Check if `document.activeElement` is an `HTMLInputElement` or `HTMLTextAreaElement` (our invisible text input)
  - If focused AND `window.innerHeight - visualViewport.height > 0`: keyboard is showing
  - Call C# `OnInputPaneChanged(owner, 0, visualViewport.height, visualViewport.width, window.innerHeight - visualViewport.height)` with the occluded rect
  - When visual viewport restores OR text input loses focus: keyboard hiding, call with `(0, 0, 0, 0)`
- Modify `resize()` method:
  - Track the "true" layout size (before keyboard appeared) in `_lastLayoutWidth`/`_lastLayoutHeight`
  - When `_isKeyboardShowing` is true, report the stored pre-keyboard layout size instead of the current (potentially shrunken) size
  - This prevents XAML from thinking the window shrunk, which fixes the flyout issue

#### 1.2 Add C# InputPane callback

**File**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/Xaml/Window/WebAssemblyWindowWrapper.cs`

- Add `[JSExport] static void OnInputPaneChanged(object instance, double x, double y, double width, double height)`
- Convert to logical `Rect` and set `InputPane.GetForCurrentView().OccludedRect`

#### 1.3 Register IInputPaneExtension for WASM

**File (new)**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/ViewManagement/InputPaneExtension.cs`

- Implement `IInputPaneExtension` with `TryShow()` returning false (browser manages) and `TryHide()` blurring the active element

**File**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Hosting/WebAssemblyBrowserHost.cs`

- Register via `ApiExtensibility.Register(typeof(IInputPaneExtension), ...)`

#### 1.4 Flyout fix (comes for free)

With Phase 1.1, when the keyboard appears on WASM, the `window.resize` handler reports the **pre-keyboard** layout size. This means:
- `WebAssemblyWindowWrapper.OnResize()` sees no size change
- `XamlRoot.Changed` does NOT fire
- `PopupPanel.XamlRootChanged` does NOT fire
- Flyouts stay in place

No explicit flyout code changes needed.

---

### Phase 2: RootScrollViewer

**Goal**: Create a custom `RootScrollViewer` class (derived from `ScrollViewer`, matching WinUI) and insert it into the visual tree for Skia targets.

**Architecture context**: On Skia, the main window **always** uses the XamlIslandRoot path (via `DesktopWindow` -> `WindowChrome` -> `DesktopWindowXamlSource` -> `XamlIslandRoot`). The `CoreWindowWindow` / `isCoreWindowContent=true` path is **never used on Skia**. This means all RSV changes are in the XamlIslandRoot path only.

Flow: `XamlIslandRoot.Content` setter -> `ContentManager.SetContent()` (just stores) -> `XamlIslandRoot.SetPublicRootVisual()` -> `VisualTree.SetPublicRootVisual()`

#### 2.1 Create the RootScrollViewer class

**File (new)**: `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/RootScrollViewer.cs`

Create a custom subclass of `ScrollViewer` matching WinUI's `RootScrollViewer_Partial.h/.cpp`:

```csharp
internal class RootScrollViewer : ScrollViewer
{
    // SIP state
    private bool _isInputPaneShow;
    private double _preInputPaneOffsetX;
    private double _preInputPaneOffsetY;

    internal RootScrollViewer()
    {
        // Match WinUI: all scrolling disabled, hidden bars, not focusable
        VerticalScrollMode = ScrollMode.Disabled;
        HorizontalScrollMode = ScrollMode.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        ZoomMode = ZoomMode.Disabled;
        IsTabStop = false;
    }

    /// <summary>
    /// Returns true if this is the RootScrollViewer.
    /// Used by ScrollViewer base to suppress behaviors (pointer, keyboard, focus).
    /// </summary>
    internal override bool IsRootScrollViewer => true;

    /// <summary>
    /// Whether the input pane (soft keyboard) is currently showing.
    /// When true, scrolling is enabled on this RSV.
    /// </summary>
    internal bool IsInputPaneShow => _isInputPaneShow;

    /// <summary>
    /// Called when the InputPane state changes (showing/hiding).
    /// Enables/disables scrolling and saves/restores scroll offsets.
    /// </summary>
    internal void NotifyInputPaneStateChange(bool isShowing, Rect inputPaneBounds)
    {
        if (isShowing && !_isInputPaneShow)
        {
            // Save pre-SIP scroll offsets
            _preInputPaneOffsetX = HorizontalOffset;
            _preInputPaneOffsetY = VerticalOffset;

            // Enable scrolling while keyboard is showing
            VerticalScrollMode = ScrollMode.Enabled;
            HorizontalScrollMode = ScrollMode.Enabled;
        }
        else if (!isShowing && _isInputPaneShow)
        {
            // Disable scrolling
            VerticalScrollMode = ScrollMode.Disabled;
            HorizontalScrollMode = ScrollMode.Disabled;

            // Restore pre-SIP scroll offsets
            ChangeView(_preInputPaneOffsetX, _preInputPaneOffsetY, null, disableAnimation: true);
        }

        _isInputPaneShow = isShowing;
    }

    // Strip template (matching WinUI: CScrollContentControl::ApplyTemplate releases m_pTemplate)
    protected override void OnApplyTemplate()
    {
        // Do NOT call base.OnApplyTemplate() - skip all template part retrieval.
        // The RSV has no visual template; the ScrollContentPresenter is wired manually.
    }

    // Suppress automation peer (invisible to automation, matching WinUI)
    protected override AutomationPeer OnCreateAutomationPeer() => null;

    // TODO: Port WinUI's CBringIntoViewHandler caret adjustments:
    // - Caret position adjustment (75% CaretAlignmentThreshold when element > viewport)
    // - 20px logical padding (ExtraPixelsForBringIntoView) around focused element
    // - AppBar height adjustment via AdjustBringIntoViewRecHeight
    // See: D:\Work\microsoft-ui-xaml2\src\dxaml\xcp\core\input\BringIntoViewHandler.cpp
}
```

Additionally, add the `IsRootScrollViewer` virtual property to `ScrollViewer`:

**File**: `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs` (or appropriate partial)

```csharp
internal virtual bool IsRootScrollViewer => false;
```

And use it to suppress behaviors in `ScrollViewer` when `IsRootScrollViewer` is true:
- `OnGotFocus`: Return early (no focus visual/state changes)
- `OnKeyDown`: Skip keyboard scroll processing when SIP is NOT showing
- `OnPointerPressed/Released/Moved`: Skip pointer processing when SIP is NOT showing
- `OnPointerWheelChanged`: Only process when SIP is showing

These suppressions match WinUI's `ScrollViewer_Partial.cpp` where `IsRootScrollViewer()` gates behavior.

**TODO comments to add in code:**
- In `ScrollViewer` behavior suppression: `// TODO: WinUI also suppresses ShowIndicators() and skips dynamic scrollbar setting changes for RSV`
- In `RootScrollViewer.NotifyInputPaneStateChange()`: `// TODO: WinUI notifies ApplicationBarService.OnBoundsChanged and FlyoutBase.NotifyInputPaneStateChange for open flyouts`
- In `RootScrollViewer`: `// TODO: WinUI has ApplyInputPaneTransition() for smooth SIP show/hide animations`

#### 2.2 Create RootScrollViewer in XamlIslandRoot

**File**: `src/Uno.UI/UI/Xaml/Internal/Islands/XamlIslandRoot.cs`

In the `Content` setter (line 27-36), create the RSV and pass it to `SetPublicRootVisual`:

```csharp
public UIElement? Content
{
    get => _contentManager.Content;
    set
    {
        _contentManager.Content = value;
        var rootScrollViewer = EnsureRootScrollViewer();
        rootScrollViewer.Content = value;
        SetPublicRootVisual(value, rootScrollViewer, null);
    }
}

private RootScrollViewer? _rootScrollViewer;

private RootScrollViewer EnsureRootScrollViewer()
{
    _rootScrollViewer ??= new RootScrollViewer();
    return _rootScrollViewer;
}
```

Note: `ContentManager.cs` does NOT need changes for the Skia path since `isCoreWindowContent=false` means the `if (_isCoreWindowContent)` block (with the `// TODO: Add RootScrollViewer everywhere` comment) is never executed on Skia.

#### 2.3 Enable RootScrollViewer in visual tree

**File**: `src/Uno.UI/UI/Xaml/Internal/VisualTree.cs`

Change `RootScrollViewer` property type from `ScrollViewer?` to `RootScrollViewer?` (line 120). In `SetPublicRootVisual()` (around lines 225-241), uncomment the RSV tree insertion. The RSV wraps PublicRootVisual as its Content, and is added to RootElement via `AddRoot()`:

```csharp
if (PublicRootVisual != null)
{
    if (RootScrollViewer != null)
    {
        // RSV already has PublicRootVisual as Content (set by ContentManager)
        AddRoot(RootScrollViewer);
    }
    else
    {
        AddRoot(PublicRootVisual);
    }
}
```

**Important**: PopupRoot, FullWindowMediaRoot, FocusVisualRoot are added as **siblings** of the RSV (lines 256-264), not children. They are unaffected by the RSV. This is confirmed by reading `RootVisual.cs` - it's a Panel that arranges all children at full window size. For XamlIslandRoot, the RootElement is the island Panel itself, and the same sibling relationship applies.

#### 2.4 Handle content changes

When `Window.Content` changes, the `XamlIslandRoot.Content` setter is called again. `EnsureRootScrollViewer()` reuses the existing RSV instance and updates its `Content` to the new value.

`VisualTree.SetPublicRootVisual()` has an early return at line 182 when `publicRootVisual == PublicRootVisual`. Since the `publicRootVisual` parameter is the actual user content (not the RSV), content changes will pass through. The RSV itself is passed as `rootScrollViewer` parameter - if it's the same RSV instance, `VisualTree` should handle re-parenting the new content correctly via `ResetRoots()` (line 191) which removes old children before re-adding.

#### 2.5 RSV sizing

On Skia, the RSV is a child of `XamlIslandRoot` (Panel), which is the `RootElement` of the island's VisualTree. `XamlIslandRoot` arranges children to fill available space. The RSV will naturally stretch to fill the window via default alignment. No explicit Width/Height setting needed initially. When the keyboard appears (Phase 3), we set an explicit `Height` to shrink the visible area (matching WinUI's approach).

---

### Phase 3: Make InputPane Per-Window (uno-private#1154)

**Goal**: Change `InputPane` from a process-wide singleton to per-XamlRoot instances, and remove all `Window.InitialWindow` usage from InputPane code. This enables multi-window support.

**Backward compatibility**: `GetForCurrentView()` must continue to work for existing callers. It should return the InputPane for the "current" context (e.g., the focused element's XamlRoot, or the main window as fallback).

#### 3.1 Change InputPane storage from singleton to per-XamlRoot dictionary

**File**: `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.cs`

Replace the static singleton with a dictionary keyed by XamlRoot:

```csharp
// Replace: private static InputPane _instance = new();
// With:
private static readonly Dictionary<XamlRoot, InputPane> _instances = new();

// GetForCurrentView() remains backward-compatible: returns the initial window's InputPane
public static InputPane GetForCurrentView()
{
    var xamlRoot = Window.InitialWindow?.Content?.XamlRoot;
    if (xamlRoot != null)
    {
        return GetForXamlRoot(xamlRoot);
    }
    // Fallback for early calls before window is set up
    return _fallbackInstance ??= new InputPane();
}

// New internal API for multi-window support:
internal static InputPane GetForXamlRoot(XamlRoot xamlRoot)
{
    if (!_instances.TryGetValue(xamlRoot, out var inputPane))
    {
        inputPane = new InputPane();
        inputPane._xamlRoot = xamlRoot;
        _instances[xamlRoot] = inputPane;
    }
    return inputPane;
}

private XamlRoot? _xamlRoot; // The associated XamlRoot (null for fallback instance)
```

**Backward compatibility**: `GetForCurrentView()` continues to return the initial window's InputPane, exactly as before. Existing app code calling `GetForCurrentView()` sees no change. Only internal framework code uses `GetForXamlRoot()` for multi-window correctness.

#### 3.2 Update platform hosts to use per-XamlRoot InputPane

**File**: `src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs`

In `OnKeyboardChanged()` (line 239-243), instead of `InputPane.GetForCurrentView()`, get the InputPane for the window's XamlRoot:
```csharp
// Replace: _inputPane.OccludedRect = ...
// With: get InputPane from the window's XamlRoot
var xamlRoot = NativeWindowWrapper.Instance.Window?.Content?.XamlRoot;
if (xamlRoot != null)
{
    InputPane.GetForXamlRoot(xamlRoot).OccludedRect = ViewHelper.PhysicalToLogicalPixels(keyboard);
}
```

**File**: `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/AppleUIKitWindowWrapper.cs`

In `OnKeyboardWillShow()`/`OnKeyboardWillHide()`, use the window's XamlRoot to get the correct InputPane instance.

**File**: `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/Xaml/Window/WebAssemblyWindowWrapper.cs` (from Phase 1)

In the new `OnInputPaneChanged()` callback, use the window's XamlRoot.

#### 3.3 Update all internal callers of GetForCurrentView()

Key internal callers that should use per-XamlRoot access:

| File | Current Usage | Change |
|------|--------------|--------|
| `ContentDialog.Visuals.cs:294` | `InputPane.GetForCurrentView().OccludedRect` | Use `InputPane.GetForXamlRoot(XamlRoot)` |
| `ContentDialog.cs:84` | `InputPane.GetForCurrentView()` | Use `InputPane.GetForXamlRoot(XamlRoot)` |
| `AutoSuggestBox.cs:229` | `InputPane.GetForCurrentView().OccludedRect` | Use `InputPane.GetForXamlRoot(XamlRoot)` |
| `FocusManager.Android.cs:31` | `InputPane.GetForCurrentView().Visible` | Use `InputPane.GetForXamlRoot(xamlRoot)` |

**External/public callers** (app code, samples, tests) continue using `GetForCurrentView()` which falls back to the main instance. No breaking change.

#### 3.4 Remove Window.InitialWindow usage from InputPane.skia.cs

**File**: `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs`

The current code (line 40-46) does:
```csharp
var initialWindow = Window.InitialWindow;
var xamlRoot = initialWindow.Content?.XamlRoot;
```

Replace with: derive XamlRoot from the focused element itself or from the InputPane's associated XamlRoot (since InputPane is now per-XamlRoot, it knows its own XamlRoot).

Add a `XamlRoot? _xamlRoot` field to InputPane that's set when the instance is created via `GetForXamlRoot()`. Then in `EnsureFocusedElementInViewPartial()`:
```csharp
var xamlRoot = _xamlRoot; // Use the associated XamlRoot, not Window.InitialWindow
```

---

### Phase 4: Connect InputPane to RootScrollViewer (ties Phase 2 + 3 together)

**Goal**: When the keyboard shows/hides, shrink/restore the RSV and ensure focused elements are scrolled into view.

#### 4.1 Refactor InputPane.skia.cs

**File**: `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs`

**Multi-window design**: After Phase 3, each InputPane has a `_xamlRoot` field set at creation. Use `_xamlRoot` to access the VisualTree and RootScrollViewer. No `Window.InitialWindow` usage.

Replace the current `EnsureFocusedElementInViewPartial()` with a two-tier approach:

```
EnsureFocusedElementInViewPartial():
  1. Get XamlRoot from this._xamlRoot (set in Phase 3)
  2. Get focused element via FocusManager.GetFocusedElement(_xamlRoot)
  3. If keyboard SHOWING:
     a. Get RootScrollViewer from _xamlRoot.VisualTree.RootScrollViewer (typed as RootScrollViewer)
     b. If RSV exists:
        - Set RSV.Height = occludedRect.Y (shrink to visible area above keyboard)
        - Call RSV.NotifyInputPaneStateChange(true, occludedRect) -> enables scrolling, saves offsets
     c. Dispatch StartBringIntoView() on focused element (after layout pass)
  4. If keyboard HIDING (OccludedRect is empty):
     a. Restore RSV.Height = double.NaN (stretch to fill)
     b. Call RSV.NotifyInputPaneStateChange(false, Rect.Empty) -> disables scrolling, restores offsets
```

**No Pad mechanism on Skia** - following WinUI's approach:
- The RSV height shrink naturally constrains all descendant ScrollViewers' viewports
- Inner ScrollViewers are sized by their parent (RSV content area), so they naturally see a smaller viewport
- `BringIntoView` bubbles through the ScrollViewer chain, each handling its portion of the scroll
- The old `Pad()` code in `ScrollContentPresenter.OccludedPadding.cs` is kept for native Android (`InputPane.Android.cs`) but removed from the Skia path
- Remove `_padScrollContentPresenter` field and `Pad()` calls from `InputPane.skia.cs`

Key implementation details:
- `RootScrollViewer.NotifyInputPaneStateChange()` (from Phase 2.1) encapsulates enable/disable scrolling and offset save/restore
- RSV Height management (shrink/restore) is done by InputPane since it knows the occluded rect dimensions
- Use `_xamlRoot` (set in Phase 3) to access `VisualTree.RootScrollViewer` - no `Window.InitialWindow`
- `BringIntoView` bubbles up through inner SCPs and the RSV's SCP
- RSV Height shrink uses explicit `Height` property (matching WinUI's `SetValueByKnownIndex(FrameworkElement_Height)`)

**TODO comments to add in code:**
- In `EnsureFocusedElementInViewPartial()`: `// TODO: WinUI's CBringIntoViewHandler adjusts for caret position (75% CaretAlignmentThreshold), adds 20px padding (ExtraPixelsForBringIntoView), and accounts for AppBar height. See BringIntoViewHandler.cpp.`
- In `EnsureFocusedElementInViewPartial()`: `// TODO: WinUI calls BringIntoView with forceIntoView=true which bypasses BringIntoViewOnFocusChange checks. Verify our StartBringIntoView() path handles this.`

#### 4.2 Re-BringIntoView on focus change while keyboard showing

**File**: `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs`

When the keyboard is visible and focus moves to a different text control, re-run BringIntoView:

- In `EnsureFocusedElementInViewPartial()`, when `Visible == true`, subscribe to a focus change notification
- Use `FocusManager.GotFocus` event on the XamlRoot (or the existing `InputManager.NotifyFocusChanged()` path which already calls `StartBringIntoView()` - verify this is sufficient)

**Existing path to verify**: `InputManager.NotifyFocusChanged()` (line 76-86) already calls `StartBringIntoView()` when `bringIntoView == true`. This is called from `FocusManager.UpdateFocus()` when the new focus state is Keyboard. This may be sufficient for the focus-change case, since:
- The focused element calls `StartBringIntoView()`
- The RSV is now in the ancestor chain with reduced height and enabled scrolling
- The `BringIntoViewRequested` event will bubble up and the RSV's ScrollContentPresenter will handle it

**However**: We also need to re-run `Pad()` on the SCP for the new focused element's position. Add a handler:

```csharp
// In EnsureFocusedElementInViewPartial, when keyboard is showing:
_focusChangedToken = FocusManager.GotFocus += (s, e) =>
{
    if (Visible && e.NewFocusedElement is UIElement newFocused)
    {
        // RSV height is already shrunk - just BringIntoView for the new element
        _ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal,
            () => newFocused.StartBringIntoView());
    }
};

// When keyboard hides, unsubscribe
// TODO: WinUI uses InputPaneProcessor::NotifyFocusChanged() which checks
// SIP showing + text-editable focused = forces bringIntoView=true.
// Our GotFocus approach is simpler but may behave differently for
// non-keyboard focus changes. See InputPaneProcessor.cpp lines 222-234.
```

---

### Phase 5: Platform-Specific Verification

#### 5.1 Android Skia - Minor changes (Phase 3.2)

**File**: `src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs`

- `WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden` (line 32) - window does NOT resize. Confirmed.
- `OnKeyboardChanged()` (line 239-243) updated in Phase 3.2 to use `GetForXamlRoot()`. This triggers `EnsureFocusedElementInViewPartial()`.
- With the new Phase 4 logic, the RSV will shrink and enable scrolling automatically.
- **Verify**: The `RaiseNativeSizeChanged()` call (line 241) should not conflict with the RSV height change.

#### 5.2 iOS Skia - Minor changes (Phase 3.2)

**File**: `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/AppleUIKitWindowWrapper.cs`

- `OnKeyboardWillShow()` (line 195-217) updated in Phase 3.2 to use `GetForXamlRoot()`.
- Window does NOT resize (keyboard is overlay).
- Same Phase 4 logic applies automatically.
- **Verify**: The occluded rect coordinates from UIKeyboard are in window-relative coordinates matching what the RSV expects.

#### 5.3 ContentDialog - No code changes expected

**File**: `src/Uno.UI/UI/Xaml/Controls/ContentDialog/ContentDialog.Visuals.cs`

- `AdjustVisualStateForInputPane()` (line 285) reads `InputPane.OccludedRect` directly.
- This works independently of the RSV mechanism.
- With WASM now setting `OccludedRect`, ContentDialog adjustment will start working on WASM too.

---

## File Change Summary

| File | Change Type | Phase |
|------|-------------|-------|
| `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/WebAssemblyWindowWrapper.ts` | Modify - add visualViewport handling | 1.1 |
| `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/Xaml/Window/WebAssemblyWindowWrapper.cs` | Modify - add OnInputPaneChanged callback | 1.2 |
| `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/Xaml/Window/WebAssemblyWindowWrapper.Interop.cs` | Modify - add interop declaration if needed | 1.2 |
| `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/UI/ViewManagement/InputPaneExtension.cs` | **New** - WASM IInputPaneExtension | 1.3 |
| `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Hosting/WebAssemblyBrowserHost.cs` | Modify - register extension | 1.3 |
| `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/RootScrollViewer.cs` | **New** - RootScrollViewer class (derives from ScrollViewer) | 2.1 |
| `src/Uno.UI/UI/Xaml/Controls/ScrollViewer/ScrollViewer.cs` | Modify - add `IsRootScrollViewer` virtual + behavior suppression | 2.1 |
| `src/Uno.UI/UI/Xaml/Internal/Islands/XamlIslandRoot.cs` | Modify - create RSV and pass to VisualTree | 2.2 |
| `src/Uno.UI/UI/Xaml/Internal/VisualTree.cs` | Modify - change RSV type, uncomment RSV insertion | 2.3 |
| `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.cs` | Modify - per-XamlRoot instances, add GetForXamlRoot() | 3.1 |
| `src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs` | Modify - use GetForXamlRoot() | 3.2 |
| `src/Uno.UI.Runtime.Skia.AppleUIKit/UI/Xaml/Window/AppleUIKitWindowWrapper.cs` | Modify - use GetForXamlRoot() | 3.2 |
| `src/Uno.UI/UI/Xaml/Controls/ContentDialog/ContentDialog.Visuals.cs` | Modify - use GetForXamlRoot() | 3.3 |
| `src/Uno.UI/UI/Xaml/Controls/ContentDialog/ContentDialog.cs` | Modify - use GetForXamlRoot() | 3.3 |
| `src/Uno.UI/UI/Xaml/Controls/AutoSuggestBox/AutoSuggestBox.cs` | Modify - use GetForXamlRoot() | 3.3 |
| `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs` | Modify - use _xamlRoot, RSV shrink/restore, focus change | 3.4, 4.1, 4.2 |

---

### Phase 6: Manual Test Pages in SamplesApp

**Goal**: Create a comprehensive manual test page in the SamplesApp with all soft input keyboard scenarios. Each test case opens as a full-page repro with a back button to return.

#### 6.1 Create main test hub page

**File (new)**: `src/SamplesApp/UITests.Shared/Windows_UI_ViewManagement/SoftInputTests.xaml`
**File (new)**: `src/SamplesApp/UITests.Shared/Windows_UI_ViewManagement/SoftInputTests.xaml.cs`

A page with buttons to navigate to each individual test scenario. Shows `InputPane.OccludedRect` status at the top.

```
[Sample("Windows.UI.ViewManagement", Name = "Soft Input Keyboard Tests")]
```

Buttons:
1. "TextBox - No ScrollViewer" -> SoftInputTest_NoScrollViewer
2. "TextBox - Inside ScrollViewer" -> SoftInputTest_InScrollViewer
3. "TextBox - In Flyout" -> SoftInputTest_InFlyout
4. "TextBox - In ContentDialog" -> SoftInputTest_InContentDialog
5. "Multiple TextBoxes - Focus Change" -> SoftInputTest_FocusChange
6. "PasswordBox - Bottom of Page" -> SoftInputTest_PasswordBox

Each button uses `Frame.Navigate()` to open the test page.

#### 6.2 Individual test page scenarios

Each page has: a Back button at the top, descriptive header, InputPane status display, and the test content.

**SoftInputTest_NoScrollViewer.xaml**: 
- No ScrollViewer in the page
- Large spacer (fills most of page height)
- TextBox at the bottom
- Expected: RootScrollViewer scrolls page up to show TextBox above keyboard

**SoftInputTest_InScrollViewer.xaml**:
- ScrollViewer wrapping content
- Tall content (e.g., 2000px Rectangle)
- TextBox at the bottom of the tall content
- Expected: Inner ScrollViewer scrolls via Pad+BringIntoView

**SoftInputTest_InFlyout.xaml**:
- Button that opens a Flyout containing a TextBox
- Expected: Flyout stays open when keyboard appears (no dismiss/reposition)

**SoftInputTest_InContentDialog.xaml**:
- Button that opens a ContentDialog with TextBox
- Expected: Dialog adjusts position above keyboard

**SoftInputTest_FocusChange.xaml**:
- No ScrollViewer
- TextBox at top and TextBox at bottom (with spacer between)
- Expected: Tap bottom TextBox (keyboard shows, page scrolls down), then tap top TextBox (page scrolls back up)

**SoftInputTest_PasswordBox.xaml**:
- Same as NoScrollViewer but with PasswordBox
- Expected: Same RootScrollViewer behavior

#### 6.3 Register all pages in UITests.Shared.projitems

**File**: `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`

Add all new XAML and code-behind files:
```xml
<Page Include="$(MSBuildThisFileDirectory)Windows_UI_ViewManagement\SoftInputTests.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
<Compile Include="$(MSBuildThisFileDirectory)Windows_UI_ViewManagement\SoftInputTests.xaml.cs">
  <DependentUpon>SoftInputTests.xaml</DependentUpon>
</Compile>
<!-- Repeat for each sub-page -->
```

---

## Verification Plan

### Unit Tests
- Build: `dotnet build src/Uno.UI-Skia-only.slnf --no-restore`
- Unit tests: `dotnet test src/Uno.UI/Uno.UI.Tests.csproj --no-build`

### Runtime Tests (use `/runtime-tests` skill)
- Test BringIntoView with RootScrollViewer present
- Test InputPane.OccludedRect propagation
- Test ScrollContentPresenter.Pad still works with RSV

### Manual Testing Matrix

| Scenario | WASM Skia | Android Skia | iOS Skia |
|----------|-----------|--------------|----------|
| TextBox at bottom of page (no ScrollViewer) | RSV scrolls into view | RSV scrolls into view | RSV scrolls into view |
| TextBox inside ScrollViewer | Pad + BringIntoView | Pad + BringIntoView | Pad + BringIntoView |
| TextBox in Flyout | Flyout stays, keyboard overlays | Flyout stays | Flyout stays |
| TextBox in ContentDialog | Dialog adjusts up | Dialog adjusts up | Dialog adjusts up |
| Focus change between TextBoxes (keyboard open) | Re-scrolls to new element | Re-scrolls | Re-scrolls |
| Real window resize (orientation/browser resize) | Layout updates normally | Layout updates normally | N/A |
| Keyboard dismiss | RSV restores, content returns | RSV restores | RSV restores |
| Page with no text controls | No RSV scrolling (disabled) | No RSV scrolling | No RSV scrolling |

### WASM-Specific Tests
- Verify `visualViewport` API detection and fallback
- Test on iOS Safari (mobile), Chrome Android, Firefox Android
- Verify `InputPane.OccludedRect` is set correctly
- Verify `window.resize` during keyboard does NOT change reported window size
- Verify flyouts with TextBox remain open when keyboard shows

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| RSV breaks existing layout | MEDIUM | RSV has scrolling disabled by default, is transparent to layout. PopupRoot/FullWindowMediaRoot are siblings, not children. |
| visualViewport API inconsistencies | LOW | Well-standardized API (Chrome 61+, Safari 13+). Add fallback for older browsers. |
| RSV affects popup coordinate space | LOW | Popups are in PopupRoot (sibling of RSV), not children. Confirmed by reading VisualTree.AddRoot. |
| ContentDialog double-adjustment | LOW | ContentDialog uses OccludedRect directly, independent of RSV. |
| Performance impact | LOW | RSV with scrolling disabled is a pass-through container. |
| Regression on Android/iOS Skia | MEDIUM | Existing Pad mechanism preserved. RSV adds a fallback layer. Runtime test on all platforms. |

---

## Design Decisions (Resolved)

1. **RSV scope**: Skia-only (`#if __SKIA__`). Native platforms handle this natively.
2. **WASM keyboard detection**: Dual-signal approach - no pixel threshold. Detect keyboard when (a) our invisible text input is focused AND (b) `visualViewport.height < window.innerHeight`. Occluded rect height = difference.
3. **RSV Height management**: Use explicit `Height` property (matching WinUI's `SetValueByKnownIndex(FrameworkElement_Height)`).
4. **Multi-window**: Build with multi-window in mind from the start. Derive XamlRoot/VisualTree from the focused element (`focusedElement.XamlRoot.VisualTree`), never from `Window.InitialWindow`. RSV is already per-VisualTree (per-ContentRoot), so it naturally supports multi-window.
