#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Win32 UIA bridge parity tests (W32-07 / W32-09). They exercise the native bridge from the
	/// outside — the window's <c>WM_GETOBJECT</c> contract and the UIA reserved-value interop —
	/// because the provider types themselves are internal to <c>Uno.UI.Runtime.Skia.Win32</c>.
	/// </summary>
	[TestClass]
	public class Given_Win32UiaBridge
	{
#if __SKIA__
		private const uint WM_GETOBJECT = 0x003D;

		// WM_GETOBJECT object ids: UIA providers answer UiaRootObjectId, Microsoft Active
		// Accessibility clients (and the UIA-to-MSAA bridge) ask for OBJID_CLIENT.
		private const int UiaRootObjectId = -25;
		private const int ObjIdClient = -4;

		[DllImport("user32.dll", EntryPoint = "SendMessageW")]
		private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumThreadWindows(uint threadId, EnumThreadWindowsProc callback, nint lParam);

		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		private delegate bool EnumThreadWindowsProc(nint hwnd, nint lParam);

		/// <summary>
		/// The UIA docs require providers to forward <c>WM_GETOBJECT</c>'s wParam/lParam to
		/// <c>UiaReturnRawElementProvider</c> "without filtering them first, because filtering can
		/// cause problems with Microsoft Active Accessibility clients", which is also what WinUI's
		/// <c>CJupiterControl::HandleGetObjectMessage</c> does. Filtering on
		/// <c>UiaRootObjectId</c> left every MSAA client falling through to <c>DefWindowProc</c>.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_WmGetObject_For_MsaaClient_Then_Provider_Is_Served()
		{
			nint uiaWindow = 0;

			await UITestHelper.WaitFor(
				() =>
				{
					uiaWindow = FindWindowServingUiaRoot();
					return uiaWindow != 0;
				},
				timeoutMS: 10000,
				message: "No window of the UI thread answered WM_GETOBJECT for UiaRootObjectId, so the UIA provider was never created.");

			var msaaResult = SendMessage(uiaWindow, WM_GETOBJECT, 0, ObjIdClient);

			Assert.AreNotEqual(
				(nint)0,
				msaaResult,
				"WM_GETOBJECT with OBJID_CLIENT must be answered by the UIA-to-MSAA bridge instead of DefWindowProc.");
		}

		/// <summary>
		/// A text range must return the process-wide UIA "not supported" sentinel for attributes it
		/// does not implement (WinUI maps <c>E_NOT_SUPPORTED</c> onto <c>m_punkNotSupportedValue</c>
		/// in <c>CUIATextRangeProviderWrapper::GetAttributeValue</c>). Clients compare the returned
		/// value against that exact instance, so it must be obtainable and stable.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public void When_ReservedNotSupportedValue_Requested_Then_Same_Sentinel_Is_Returned()
		{
			var interopType = Type.GetType(
				"Uno.UI.Runtime.Skia.Win32.Win32UIAutomationInterop, Uno.UI.Runtime.Skia.Win32",
				throwOnError: false);
			Assert.IsNotNull(interopType, "Unable to locate Uno.UI.Runtime.Skia.Win32.Win32UIAutomationInterop at runtime.");

			var method = interopType!.GetMethod(
				"GetReservedNotSupportedValue",
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			Assert.IsNotNull(method, "Unable to locate Win32UIAutomationInterop.GetReservedNotSupportedValue().");

			var first = method!.Invoke(obj: null, parameters: null);
			var second = method.Invoke(obj: null, parameters: null);

			Assert.IsNotNull(first, "UiaGetReservedNotSupportedValue must produce the UIA sentinel.");
			Assert.AreSame(first, second, "The sentinel must be cached; clients compare it by identity.");
		}

		/// <summary>
		/// Returns the first window owned by the UI thread that answers <c>WM_GETOBJECT</c> for
		/// <c>UiaRootObjectId</c>, or <c>0</c>. The HWND is discovered instead of read from the
		/// <see cref="Microsoft.UI.Xaml.Window"/> because on Skia the window id is a synthetic
		/// counter rather than the native handle.
		/// </summary>
		private static nint FindWindowServingUiaRoot()
		{
			var candidates = new List<nint>();
			EnumThreadWindows(
				GetCurrentThreadId(),
				(hwnd, _) =>
				{
					candidates.Add(hwnd);
					return true;
				},
				0);

			foreach (var hwnd in candidates)
			{
				if (SendMessage(hwnd, WM_GETOBJECT, 0, UiaRootObjectId) != 0)
				{
					return hwnd;
				}
			}

			return 0;
		}
#endif
	}
}
