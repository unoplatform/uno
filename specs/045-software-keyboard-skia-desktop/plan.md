# Software keyboard on TextBox focus (Skia desktop) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Raise the OS software/touch keyboard when a `TextBox`/`PasswordBox` is focused via touch or pen on Skia **Win32**, and hide it on blur — making the public `InputPane` API functional on desktop.

**Architecture:** Wire the existing `InputPane.TryShow()/TryHide()` into the shared Skia `TextBox` focus path (touch/pen-gated via `InputManager.LastInputDeviceType`), and implement the long-missing `IInputPaneExtension` on Win32 using the WinRT `IInputPaneInterop`→`IInputPane2` interop (the WPF/Firefox approach) for show/hide plus the plain-COM `IFrameworkInputPane` for occlusion. X11/macOS/FrameBuffer get honest no-ops. The singleton `InputPane` dynamically targets the focused control's window.

**Tech Stack:** C# / Uno.UI (Skia), CsWin32 (`Microsoft.Windows.CsWin32`) for P/Invoke + COM, WinRT COM interop (`[ComImport]`), MSTest runtime tests (`Uno.UI.RuntimeTests`).

## Global Constraints

- **Skia-first / native untouched.** All changes target Skia (`__SKIA__`) and the Skia runtime projects. Do **not** modify native Android/iOS/WASM UI behavior. (AGENTS.md → Development scope)
- **New `.cs` files must be UTF-8 **with BOM** and CRLF line endings**, else `dotnet format`/IDE0055 fails on CI. After creating any new `.cs` file, normalize encoding (e.g. `Set-Content -Encoding utf8BOM` or equivalent) and run `dotnet format` before staging.
- **Code style:** tabs, Allman braces always, expression-bodied one-liners, `#nullable enable` per new file, file-scoped namespaces. Uno-native files (these) carry **no** MS/MUX header.
- **No `event Action`** — N/A here (no new events), but keep in mind.
- **Windows API floor:** `OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393)` (Windows 10 1607) guards all InputPane interop.
- **Never auto-commit.** Stage logical chunks with `git add` only; do not `git commit` (user rule overrides the plan's commit steps — treat "Commit" steps as "stage + report"). Run `dotnet format` before staging.
- **Validation honesty:** label evidence **Code review** / **Compile** / **Runtime**. The Win32 COM tasks cannot be runtime-tested headlessly — they are compile-validated + manually validated on a Windows touch device. Say so.

---

## File Structure

**Uno.UI (shared Skia):**
- `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs` — *modify*: internal `TargetXamlRoot`, test seam, pan-into-view uses the target/focused `XamlRoot`.
- `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs` — *create*: the touch/pen-gated show + deferred-hide helpers.
- `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs` — *modify*: call the helpers from `OnFocusStateChangedPartial`.

**Win32 runtime:**
- `src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs` — *create*: `IInputPaneExtension` + COM interop + `IFrameworkInputPaneHandler`.
- `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs` — *modify*: register the extension.
- `src/Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt` — *modify*: add WinRT-activation + `IFrameworkInputPane*` symbols.

**Other desktop runtimes (no-op):**
- `src/Uno.UI.Runtime.Skia.X11/UI/ViewManagement/X11InputPaneExtension.cs` + `Hosting/X11ApplicationHost.cs`
- `src/Uno.UI.Runtime.Skia.MacOS/UI/ViewManagement/MacOSInputPaneExtension.cs` + `Hosting/MacSkiaHost.cs`
- `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/UI/ViewManagement/FrameBufferInputPaneExtension.cs` + `Hosting/FramebufferHost.cs`

**Tests & sample:**
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_TextBox.SoftwareKeyboard.skia.cs` — *create*: trigger tests.
- `src/SamplesApp/SamplesApp.Samples/Samples/.../SoftwareKeyboardSample.{xaml,xaml.cs}` — *create*.

---

## Task 1: InputPane test seam + window target + pan-into-view fix (Uno.UI Skia)

**Files:**
- Modify: `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs`
- Test: `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_TextBox.SoftwareKeyboard.skia.cs` (create)

**Interfaces:**
- Produces:
  - `internal Microsoft.UI.Xaml.XamlRoot? InputPane.TargetXamlRoot { get; set; }`
  - `internal static IDisposable InputPane.SetExtensionForTesting(IInputPaneExtension extension)`
  - Existing `IInputPaneExtension { bool TryShow(); bool TryHide(); }` (unchanged).
- Consumes (from Uno.UI internals): `XamlRoot`, `FocusManager.GetFocusedElement(XamlRoot)`, `Window.InitialWindow`.

- [ ] **Step 1: Write the failing test** — `Given_TextBox.SoftwareKeyboard.skia.cs`

```csharp
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core;   // VisualTree
using Uno.UI.Xaml.Input;  // InputDeviceType
using MUXC = Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_TextBox_SoftwareKeyboard
{
	// Models the platform contract: a show populates OccludedRect (=> Visible),
	// a hide clears it — mirroring the real Win32 occlusion forwarding.
	private sealed class FakeInputPaneExtension : IInputPaneExtension
	{
		public int ShowCount { get; private set; }
		public int HideCount { get; private set; }

		public bool TryShow()
		{
			ShowCount++;
			InputPane.GetForCurrentView().OccludedRect = new Rect(0, 0, 100, 200);
			return true;
		}

		public bool TryHide()
		{
			HideCount++;
			InputPane.GetForCurrentView().OccludedRect = default;
			return true;
		}
	}

	[TestMethod]
	public async Task When_InputPane_Routes_To_Injected_Extension()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var inputPane = InputPane.GetForCurrentView();
		Assert.IsTrue(inputPane.TryShow());
		Assert.AreEqual(1, fake.ShowCount);
		Assert.IsTrue(inputPane.Visible);     // OccludedRect set by the fake
		Assert.IsTrue(inputPane.TryHide());
		Assert.AreEqual(1, fake.HideCount);
		Assert.IsFalse(inputPane.Visible);
	}
}
```

- [ ] **Step 2: Run it; verify it fails** (no `SetExtensionForTesting`)

Use the `/runtime-tests` skill (Skia Desktop), filter `Given_TextBox_SoftwareKeyboard.When_InputPane_Routes_To_Injected_Extension`.
Expected: **compile error** `'InputPane' does not contain a definition for 'SetExtensionForTesting'`.

- [ ] **Step 3: Add the seam + target + pan fix** — `InputPane.skia.cs`

Add fields/properties to the `partial class InputPane`:

```csharp
internal Microsoft.UI.Xaml.XamlRoot? TargetXamlRoot { get; set; }

// Test seam: when set, platform show/hide route here instead of the registered extension.
internal static IInputPaneExtension? ExtensionForTesting { get; private set; }

internal static IDisposable SetExtensionForTesting(IInputPaneExtension extension)
{
	var previous = ExtensionForTesting;
	ExtensionForTesting = extension;
	GetForCurrentView().OccludedRect = default;
	return Uno.Disposables.Disposable.Create(() =>
	{
		ExtensionForTesting = previous;
		GetForCurrentView().OccludedRect = default;
	});
}
```

Route the platform calls through the seam:

```csharp
private bool TryShowPlatform() => (ExtensionForTesting ?? _inputPaneExtension?.Value)?.TryShow() ?? false;

private bool TryHidePlatform() => (ExtensionForTesting ?? _inputPaneExtension?.Value)?.TryHide() ?? false;
```

Fix `EnsureFocusedElementInViewPartial` to use the target/focused window instead of the hardcoded `Window.InitialWindow`:

```csharp
partial void EnsureFocusedElementInViewPartial()
{
	_padScrollContentPresenter?.Dispose(); // Restore padding

	// Use the window the pane currently targets (set by the focusing TextBox),
	// falling back to the initial window for direct API callers. This makes
	// pan-into-view correct in multi-window apps.
	var xamlRoot = TargetXamlRoot ?? Window.InitialWindow?.Content?.XamlRoot;

	if (xamlRoot is not null && Visible && FocusManager.GetFocusedElement(xamlRoot) is UIElement focusedElement)
	{
		// ... existing body unchanged (ScrollContentPresenter lookup + Pad + StartBringIntoView) ...
	}
}
```

(Keep the existing body; only the `xamlRoot` source changes. Remove the now-unused `initialWindow` local.)

- [ ] **Step 4: Run the test; verify it passes**

`/runtime-tests` Skia Desktop, same filter. Expected: **PASS**.

- [ ] **Step 5: Stage**

```bash
dotnet format
git add src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs \
        src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_TextBox.SoftwareKeyboard.skia.cs
# (do not commit — user stages only)
```

---

## Task 2: Touch/pen-gated show on focus (trigger — show path)

**Files:**
- Create: `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs`
- Modify: `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs:427` (focus-gained branch)
- Test: `Given_TextBox.SoftwareKeyboard.skia.cs`

**Interfaces:**
- Consumes: `InputPane.GetForCurrentView()`, `InputPane.TargetXamlRoot`, `VisualTree.GetContentRootForElement`, `InputManager.LastInputDeviceType`, `InputDeviceType`.
- Produces: `private void TextBox.ShowSoftwareKeyboardForTouchFocus(FocusState focusState)`; `private static int TextBox._softwareKeyboardShowGeneration`.

- [ ] **Step 1: Write the failing tests** — append to `Given_TextBox.SoftwareKeyboard.skia.cs`

```csharp
	[TestMethod]
	public async Task When_TouchFocus_Then_Keyboard_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_MouseFocus_Then_Keyboard_Not_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Mouse;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_KeyboardFocus_Then_Keyboard_Not_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Keyboard); // not a pointer-driven focus
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, fake.ShowCount);
	}
```

> Note: if `Focus(FocusState.Pointer)` resets `LastInputDeviceType`, set it again immediately before `Focus` returns is not possible — instead drive focus by injecting a real touch tap (see `Given_TextBox.skia.cs` touch helpers / `InputInjectorHelper`) and assert the same. Keep whichever proves deterministic; the assertions are unchanged.

- [ ] **Step 2: Run; verify failure**

`/runtime-tests` Skia Desktop, filter `Given_TextBox_SoftwareKeyboard.When_TouchFocus_Then_Keyboard_Shown`.
Expected: FAIL — `ShowCount` is 0 (no trigger yet).

- [ ] **Step 3: Create the helper file** — `TextBox.SoftwareKeyboard.skia.cs`

```csharp
#nullable enable
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Core;   // VisualTree
using Uno.UI.Xaml.Input;  // InputDeviceType

namespace Microsoft.UI.Xaml.Controls;

public partial class TextBox
{
	// One physical OS keyboard exists; this generation is shared across all TextBoxes
	// so a focus hand-off (TextBox -> TextBox) cancels the previous box's pending hide.
	private static int _softwareKeyboardShowGeneration;

	private void ShowSoftwareKeyboardForTouchFocus(FocusState focusState)
	{
		// Only pointer-driven focus, and only when the pointer was touch or pen
		// (mouse / keyboard / programmatic focus must not raise the keyboard).
		if (focusState != FocusState.Pointer)
		{
			return;
		}

		var lastInputDeviceType = VisualTree.GetContentRootForElement(this)?.InputManager.LastInputDeviceType;
		if (lastInputDeviceType is not (InputDeviceType.Touch or InputDeviceType.Pen))
		{
			return;
		}

		_softwareKeyboardShowGeneration++;

		var inputPane = InputPane.GetForCurrentView();
		inputPane.TargetXamlRoot = XamlRoot;
		inputPane.TryShow();
	}
}
```

- [ ] **Step 4: Call it from the focus-gained branch** — `TextBox.skia.cs`

In `OnFocusStateChangedPartial`, inside `if (focusState != FocusState.Unfocused)` (after `StartImeSession();`, ~line 431):

```csharp
				StartImeSession();
				ShowSoftwareKeyboardForTouchFocus(focusState);
```

- [ ] **Step 5: Run; verify all three pass**

`/runtime-tests` Skia Desktop, filter `Given_TextBox_SoftwareKeyboard`.
Expected: `When_TouchFocus…` PASS, `When_MouseFocus…` PASS, `When_KeyboardFocus…` PASS.

- [ ] **Step 6: Stage**

```bash
dotnet format
git add src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs \
        src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs \
        src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_TextBox.SoftwareKeyboard.skia.cs
```

---

## Task 3: Deferred hide on blur + anti-flicker + PasswordBox (trigger — hide path)

**Files:**
- Modify: `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs`
- Modify: `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs:452` (focus-lost branch)
- Test: `Given_TextBox.SoftwareKeyboard.skia.cs`

**Interfaces:**
- Produces: `private void TextBox.RequestHideSoftwareKeyboard()`.

- [ ] **Step 1: Write the failing tests**

```csharp
	[TestMethod]
	public async Task When_Blur_To_NonText_Then_Keyboard_Hidden()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		var button = new Button() { Content = "x" };
		var panel = new StackPanel();
		panel.Children.Add(textBox);
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(1, fake.ShowCount);

		button.Focus(FocusState.Pointer); // focus leaves the text box to a non-text control
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.HideCount);
	}

	[TestMethod]
	public async Task When_Focus_Moves_Between_TextBoxes_Then_No_Flicker()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var tb1 = new TextBox();
		var tb2 = new TextBox();
		var panel = new StackPanel();
		panel.Children.Add(tb1);
		panel.Children.Add(tb2);
		await UITestHelper.Load(panel);

		var inputManager = VisualTree.GetContentRootForElement(tb1)!.InputManager;

		inputManager.LastInputDeviceType = InputDeviceType.Touch;
		tb1.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		inputManager.LastInputDeviceType = InputDeviceType.Touch;
		tb2.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		// tb2 took over the keyboard before tb1's deferred hide ran => no hide, no flicker.
		Assert.AreEqual(0, fake.HideCount);
		Assert.AreEqual(2, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_PasswordBox_TouchFocus_Then_Keyboard_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var passwordBox = new PasswordBox();
		await UITestHelper.Load(passwordBox);

		VisualTree.GetContentRootForElement(passwordBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		passwordBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.ShowCount);
	}
```

- [ ] **Step 2: Run; verify failure**

`/runtime-tests` Skia Desktop, filter `Given_TextBox_SoftwareKeyboard.When_Blur_To_NonText_Then_Keyboard_Hidden`.
Expected: FAIL — `HideCount` is 0 (no hide wired yet).

- [ ] **Step 3: Add the hide helper** — append to `TextBox.SoftwareKeyboard.skia.cs`

```csharp
	private void RequestHideSoftwareKeyboard()
	{
		var generation = _softwareKeyboardShowGeneration;
		var dispatcher = DispatcherQueue;

		if (dispatcher is null)
		{
			InputPane.GetForCurrentView().TryHide();
			return;
		}

		// Defer the hide one tick: if another editable control raises the keyboard
		// in the meantime it bumps the generation, and we leave it shown (no flicker).
		dispatcher.TryEnqueue(() =>
		{
			if (_softwareKeyboardShowGeneration == generation)
			{
				InputPane.GetForCurrentView().TryHide();
			}
		});
	}
```

- [ ] **Step 4: Call it from the focus-lost branch** — `TextBox.skia.cs`

In `OnFocusStateChangedPartial`, inside `if (focusState == FocusState.Unfocused && !_forceFocusedVisualState)` (after `EndImeSession();`, ~line 454):

```csharp
				EndImeSession();
				RequestHideSoftwareKeyboard();
```

- [ ] **Step 5: Run; verify all four pass**

`/runtime-tests` Skia Desktop, filter `Given_TextBox_SoftwareKeyboard`.
Expected: all PASS (5 from Tasks 1–2 plus these 4).

- [ ] **Step 6: Stage**

```bash
dotnet format
git add src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs \
        src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs \
        src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Controls/Given_TextBox.SoftwareKeyboard.skia.cs
```

---

## Task 4: CsWin32 symbols for InputPane interop (Win32.Support)

**Files:**
- Modify: `src/Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt`

**Interfaces:**
- Produces (generated `Windows.Win32.*`): `PInvoke.RoGetActivationFactory`, `PInvoke.WindowsCreateString`, `PInvoke.WindowsDeleteString`, `IFrameworkInputPane`, `IFrameworkInputPaneHandler`, `FrameworkInputPane` (CLSID), `PInvoke.ScreenToClient` (already present), `RECT` (present).

- [ ] **Step 1: Add symbols** — under the appropriate sections of `NativeMethods.txt`

Append (functions):
```
RoGetActivationFactory
WindowsCreateString
WindowsDeleteString
```
Append (COM interfaces / structs):
```
IFrameworkInputPane
IFrameworkInputPaneHandler
```
`ScreenToClient` and `RECT` already exist; add `ScreenToClient` if missing.

- [ ] **Step 2: Compile the Win32 runtime to generate bindings**

```
cd src && dotnet build Uno.UI.Runtime.Skia.Win32/Uno.UI.Runtime.Skia.Win32.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true
```
Expected: **builds**. If CsWin32 reports a name it can't find (e.g. `IFrameworkInputPane` not in metadata), remove it from the txt and declare that interface manually with `[ComImport]` in Task 6 instead. Record which symbols generated successfully.

- [ ] **Step 3: Stage**

```bash
git add src/Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt
```

---

## Task 5: Win32 `IInputPaneExtension` — show/hide via IInputPaneInterop/IInputPane2

**Files:**
- Create: `src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs`
- Modify: `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs:108` (after the `IImeTextBoxExtension` registration)

**Interfaces:**
- Consumes: `InputPane` (owner, with internal `TargetXamlRoot`/`OccludedRect`), `XamlRootMap.GetHostForRoot`, `Win32WindowWrapper.NativeWindow`, `Win32NativeWindow.Hwnd`, `FocusManager.GetFocusedElement`, `Windows.Win32.PInvoke`.
- Produces: `internal sealed class Win32InputPaneExtension : IInputPaneExtension` with ctor `(InputPane owner)` and a private `bool TryResolveHwnd(out HWND hwnd)`.

**Validation note:** COM activation + the OS keyboard cannot be exercised in headless CI. This task is **Compile-validated** and **manually validated** on a Windows touch device (Task 8 sample). State this in the report.

- [ ] **Step 1: Create the extension (show/hide only; occlusion added in Task 6)**

```csharp
#nullable enable
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32InputPaneExtension : IInputPaneExtension
{
	// WinRT InputPane interop interfaces (not in Win32 metadata) — declared here.
	// Shapes verbatim from the Microsoft WPF-Samples TouchKeyboard sample (InputPaneRcw.cs).
	[ComImport]
	[Guid("75CF2C57-9195-4931-8332-F0B409E916AF")]
	[InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
	private interface IInputPaneInterop
	{
		[return: MarshalAs(UnmanagedType.IInspectable)]
		object GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
	}

	[ComImport]
	[Guid("8A6B3F26-7090-4793-944C-C3F2CDE26276")]
	[InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
	private interface IInputPane2
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		bool TryShow();

		[return: MarshalAs(UnmanagedType.Bool)]
		bool TryHide();
	}

	private const string InputPaneRuntimeClass = "Windows.UI.ViewManagement.InputPane";

	private readonly InputPane _owner;
	private IInputPaneInterop? _interop;

	public Win32InputPaneExtension(InputPane owner) => _owner = owner;

	public bool TryShow()
	{
		if (!TryGetWinRtInputPane(out var inputPane))
		{
			return false;
		}

		return inputPane.TryShow();
	}

	public bool TryHide()
	{
		if (!TryGetWinRtInputPane(out var inputPane))
		{
			return false;
		}

		return inputPane.TryHide();
	}

	private bool TryGetWinRtInputPane(out IInputPane2 inputPane)
	{
		inputPane = null!;

		if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
		{
			return false;
		}

		if (!TryResolveHwnd(out var hwnd))
		{
			return false;
		}

		try
		{
			_interop ??= GetInterop();
			var iid = typeof(IInputPane2).GUID;
			inputPane = (IInputPane2)_interop.GetForWindow(hwnd.Value, ref iid);
			return inputPane is not null;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to acquire WinRT InputPane for the window.", e);
			}
			return false;
		}
	}

	private static IInputPaneInterop GetInterop()
	{
		// Acquire the InputPane activation factory as IInputPaneInterop via RoGetActivationFactory.
		PInvoke.WindowsCreateString(InputPaneRuntimeClass, (uint)InputPaneRuntimeClass.Length, out var hClassName).ThrowOnFailure();
		try
		{
			var iid = typeof(IInputPaneInterop).GUID;
			PInvoke.RoGetActivationFactory(hClassName, in iid, out var factory).ThrowOnFailure();
			return (IInputPaneInterop)factory;
		}
		finally
		{
			PInvoke.WindowsDeleteString(hClassName);
		}
	}

	private bool TryResolveHwnd(out HWND hwnd)
	{
		hwnd = HWND.Null;

		// Prefer the window the focusing TextBox targeted; fall back to the focused element's root.
		var xamlRoot = _owner.TargetXamlRoot;
		if (xamlRoot is null)
		{
			foreach (var pair in XamlRootMap.Enumerate())
			{
				if (FocusManager.GetFocusedElement(pair.Key) is not null)
				{
					xamlRoot = pair.Key;
					break;
				}
			}
		}

		if (xamlRoot is null || XamlRootMap.GetHostForRoot(xamlRoot) is not Win32WindowWrapper wrapper)
		{
			return false;
		}

		if (wrapper.NativeWindow is not Win32NativeWindow nativeWindow)
		{
			return false;
		}

		hwnd = (HWND)nativeWindow.Hwnd;
		return true;
	}
}
```

> CsWin32 signature check at compile: `RoGetActivationFactory(HSTRING, in Guid, out object)` and `WindowsCreateString(string, uint, out HSTRING)` — adjust the exact overload/marshaling to what CsWin32 generated in Task 4 (e.g. `in iid` vs `ref iid`, return `HRESULT` with `.ThrowOnFailure()`). Keep the logic identical.

- [ ] **Step 2: Register in `Win32Host`** — after line 108

```csharp
			ApiExtensibility.Register(typeof(IImeTextBoxExtension), _ => Win32ImeTextBoxExtension.Instance);
			ApiExtensibility.Register<InputPane>(typeof(IInputPaneExtension), inputPane => new Win32InputPaneExtension(inputPane));
```

Add `using Windows.UI.ViewManagement;` to `Win32Host.cs` if not present.

- [ ] **Step 3: Compile**

```
cd src && dotnet build Uno.UI.Runtime.Skia.Win32/Uno.UI.Runtime.Skia.Win32.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true
```
Expected: **builds**. Fix any CsWin32 signature mismatches per the note above.

- [ ] **Step 4: Stage**

```bash
dotnet format
git add src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs \
        src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs
```

---

## Task 6: Win32 occlusion forwarding via IFrameworkInputPane

**Files:**
- Modify: `src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs`

**Interfaces:**
- Consumes: `IFrameworkInputPane`, `IFrameworkInputPaneHandler`, `FrameworkInputPane` CLSID, `PInvoke.CoCreateInstance`, `PInvoke.ScreenToClient`, `RECT`, `XamlRoot.RasterizationScale`, `InputPane.OccludedRect`, `DispatcherQueue`.
- Produces: occlusion rect pushed to `_owner.OccludedRect` when the OS keyboard shows/hides.

**Validation note:** Compile + manual (same as Task 5).

- [ ] **Step 1: Implement the handler + advise**

Add to `Win32InputPaneExtension` (use the CsWin32-generated `IFrameworkInputPane`/`IFrameworkInputPaneHandler` if Task 4 generated them; otherwise declare them `[ComImport]` per the IDL: `IFrameworkInputPane` IID `5752238B-24F0-495A-82F1-2FD593056796`, `IFrameworkInputPaneHandler` IID `EB3D7A2C-B0FE-49d4-9D85-D04E69A2A89E`, CLSID `FrameworkInputPane` `D5120AA3-46BA-44C5-822D-CA8092C1FC72`):

```csharp
	private object? _frameworkInputPane; // IFrameworkInputPane
	private uint _adviseCookie;
	private HWND _advisedHwnd;

	private sealed class OcclusionHandler : IFrameworkInputPaneHandler
	{
		private readonly Win32InputPaneExtension _ext;
		public OcclusionHandler(Win32InputPaneExtension ext) => _ext = ext;

		public void Showing(in RECT prcInputPaneScreenLocation, BOOL fEnsureFocusedElementInView)
			=> _ext.OnOcclusionChanged(prcInputPaneScreenLocation);

		public void Hiding(BOOL fEnsureFocusedElementInView)
			=> _ext.OnOcclusionChanged(default);
	}

	private void EnsureAdvised(HWND hwnd)
	{
		if (_adviseCookie != 0 && _advisedHwnd == hwnd)
		{
			return;
		}

		Unadvise();

		var clsid = typeof(FrameworkInputPane).GUID; // CLSID from CsWin32, or the literal CLSID
		var iid = typeof(IFrameworkInputPane).GUID;
		PInvoke.CoCreateInstance(in clsid, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER, in iid, out var obj).ThrowOnFailure();
		_frameworkInputPane = obj;
		((IFrameworkInputPane)obj).AdviseWithHWND(hwnd, new OcclusionHandler(this), out _adviseCookie);
		_advisedHwnd = hwnd;
	}

	private void Unadvise()
	{
		if (_frameworkInputPane is IFrameworkInputPane fip && _adviseCookie != 0)
		{
			fip.Unadvise(_adviseCookie);
		}
		_adviseCookie = 0;
		_frameworkInputPane = null;
		_advisedHwnd = HWND.Null;
	}

	private void OnOcclusionChanged(RECT screenRect)
	{
		if (!TryResolveHwnd(out var hwnd))
		{
			return;
		}

		Rect occluded = default;
		if (screenRect.right > screenRect.left && screenRect.bottom > screenRect.top)
		{
			// Screen (virtual-desktop) pixels -> this window's client pixels -> client DIPs.
			var topLeft = new System.Drawing.Point(screenRect.left, screenRect.top);
			var bottomRight = new System.Drawing.Point(screenRect.right, screenRect.bottom);
			unsafe
			{
				var pt = new Windows.Win32.Foundation.POINT { x = topLeft.X, y = topLeft.Y };
				PInvoke.ScreenToClient(hwnd, ref pt);
				var pt2 = new Windows.Win32.Foundation.POINT { x = bottomRight.X, y = bottomRight.Y };
				PInvoke.ScreenToClient(hwnd, ref pt2);
				var scale = _owner.TargetXamlRoot?.RasterizationScale ?? 1.0;
				occluded = new Rect(pt.x / scale, pt.y / scale, (pt2.x - pt.x) / scale, (pt2.y - pt.y) / scale);
			}
		}

		// OccludedRect must be mutated on the UI thread (raises Showing/Hiding + pan).
		var dispatcher = _owner.TargetXamlRoot?.HostWindow?.DispatcherQueue;
		if (dispatcher is not null)
		{
			dispatcher.TryEnqueue(() => _owner.OccludedRect = occluded);
		}
		else
		{
			_owner.OccludedRect = occluded;
		}
	}
```

Add `EnsureAdvised(hwnd);` inside `TryGetWinRtInputPane` right after `TryResolveHwnd` succeeds, so monitoring starts when the keyboard is first requested for a window. (`using Windows.Win32.System.Com;`, `using Windows.Foundation;` for `Rect`.)

> `DispatcherQueue` accessor: use whatever the Win32 wrapper exposes (`Win32WindowWrapper` / `XamlRoot.HostWindow`). If `RasterizationScale`/dispatcher aren't reachable from the resolved root, get them via the `Win32WindowWrapper` already resolved in `TryResolveHwnd` (refactor `TryResolveHwnd` to also out the `wrapper`).

- [ ] **Step 2: Compile**

```
cd src && dotnet build Uno.UI.Runtime.Skia.Win32/Uno.UI.Runtime.Skia.Win32.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true
```
Expected: **builds**. Resolve any CsWin32 signature differences (`AdviseWithHWND`, `ScreenToClient`, `POINT`, `CoCreateInstance`) against the generated API.

- [ ] **Step 3: Stage**

```bash
dotnet format
git add src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs
```

---

## Task 7: No-op extensions for X11 / macOS / FrameBuffer

**Files:**
- Create: `src/Uno.UI.Runtime.Skia.X11/UI/ViewManagement/X11InputPaneExtension.cs`
- Create: `src/Uno.UI.Runtime.Skia.MacOS/UI/ViewManagement/MacOSInputPaneExtension.cs`
- Create: `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/UI/ViewManagement/FrameBufferInputPaneExtension.cs`
- Modify: `X11ApplicationHost.cs`, `MacSkiaHost.cs`, `FramebufferHost.cs` (registration blocks)

**Interfaces:**
- Produces three `IInputPaneExtension` no-op singletons returning `false`.

- [ ] **Step 1: Create the X11 no-op** (repeat the same shape for MacOS/FrameBuffer, changing namespace/class)

```csharp
#nullable enable
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.X11;

// No portable programmatic API exists to toggle the on-screen keyboard on X11 desktops
// (environment-specific: GNOME a11y, squeekboard/Wayland, onboard). Honest no-op.
internal sealed class X11InputPaneExtension : IInputPaneExtension
{
	internal static X11InputPaneExtension Instance { get; } = new();

	private X11InputPaneExtension() { }

	public bool TryShow() => false;

	public bool TryHide() => false;
}
```

MacOS variant comment: "macOS has no public API to invoke the Keyboard Viewer / Accessibility Keyboard; it is user-controlled." FrameBuffer variant comment: "No OS keyboard service on the Linux framebuffer host."

- [ ] **Step 2: Register in each host**

- `X11ApplicationHost` cctor: `ApiExtensibility.Register(typeof(IInputPaneExtension), _ => X11InputPaneExtension.Instance);`
- `MacSkiaHost` cctor: `ApiExtensibility.Register(typeof(IInputPaneExtension), _ => MacOSInputPaneExtension.Instance);`
- `FramebufferHost.InnerInitialize()`: `ApiExtensibility.Register(typeof(IInputPaneExtension), _ => FrameBufferInputPaneExtension.Instance);`

Add `using Windows.UI.ViewManagement;` where needed.

- [ ] **Step 3: Compile X11 + macOS runtimes**

```
cd src && dotnet build Uno.UI.Runtime.Skia.X11/Uno.UI.Runtime.Skia.X11.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true
```
Expected: builds. (macOS/FrameBuffer build on their platforms; at minimum verify they compile in CI.)

- [ ] **Step 4: Stage**

```bash
dotnet format
git add src/Uno.UI.Runtime.Skia.X11/UI/ViewManagement/X11InputPaneExtension.cs \
        src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs \
        src/Uno.UI.Runtime.Skia.MacOS/UI/ViewManagement/MacOSInputPaneExtension.cs \
        src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs \
        src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/UI/ViewManagement/FrameBufferInputPaneExtension.cs \
        src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Hosting/FramebufferHost.cs
```

---

## Task 8: SamplesApp sample (manual touch validation)

**Files:**
- Create: `src/SamplesApp/SamplesApp.Samples/Samples/Windows_UI_ViewManagement/SoftwareKeyboardSample.xaml` (+ `.xaml.cs`)

**Interfaces:** none (UI sample).

- [ ] **Step 1: Create the code-behind**

```csharp
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement", Name = "InputPane_SoftwareKeyboard",
	Description = "TextBox/PasswordBox in a ScrollViewer for touch-keyboard validation (issue #17363).")]
public sealed partial class SoftwareKeyboardSample
{
	public SoftwareKeyboardSample() => this.InitializeComponent();
}
```

- [ ] **Step 2: Create the XAML** — `TextBox` + `PasswordBox` near the bottom of a `ScrollViewer`, plus buttons calling `InputPane.GetForCurrentView().TryShow()/.TryHide()` to exercise the public API, with `{ThemeResource}` backgrounds.

```xml
<UserControl x:Class="SamplesApp.Windows_UI_ViewManagement.SoftwareKeyboardSample"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
	<Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
		<ScrollViewer>
			<StackPanel Spacing="12" Padding="16">
				<TextBlock TextWrapping="Wrap"
				           Text="Tap a field with touch/pen: the software keyboard should appear (Windows tablet) and the field should scroll into view. Mouse focus should NOT show it." />
				<!-- spacer to push fields down -->
				<Border Height="600" Background="{ThemeResource SystemControlBackgroundBaseLowBrush}" />
				<TextBox Header="Text" PlaceholderText="Tap me" />
				<PasswordBox Header="Password" PlaceholderText="Tap me" />
			</StackPanel>
		</ScrollViewer>
	</Grid>
</UserControl>
```

- [ ] **Step 3: Format + build SamplesApp**

```
dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/Samples/Windows_UI_ViewManagement/SoftwareKeyboardSample.xaml
```
Build/run SamplesApp.Skia (Win32) and confirm the sample appears.

- [ ] **Step 4: Manual validation (record results in the PR)**

On a Windows touch device: tap `TextBox` → keyboard shows on the same screen, field scrolls into view; tap `PasswordBox` → keyboard shows; tap a non-text area → keyboard hides; mouse-click a field → no keyboard. On a multi-monitor extended setup, confirm the keyboard appears on the touch screen's monitor.

- [ ] **Step 5: Stage**

```bash
dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/Samples/Windows_UI_ViewManagement/SoftwareKeyboardSample.xaml
git add src/SamplesApp/SamplesApp.Samples/Samples/Windows_UI_ViewManagement/
```

---

## Final validation checklist

- [ ] **Compile:** `dotnet build Uno.UI-Skia-only.slnf -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true` succeeds.
- [ ] **Runtime tests (Skia Desktop):** all `Given_TextBox_SoftwareKeyboard` tests pass — touch shows, mouse/keyboard don't, blur hides, no-flicker on TextBox→TextBox, PasswordBox shows.
- [ ] **Manual (Windows touch device):** keyboard shows/hides per Task 8; correct monitor in extended mode.
- [ ] **No-op platforms compile** (X11/macOS/FrameBuffer).
- [ ] **`dotnet format`** clean; new `.cs` files are UTF-8 BOM + CRLF.
- [ ] **Issue link:** PR references `closes #17363`.
