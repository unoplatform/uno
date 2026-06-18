#nullable enable
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Shows/hides the Windows touch keyboard via the WinRT InputPane interop (the WPF/Firefox
/// approach), and forwards the keyboard occlusion rectangle back to <see cref="InputPane"/>
/// via the plain-COM IFrameworkInputPane so layout can pan the focused field into view.
/// </summary>
internal sealed class Win32InputPaneExtension : IInputPaneExtension
{
	// IInputPaneInterop / IInputPane2 are IInspectable-derived WinRT interfaces. Modern .NET
	// removed built-in IInspectable marshalling, so we declare them as IUnknown and pad the 3
	// IInspectable vtable slots (GetIids/GetRuntimeClassName/GetTrustLevel) so the real methods
	// land at the correct vtable offset. Pointers are marshalled manually (no IInspectable/HString).
	[ComImport]
	[Guid("75CF2C57-9195-4931-8332-F0B409E916AF")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IInputPaneInterop
	{
		void GetIids(out uint iidCount, out IntPtr iids);
		void GetRuntimeClassName(out IntPtr className);
		void GetTrustLevel(out int trustLevel);

		IntPtr GetForWindow(IntPtr appWindow, [In] ref Guid riid);
	}

	[ComImport]
	[Guid("8A6B3F26-7090-4793-944C-C3F2CDE26276")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IInputPane2
	{
		void GetIids(out uint iidCount, out IntPtr iids);
		void GetRuntimeClassName(out IntPtr className);
		void GetTrustLevel(out int trustLevel);

		[return: MarshalAs(UnmanagedType.U1)]
		bool TryShow();

		[return: MarshalAs(UnmanagedType.U1)]
		bool TryHide();
	}

	[ComImport]
	[Guid("5752238B-24F0-495A-82F1-2FD593056796")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFrameworkInputPane
	{
		void Advise([MarshalAs(UnmanagedType.IUnknown)] object pWindow, IFrameworkInputPaneHandler pHandler, out uint pdwCookie);

		void AdviseWithHWND(IntPtr hwnd, IFrameworkInputPaneHandler pHandler, out uint pdwCookie);

		void Unadvise(uint dwCookie);

		void Location(out RECT prcInputPaneScreenLocation);
	}

	[ComImport]
	[Guid("EB3D7A2C-B0FE-49D4-9D85-D04E69A2A89E")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFrameworkInputPaneHandler
	{
		void Showing(ref RECT prcInputPaneScreenLocation, [MarshalAs(UnmanagedType.U1)] bool fEnsureFocusedElementInView);

		void Hiding([MarshalAs(UnmanagedType.U1)] bool fEnsureFocusedElementInView);
	}

	[DllImport("combase.dll", PreserveSig = true)]
	private static extern int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

	[DllImport("combase.dll", PreserveSig = true)]
	private static extern int WindowsDeleteString(IntPtr hstring);

	[DllImport("combase.dll", PreserveSig = true)]
	private static extern int RoGetActivationFactory(IntPtr activatableClassId, [In] ref Guid iid, out IntPtr factory);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow(); // DIAG

	private static readonly Guid s_clsidFrameworkInputPane = new("D5120AA3-46BA-44C5-822D-CA8092C1FC72");
	private const string InputPaneRuntimeClass = "Windows.UI.ViewManagement.InputPane";

	private readonly InputPane _owner;
	private IInputPaneInterop? _interop;
	private IFrameworkInputPane? _frameworkInputPane;
	private uint _adviseCookie;
	private HWND _advisedHwnd;

	public Win32InputPaneExtension(InputPane owner) => _owner = owner;

	public bool TryShow()
	{
		var resolved = TryGetWinRtInputPane(out var inputPane);
		System.Console.WriteLine($"[SoftKeyboard][Win32] TryShow: winrtInputPaneResolved={resolved}"); // DIAG
		if (!resolved)
		{
			return false;
		}

		var shown = inputPane.TryShow();
		System.Console.WriteLine($"[SoftKeyboard][Win32] IInputPane2.TryShow() returned {shown}"); // DIAG
		return shown;
	}

	public bool TryHide() => TryGetWinRtInputPane(out var inputPane) && inputPane.TryHide();

	private bool TryGetWinRtInputPane(out IInputPane2 inputPane)
	{
		inputPane = null!;

		if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
		{
			System.Console.WriteLine("[SoftKeyboard][Win32] skipped: OS < Windows 10 1607"); // DIAG
			return false;
		}

		if (!TryResolveHwnd(out var hwnd, out _))
		{
			System.Console.WriteLine("[SoftKeyboard][Win32] skipped: could not resolve HWND"); // DIAG
			return false;
		}

		System.Console.WriteLine($"[SoftKeyboard][Win32] resolved HWND=0x{hwnd.Value:X}, foreground=0x{GetForegroundWindow():X}"); // DIAG

		try
		{
			_interop ??= GetInterop();
			var iid = typeof(IInputPane2).GUID;
			var ptr = _interop.GetForWindow(hwnd.Value, ref iid);
			System.Console.WriteLine($"[SoftKeyboard][Win32] GetForWindow ptr=0x{ptr:X}"); // DIAG
			if (ptr == IntPtr.Zero)
			{
				return false;
			}

			inputPane = (IInputPane2)Marshal.GetObjectForIUnknown(ptr);
			Marshal.Release(ptr);

			EnsureAdvised(hwnd);
			return true;
		}
		catch (Exception e)
		{
			System.Console.WriteLine($"[SoftKeyboard][Win32] EXCEPTION: {e}"); // DIAG
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to acquire the WinRT InputPane for the window.", e);
			}
			return false;
		}
	}

	private static IInputPaneInterop GetInterop()
	{
		Marshal.ThrowExceptionForHR(WindowsCreateString(InputPaneRuntimeClass, InputPaneRuntimeClass.Length, out var hstring));
		try
		{
			var iid = typeof(IInputPaneInterop).GUID;
			Marshal.ThrowExceptionForHR(RoGetActivationFactory(hstring, ref iid, out var factory));
			var interop = (IInputPaneInterop)Marshal.GetObjectForIUnknown(factory);
			Marshal.Release(factory);
			return interop;
		}
		finally
		{
			WindowsDeleteString(hstring);
		}
	}

	private void EnsureAdvised(HWND hwnd)
	{
		if (_adviseCookie != 0 && _advisedHwnd == hwnd)
		{
			return;
		}

		Unadvise();

		try
		{
			_frameworkInputPane = (IFrameworkInputPane)Activator.CreateInstance(Type.GetTypeFromCLSID(s_clsidFrameworkInputPane)!)!;
			_frameworkInputPane.AdviseWithHWND(hwnd.Value, new OcclusionHandler(this), out _adviseCookie);
			_advisedHwnd = hwnd;
			System.Console.WriteLine($"[SoftKeyboard][Win32] AdviseWithHWND ok, cookie={_adviseCookie}"); // DIAG
		}
		catch (Exception e)
		{
			System.Console.WriteLine($"[SoftKeyboard][Win32] AdviseWithHWND EXCEPTION: {e.Message}"); // DIAG
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Failed to subscribe to IFrameworkInputPane occlusion notifications.", e);
			}
		}
	}

	private void Unadvise()
	{
		try
		{
			if (_frameworkInputPane is { } fip && _adviseCookie != 0)
			{
				fip.Unadvise(_adviseCookie);
			}
		}
		catch
		{
			// Best-effort teardown.
		}

		_adviseCookie = 0;
		_frameworkInputPane = null;
		_advisedHwnd = default;
	}

	private void OnOcclusionChanged(RECT screenRect)
	{
		if (!TryResolveHwnd(out var hwnd, out var xamlRoot))
		{
			return;
		}

		Rect occluded = default;
		if (screenRect.right > screenRect.left && screenRect.bottom > screenRect.top)
		{
			// Screen (virtual-desktop) pixels -> this window's client pixels -> client DIPs.
			var topLeft = new System.Drawing.Point(screenRect.left, screenRect.top);
			var bottomRight = new System.Drawing.Point(screenRect.right, screenRect.bottom);
			PInvoke.ScreenToClient(hwnd, ref topLeft);
			PInvoke.ScreenToClient(hwnd, ref bottomRight);

			var scale = xamlRoot.RasterizationScale;
			if (scale <= 0)
			{
				scale = 1;
			}

			occluded = new Rect(
				topLeft.X / scale,
				topLeft.Y / scale,
				(bottomRight.X - topLeft.X) / scale,
				(bottomRight.Y - topLeft.Y) / scale);
		}

		// OccludedRect must be mutated on the UI thread (it raises Showing/Hiding + pan-into-view).
		NativeDispatcher.Main.Enqueue(() => _owner.OccludedRect = occluded);
	}

	private bool TryResolveHwnd(out HWND hwnd, out XamlRoot xamlRoot)
	{
		hwnd = HWND.Null;
		xamlRoot = null!;

		// Prefer the window the focusing control targeted; fall back to the focused element's root.
		var root = _owner.TargetXamlRoot;
		if (root is null)
		{
			foreach (var pair in XamlRootMap.Enumerate())
			{
				if (FocusManager.GetFocusedElement(pair.Key) is not null)
				{
					root = pair.Key;
					break;
				}
			}
		}

		if (root is null || XamlRootMap.GetHostForRoot(root) is not Win32WindowWrapper wrapper)
		{
			return false;
		}

		if (wrapper.NativeWindow is not Win32NativeWindow nativeWindow)
		{
			return false;
		}

		hwnd = (HWND)nativeWindow.Hwnd;
		xamlRoot = root;
		return true;
	}

	private sealed class OcclusionHandler : IFrameworkInputPaneHandler
	{
		private readonly Win32InputPaneExtension _extension;

		public OcclusionHandler(Win32InputPaneExtension extension) => _extension = extension;

		public void Showing(ref RECT prcInputPaneScreenLocation, bool fEnsureFocusedElementInView)
		{
			System.Console.WriteLine($"[SoftKeyboard][Win32] IFrameworkInputPane.Showing fired: rect=({prcInputPaneScreenLocation.left},{prcInputPaneScreenLocation.top},{prcInputPaneScreenLocation.right},{prcInputPaneScreenLocation.bottom})"); // DIAG
			_extension.OnOcclusionChanged(prcInputPaneScreenLocation);
		}

		public void Hiding(bool fEnsureFocusedElementInView)
		{
			System.Console.WriteLine("[SoftKeyboard][Win32] IFrameworkInputPane.Hiding fired"); // DIAG
			_extension.OnOcclusionChanged(default);
		}
	}
}
