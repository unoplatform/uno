#if __SKIA__
#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_NativeElementHosting;

[TestClass]
[RunsOnUIThread]
[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
public class Given_MacOSNativeView
{
	[TestMethod]
	public void When_Handle_Is_Zero_Then_Throws()
		=> Assert.ThrowsExactly<ArgumentException>(() => new MacOSNativeView(IntPtr.Zero));

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Hosted_Then_Detach_And_Reattach()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		// the wrapper now owns the view: drop the reference taken by `alloc`
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var panel = new StackPanel();

		try
		{
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);

			for (var i = 0; i < 3; i++)
			{
				panel.Children.Add(host);
				await WindowHelper.WaitForLoaded(host);
				await WindowHelper.WaitFor(() => GetWindow(view) != 0, message: $"attach #{i}");

				panel.Children.Remove(host);
				await WindowHelper.WaitFor(() => GetWindow(view) == 0, message: $"detach #{i}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Moved_Between_Windows()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var first = new Border();
		var secondWindow = new Window();
		WindowHelper.WindowContent = first;

		try
		{
			first.Child = host;
			await WindowHelper.WaitForLoaded(host);
			await WindowHelper.WaitFor(() => GetWindow(view) != 0);
			var firstNativeWindow = GetWindow(view);

			first.Child = null;
			secondWindow.Content = new Border { Child = host };
			secondWindow.Activate();
			await WindowHelper.WaitForLoaded(host);

			await WindowHelper.WaitFor(() => GetWindow(view) is var w && w != 0 && w != firstNativeWindow);
		}
		finally
		{
			secondWindow.Close();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Disposed_Then_View_Is_Released_From_Host()
	{
		var view = CreateNSView();
		var wrapper = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };

		try
		{
			await UITestHelper.Load(host);
			await WindowHelper.WaitFor(() => GetWindow(view) != 0);

			host.Content = null;
			wrapper.Dispose();
			Assert.IsTrue(wrapper.IsDisposed);

			// the view is handed back to the app, which still owns it through `alloc`
			await WindowHelper.WaitFor(() => GetSuperview(view) == 0);

			// a disposed wrapper must not be handed to native code again
			host.Content = wrapper;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, GetWindow(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Unattached_Wrapper_Is_Collected_Then_View_Is_Released()
	{
		var view = CreateNSView();
		var weakWrapper = CreateUnattachedWrapper(view);

		for (var i = 0; i < 10 && weakWrapper.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WindowHelper.WaitForIdle();
		}

		Assert.IsFalse(weakWrapper.IsAlive);
		await WindowHelper.WaitFor(() => GetSuperview(view) == 0);
		Release(view);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateUnattachedWrapper(nint view) => new(new MacOSNativeView(view));

	private static nint CreateNSView()
	{
		var view = objc_msgSend(objc_msgSend(objc_getClass("NSView"), sel_registerName("alloc")), sel_registerName("init"));
		objc_msgSend_size(view, sel_registerName("setFrameSize:"), 100, 50);
		return view;
	}

	private static void Release(nint obj) => objc_msgSend(obj, sel_registerName("release"));

	private static nint GetWindow(nint view) => objc_msgSend(view, sel_registerName("window"));

	private static nint GetSuperview(nint view) => objc_msgSend(view, sel_registerName("superview"));

	[DllImport("/usr/lib/libobjc.A.dylib")]
	private static extern nint objc_getClass(string name);

	[DllImport("/usr/lib/libobjc.A.dylib")]
	private static extern nint sel_registerName(string name);

	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
	private static extern nint objc_msgSend(nint receiver, nint selector);

	[DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
	private static extern void objc_msgSend_size(nint receiver, nint selector, double width, double height);
}
#endif
