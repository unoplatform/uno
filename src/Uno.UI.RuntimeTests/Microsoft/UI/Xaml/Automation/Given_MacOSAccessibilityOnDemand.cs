#if __SKIA__
#nullable enable
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// The macOS accessibility bridge only builds and maintains the native tree once an accessibility client queries the
/// window. Uses reflection for the runtime-specific types, as <see cref="Given_MultiWindowAccessibility"/> does.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
public class Given_MacOSAccessibilityOnDemand
{
	[TestMethod]
	public async Task When_No_Client_Queried_Then_Tree_Is_Built_On_First_Query()
	{
		var window = new Window();
		try
		{
			var button = new Button { Content = "On demand" };
			window.Content = button;
			var activated = false;
			window.Activated += (_, _) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForLoaded(button);
			await TestServices.WindowHelper.WaitForIdle();

			var accessibility = GetAccessibility(button.XamlRoot!);
			Assert.IsFalse(IsAccessibilityEnabled(accessibility), "no client has queried the window yet");
			Assert.IsFalse(IsTreeInitialized(accessibility), "the native tree must not be built before a client asks");

			// What an accessibility client's first query does: ask the window for its children.
			var windowHandle = (nint)accessibility.GetType().GetProperty("WindowHandle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(accessibility)!;
			var children = objc_msgSend(windowHandle, sel_registerName("accessibilityChildren"));
			var count = objc_msgSend_nuint(children, sel_registerName("count"));

			Assert.IsTrue(IsAccessibilityEnabled(accessibility), "the first query must enable the bridge");
			Assert.IsTrue(IsTreeInitialized(accessibility), "the first query must build the tree");
			Assert.IsTrue(count >= 1, $"the first query must already see the root element (got {count} children)");
		}
		finally
		{
			window.Close();
		}
	}

	private static object GetAccessibility(XamlRoot xamlRoot)
	{
		var xamlRootMap = typeof(XamlRoot).Assembly.GetType("Uno.UI.Hosting.XamlRootMap")!;
		var host = xamlRootMap.GetMethod("GetHostForRoot", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new object[] { xamlRoot })!;
		return host.GetType().GetField("_accessibility", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)
			?? throw new InvalidOperationException("The window host has no accessibility instance.");
	}

	private static bool IsAccessibilityEnabled(object accessibility)
		=> accessibility.GetType().GetProperty("IsAccessibilityEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(accessibility) is true;

	private static bool IsTreeInitialized(object accessibility)
		=> accessibility.GetType().GetField("_accessibilityTreeInitialized", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(accessibility) is true;

	[DllImport("/usr/lib/libobjc.A.dylib")]
	private static extern nint sel_registerName(string name);

	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
	private static extern nint objc_msgSend(nint receiver, nint selector);

	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
	private static extern nuint objc_msgSend_nuint(nint receiver, nint selector);
}
#endif
