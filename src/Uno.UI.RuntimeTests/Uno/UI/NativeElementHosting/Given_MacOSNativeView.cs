#if __SKIA__
#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_NativeElementHosting;

[TestClass]
[RunsOnUIThread]
public class Given_MacOSNativeView
{
	private const string LibObjC = "/usr/lib/libobjc.A.dylib";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	public void When_Handle_Is_Zero_Then_Throws()
		=> Assert.ThrowsExactly<ArgumentException>(() => new MacOSNativeView(IntPtr.Zero));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public void When_Handle_Is_Not_A_View_Then_Throws()
	{
		var obj = objc_msgSend(objc_msgSend(objc_getClass("NSObject"), sel_registerName("alloc")), sel_registerName("init"));

		try
		{
			Assert.ThrowsExactly<ArgumentException>(() => new MacOSNativeView(obj));
		}
		finally
		{
			Release(obj);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Created_Off_The_UI_Thread_Then_Throws()
	{
		var view = CreateNSView();

		try
		{
			await Task.Run(() => Assert.ThrowsExactly<InvalidOperationException>(() => new MacOSNativeView(view)));
			Assert.AreEqual(0, GetSuperview(view));
		}
		finally
		{
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Hosted_Then_Detach_And_Reattach()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		// the wrapper now owns the view: drop the reference taken by `alloc`
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var panel = new StackPanel();
		var window = GetNativeWindow(WindowHelper.CurrentTestWindow);

		try
		{
			WindowHelper.WindowContent = panel;
			// An empty panel never gets a size, which the default loaded check waits for.
			await WindowHelper.WaitForLoaded(panel, p => p.IsLoaded);

			for (var i = 0; i < 3; i++)
			{
				panel.Children.Add(host);
				await WindowHelper.WaitForLoaded(host);
				await WindowHelper.WaitFor(() => GetWindow(view) == window, message: $"attach #{i}");
				await WindowHelper.WaitFor(() => GetFrame(view) is { Width: 200, Height: 100 }, message: $"arrange #{i}");
				Assert.IsFalse(IsHiddenOrHasHiddenAncestor(view), $"hidden #{i}");

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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Unconstrained_Then_Sized_By_The_View()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		Release(view);

		var host = new ContentControl { Content = wrapper };
		var vertical = new StackPanel();
		var horizontal = new StackPanel { Orientation = Orientation.Horizontal };

		try
		{
			vertical.Children.Add(host);
			WindowHelper.WindowContent = vertical;
			await WindowHelper.WaitForLoaded(host);
			Assert.AreEqual(50, host.ActualHeight);

			vertical.Children.Remove(host);
			horizontal.Children.Add(host);
			WindowHelper.WindowContent = horizontal;
			await WindowHelper.WaitForLoaded(host);
			Assert.AreEqual(100, host.ActualWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Opacity_And_Visibility_Change_Then_View_Follows()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var parent = new Border { Child = host };

		try
		{
			await UITestHelper.Load(parent);
			var window = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == window);

			parent.Opacity = 0.5;
			await WindowHelper.WaitFor(() => GetAlphaValue(GetSuperview(view)) == 0.5);

			bool IsShown() => GetWindow(view) == window && IsShownAt(GetSuperview(view), 100, 50);

			// Visibility reaches the native view through the frames, which detach the hosts they no longer draw.
			await UITestHelper.WaitForRender(2);
			host.Visibility = Visibility.Collapsed;
			await UITestHelper.WaitForRender(2);
			await WindowHelper.WaitFor(() => !IsShown(), message: "collapsed");

			host.Visibility = Visibility.Visible;
			await UITestHelper.WaitForRender(2);
			await WindowHelper.WaitFor(IsShown, message: "visible");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	[BackendCondition(ConditionMode.Exclude, RuntimeTestBackends.WebGpu)] // WebGPU on macOS keeps the previous native clip when a popup opens
	public async Task When_Popup_Over_View_Then_View_Is_Clipped()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var popup = new Popup { Child = new Border { Width = 50, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) } };
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = { host, popup },
		};

		try
		{
			await UITestHelper.Load(root);
			await WindowHelper.WaitFor(() => GetWindow(view) != 0);
			var container = GetSuperview(view);

			// The mask is updated as frames are presented.
			popup.IsOpen = true;
			await UITestHelper.WaitForRender(2);
			await WindowHelper.WaitFor(() => !IsShownAt(container, 25, 50) && IsShownAt(container, 150, 50), message: "masked under the popup");

			popup.IsOpen = false;
			await UITestHelper.WaitForRender(2);
			await WindowHelper.WaitFor(() => IsShownAt(container, 25, 50), message: "unmasked once the popup closed");
		}
		finally
		{
			popup.IsOpen = false;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
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
			var firstNativeWindow = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == firstNativeWindow);

			first.Child = null;
			secondWindow.Content = new Border { Child = host };
			secondWindow.Activate();
			await WindowHelper.WaitForLoaded(host);

			var secondNativeWindow = GetNativeWindow(secondWindow);
			Assert.AreNotEqual(firstNativeWindow, secondNativeWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == secondNativeWindow);
		}
		finally
		{
			secondWindow.Close();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Hosted_In_Two_Windows_Then_Both_Stay_Attached()
	{
		var firstView = CreateNSView();
		using var firstWrapper = new MacOSNativeView(firstView);
		Release(firstView);
		var secondView = CreateNSView();
		using var secondWrapper = new MacOSNativeView(secondView);
		Release(secondView);

		var firstHost = new ContentControl { Width = 200, Height = 100, Content = firstWrapper };
		var secondHost = new ContentControl { Width = 200, Height = 100, Content = secondWrapper };
		var secondWindow = new Window();

		try
		{
			await UITestHelper.Load(firstHost);
			var firstNativeWindow = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(firstView) == firstNativeWindow);

			// Only the second window's native z-order changes.
			secondWindow.Content = secondHost;
			secondWindow.Activate();
			await WindowHelper.WaitForLoaded(secondHost);
			var secondNativeWindow = GetNativeWindow(secondWindow);
			await WindowHelper.WaitFor(() => GetWindow(secondView) == secondNativeWindow);

			// Let both windows render a few frames.
			for (var i = 0; i < 5; i++)
			{
				await Task.Delay(50);
				await WindowHelper.WaitForIdle();
			}

			Assert.AreEqual(firstNativeWindow, GetWindow(firstView));
			Assert.AreEqual(secondNativeWindow, GetWindow(secondView));
		}
		finally
		{
			secondWindow.Close();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Window_Closed_Then_View_Can_Be_Hosted_Elsewhere()
	{
		var view = CreateNSView();
		using var wrapper = new MacOSNativeView(view);
		Release(view);

		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };
		var secondWindow = new Window { Content = new Border { Child = host } };
		var closed = false;

		try
		{
			secondWindow.Activate();
			await WindowHelper.WaitForLoaded(host);
			var secondNativeWindow = GetNativeWindow(secondWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == secondNativeWindow);

			secondWindow.Close();
			closed = true;
			await WindowHelper.WaitFor(() => GetWindow(view) == 0);

			// A new ContentControl: one moved out of a closed window keeps that window's XamlRoot.
			host.Content = null;
			var mainHost = new ContentControl { Width = 200, Height = 100, Content = wrapper };
			await UITestHelper.Load(mainHost);
			var window = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == window);
		}
		finally
		{
			if (!closed)
			{
				secondWindow.Close();
			}
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Disposed_Then_View_Is_Returned_As_Wrapped()
	{
		var view = CreateNSView();
		var wrapper = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };

		try
		{
			await UITestHelper.Load(host);
			await WindowHelper.WaitFor(() => GetFrame(view) is { Width: 200, Height: 100 });

			host.Content = null;
			wrapper.Dispose();

			// the app still owns the view through `alloc`
			Assert.AreEqual(0, GetSuperview(view));
			Assert.AreEqual(new CGRect(0, 0, 100, 50), GetFrame(view));
			Assert.AreEqual(0, GetAutoresizingMask(view));

			// a disposed wrapper must not reach native code again, which would crash the process
			host.Content = wrapper;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, GetSuperview(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Disposed_While_Hosted_Then_View_Is_Released()
	{
		var view = CreateNSView();
		var wrapper = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = wrapper };

		try
		{
			await UITestHelper.Load(host);
			await WindowHelper.WaitFor(() => GetWindow(view) != 0);

			wrapper.Dispose();
			Assert.AreEqual(0, GetSuperview(view));

			// layout and rendering go on around the released peer
			host.Width = 150;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(150, host.ActualWidth);
			Assert.AreEqual(0, GetSuperview(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Disposed_Then_View_Can_Be_Wrapped_Again()
	{
		var view = CreateNSView();
		var first = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = first };

		try
		{
			await UITestHelper.Load(host);
			var window = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == window);

			host.Content = null;
			first.Dispose();
			using var second = new MacOSNativeView(view);
			host.Content = second;
			await WindowHelper.WaitFor(() => GetWindow(view) == window);

			// nothing left from the first wrapper may take the view back out
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(window, GetWindow(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Wrapped_Twice_Then_Disposing_The_First_Leaves_The_View()
	{
		var view = CreateNSView();
		var first = new MacOSNativeView(view);
		// takes the view from the first wrapper's container
		using var second = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = second };

		try
		{
			await UITestHelper.Load(host);
			var window = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == window);

			first.Dispose();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(window, GetWindow(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Wrapped_Twice_Then_Collecting_The_First_Leaves_The_View()
	{
		var view = CreateNSView();
		var weakFirst = CreateUnattachedWrapper(view);
		using var second = new MacOSNativeView(view);
		var host = new ContentControl { Width = 200, Height = 100, Content = second };

		try
		{
			await UITestHelper.Load(host);
			var window = GetNativeWindow(WindowHelper.CurrentTestWindow);
			await WindowHelper.WaitFor(() => GetWindow(view) == window);

			// a collected wrapper's release is queued to the main thread, so it runs after the view moved on
			await Collect(weakFirst);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(window, GetWindow(view));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Unattached_Wrapper_Is_Collected_Then_View_Is_Released()
	{
		var view = CreateNSView();

		try
		{
			await Collect(CreateUnattachedWrapper(view));
			await WindowHelper.WaitFor(() => GetSuperview(view) == 0);
		}
		finally
		{
			Release(view);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25639")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Detached_Wrapper_Is_Collected_Then_View_Is_Released()
	{
		var view = CreateNSView();
		var panel = new StackPanel();

		try
		{
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel, p => p.IsLoaded);

			await Collect(await HostAndRemove(panel, view));
			await WindowHelper.WaitFor(() => GetSuperview(view) == 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			Release(view);
		}
	}

	private static async Task Collect(WeakReference reference)
	{
		for (var i = 0; i < 10 && reference.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WindowHelper.WaitForIdle();
		}

		Assert.IsFalse(reference.IsAlive);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateUnattachedWrapper(nint view) => new(new MacOSNativeView(view));

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> HostAndRemove(Panel panel, nint view)
	{
		var host = new ContentControl { Width = 200, Height = 100, Content = new MacOSNativeView(view) };
		panel.Children.Add(host);
		await WindowHelper.WaitForLoaded(host);
		await WindowHelper.WaitFor(() => GetWindow(view) != 0);

		panel.Children.Remove(host);
		await WindowHelper.WaitFor(() => GetWindow(view) == 0);

		return new(host.Content);
	}

	private static nint CreateNSView()
	{
		var view = objc_msgSend(objc_msgSend(objc_getClass("NSView"), sel_registerName("alloc")), sel_registerName("init"));
		objc_msgSend_size(view, sel_registerName("setFrameSize:"), 100, 50);
		return view;
	}

	// MacOSWindowNative is internal to the macOS runtime, which the tests don't reference.
	private static nint GetNativeWindow(Window window)
	{
		var native = window.NativeWindow!;
		return (nint)native.GetType().GetProperty("Handle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(native)!;
	}

	private static void Release(nint obj) => objc_msgSend(obj, sel_registerName("release"));

	private static nint GetWindow(nint view) => objc_msgSend(view, sel_registerName("window"));

	private static nint GetSuperview(nint view) => objc_msgSend(view, sel_registerName("superview"));

	private static nint GetAutoresizingMask(nint view) => objc_msgSend(view, sel_registerName("autoresizingMask"));

	private static double GetAlphaValue(nint view) => objc_msgSend_double(view, sel_registerName("alphaValue"));

	private static bool IsHiddenOrHasHiddenAncestor(nint view) => objc_msgSend_bool(view, sel_registerName("isHiddenOrHasHiddenAncestor"));

	// Whether the point, in the view's coordinates, is inside the mask that keeps managed content drawn over the view visible.
	private static bool IsShownAt(nint view, double x, double y)
	{
		var mask = objc_msgSend(objc_msgSend(view, sel_registerName("layer")), sel_registerName("mask"));
		return mask == 0 || CGPathContainsPoint(objc_msgSend(mask, sel_registerName("path")), 0, new CGPoint(x, y), false);
	}

	// arm64 returns an NSRect in registers; x64 returns it through the hidden pointer `objc_msgSend_stret` takes.
	private static CGRect GetFrame(nint view)
		=> RuntimeInformation.ProcessArchitecture == Architecture.X64
			? objc_msgSend_stret_rect(view, sel_registerName("frame"))
			: objc_msgSend_rect(view, sel_registerName("frame"));

	[StructLayout(LayoutKind.Sequential)]
	private record struct CGRect(double X, double Y, double Width, double Height);

	[StructLayout(LayoutKind.Sequential)]
	private record struct CGPoint(double X, double Y);

	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool CGPathContainsPoint(nint path, nint transform, CGPoint point, [MarshalAs(UnmanagedType.I1)] bool eoFill);

	[DllImport(LibObjC)]
	private static extern nint objc_getClass(string name);

	[DllImport(LibObjC)]
	private static extern nint sel_registerName(string name);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend")]
	private static extern nint objc_msgSend(nint receiver, nint selector);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend")]
	private static extern void objc_msgSend_size(nint receiver, nint selector, double width, double height);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend")]
	private static extern double objc_msgSend_double(nint receiver, nint selector);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool objc_msgSend_bool(nint receiver, nint selector);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend")]
	private static extern CGRect objc_msgSend_rect(nint receiver, nint selector);

	[DllImport(LibObjC, EntryPoint = "objc_msgSend_stret")]
	private static extern CGRect objc_msgSend_stret_rect(nint receiver, nint selector);
}
#endif
