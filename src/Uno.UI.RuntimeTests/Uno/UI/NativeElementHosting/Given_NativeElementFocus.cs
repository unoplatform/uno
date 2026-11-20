#if __SKIA__
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_NativeElementHosting;

[TestClass]
[RunsOnUIThread]
public class Given_NativeElementFocus
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_Then_Host_Gets_Focus()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();

		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");
		Assert.AreEqual(FocusState.Unfocused, textBox.FocusState);
		Assert.IsTrue(native.HasFocus, "The native element should keep platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_And_Host_Redirects_Focus()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var redirectTarget = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };
		host.GettingFocus += (_, args) =>
		{
			if (args.NewFocusedElement == host)
			{
				args.TrySetNewFocusedElement(redirectTarget);
			}
		};

		await LoadAndFocus(textBox, host, native, redirectTarget);

		native.Focus();

		await WindowHelper.WaitFor(() => redirectTarget.FocusState != FocusState.Unfocused, message: "The redirect target should get XAML focus.");
		Assert.AreEqual(FocusState.Unfocused, host.FocusState);
		Assert.IsFalse(
			Xaml.Core.VisualTree.GetFocusManagerForElement(redirectTarget)!.IsNativeElementFocusOwner(redirectTarget),
			"Focus redirected away from the host is a regular focus change, so it must take platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_And_Host_Disallows_Focus_On_Interaction()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content, AllowFocusOnInteraction = false };

		await LoadAndFocus(textBox, host, native);

		native.Focus();

		// Uno specific: WinUI would keep the TextBox focused and mark the window as unfocused (#26295).
		// On WebAssembly this already passed before the sync, since the browser blurs the TextBox's input itself.
		await WindowHelper.WaitFor(() => textBox.FocusState == FocusState.Unfocused, message: "The TextBox should lose XAML focus.");
		Assert.AreEqual(FocusState.Unfocused, host.FocusState);
		Assert.IsNull(FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		Assert.IsTrue(native.HasFocus, "The native element should keep platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_And_Host_Is_Already_Focused()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);
		host.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		var focusState = host.FocusState; // Programmatic is coerced to the last input device's state.
		Assert.AreNotEqual(FocusState.Unfocused, focusState);

		native.Focus();

		var focusManager = Xaml.Core.VisualTree.GetFocusManagerForElement(host)!;
		await WindowHelper.WaitFor(() => focusManager.IsNativeElementFocusOwner(host), message: "The focused host should be recorded as holding the native element's focus.");
		Assert.AreEqual(focusState, host.FocusState, "Focus didn't move, so its state shouldn't change either.");
		Assert.IsTrue(native.HasFocus, "The native element should keep platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_In_Flyout_Gets_Focus()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentPresenter { Width = 200, Height = 50, Content = native.Content };
		var flyout = new Flyout { Content = new StackPanel { Children = { textBox, host } } };
		var anchor = new Button { Content = "Anchor" };

		WindowHelper.WindowContent = anchor;
		await WindowHelper.WaitForLoaded(anchor);
		flyout.ShowAt(anchor);
		try
		{
			await WindowHelper.WaitForLoaded(host);
			await native.WaitForReady();
			textBox.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			Assert.AreNotEqual(FocusState.Unfocused, textBox.FocusState);

			native.Focus();

			await WindowHelper.WaitFor(() => textBox.FocusState == FocusState.Unfocused, message: "The TextBox should lose XAML focus.");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(flyout.IsOpen, "The flyout should stay open.");
			Assert.IsTrue(native.HasFocus, "The native element should keep platform focus.");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Accessibility_Is_Enabled_While_Native_Element_Has_Focus()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();
		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");

		try
		{
			Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.EnableAccessibilityThroughDom();
			await WindowHelper.WaitFor(() => Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.SemanticElementExists(textBox), timeoutMS: 5000, message: "Timed out waiting for accessibility to be enabled.");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(FocusState.Pointer, host.FocusState);
			Assert.IsTrue(native.HasFocus, "Enabling accessibility must not pull browser focus out of the native element.");
		}
		finally
		{
			Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.DisableAccessibility();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_Then_TextBox_Can_Take_Focus_Back()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();
		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");

		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreNotEqual(FocusState.Unfocused, textBox.FocusState);
		Assert.AreEqual(FocusState.Unfocused, host.FocusState);

		// Win32 only takes focus back from a hosted window on a press on the XAML surface, not on a focus change from code.
		if (!OperatingSystem.IsWindows())
		{
			Assert.IsFalse(native.HasFocus, "The native element should give platform focus back.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Native_Element_Gets_Focus_And_Host_Is_Not_A_Tab_Stop()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content, IsTabStop = false };
		var outer = new ContentControl { Content = host };

		await LoadAndFocus(textBox, outer, native);

		native.Focus();

		await WindowHelper.WaitFor(() => outer.FocusState == FocusState.Pointer, message: "The nearest focusable ancestor should get XAML focus.");
		Assert.AreEqual(FocusState.Unfocused, textBox.FocusState);
		Assert.AreEqual(FocusState.Unfocused, host.FocusState);
		Assert.IsTrue(native.HasFocus, "The native element should keep platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Native_Element_Gets_And_Loses_Focus_Before_The_Event_Is_Processed()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		// The focus WinEvents are queued, so both are processed after platform focus is already back on the XAML window.
		native.Focus();
		native.Blur();
		await Task.Delay(200);
		await WindowHelper.WaitForIdle();

		Assert.AreNotEqual(FocusState.Unfocused, textBox.FocusState, "A late event must not move XAML focus away from where platform focus is now.");
		Assert.AreEqual(FocusState.Unfocused, host.FocusState);

		native.Focus();
		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should still get XAML focus when the native element keeps it.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaAndroid)]
	public async Task When_Focused_Native_Element_Is_Detached()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();
		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");
		var focusManager = Xaml.Core.VisualTree.GetFocusManagerForElement(host)!;
		Assert.IsTrue(focusManager.IsNativeElementFocusOwner(host));

		host.Content = new TextBlock { Text = "Replaced" };
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(FocusState.Pointer, host.FocusState);
		Assert.IsFalse(focusManager.IsNativeElementFocusOwner(host), "A detached native element no longer holds platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Focused_Native_Element_Loses_Focus_Outside_Xaml()
	{
		await using var native = FocusableNativeElement.Create();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();
		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");
		var focusManager = Xaml.Core.VisualTree.GetFocusManagerForElement(host)!;
		Assert.IsTrue(focusManager.IsNativeElementFocusOwner(host));

		native.Blur();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(native.HasFocus);
		Assert.IsFalse(focusManager.IsNativeElementFocusOwner(host), "The native element no longer holds platform focus.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26274")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Input_In_Hosted_Iframe_Gets_Focus_Then_Host_Gets_Focus()
	{
		await using var native = new FocusableNativeElement.HtmlIframeInput();
		var textBox = new TextBox();
		var host = new ContentControl { Width = 200, Height = 50, Content = native.Content };

		await LoadAndFocus(textBox, host, native);

		native.Focus();

		await WindowHelper.WaitFor(() => host.FocusState == FocusState.Pointer, message: "The host should get XAML focus.");
		Assert.AreEqual(FocusState.Unfocused, textBox.FocusState);
		Assert.IsTrue(native.HasFocus, "The input in the iframe should keep browser focus.");
	}

	private static async Task LoadAndFocus(TextBox textBox, FrameworkElement host, FocusableNativeElement native, UIElement? other = null)
	{
		var panel = new StackPanel { Children = { textBox, host } };
		if (other is not null)
		{
			panel.Children.Add(other);
		}

		WindowHelper.WindowContent = panel;
		await WindowHelper.WaitForLoaded(host);
		await native.WaitForReady();

		textBox.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.AreNotEqual(FocusState.Unfocused, textBox.FocusState);
	}

	private abstract class FocusableNativeElement : IAsyncDisposable
	{
		public static FocusableNativeElement Create()
			=> OperatingSystem.IsBrowser() ? new HtmlInput()
			: OperatingSystem.IsAndroid() ? new AndroidEditText()
			: new Win32Edit();

		public abstract object Content { get; }

		public abstract bool HasFocus { get; }

		public abstract void Focus();

		/// <summary>Moves platform focus out of the native element, without going through XAML.</summary>
		public abstract void Blur();

		public virtual Task WaitForReady() => WindowHelper.WaitForIdle();

		public async ValueTask DisposeAsync()
		{
			// Give platform focus back and detach the native element before releasing it, so later tests start clean.
			Blur();
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			DisposeNative();
		}

		protected virtual void DisposeNative() { }

		private sealed class HtmlInput : FocusableNativeElement
		{
			private readonly BrowserHtmlElement _element = BrowserHtmlElement.CreateHtmlElement("input");

			public override object Content => _element;

			public override bool HasFocus => _element.ExecuteJavascript("return String(document.activeElement === element)") == "true";

			public override void Focus() => _element.ExecuteJavascript("element.focus()");

			public override void Blur() => _element.ExecuteJavascript("element.blur()");

			protected override void DisposeNative() => _element.Dispose();
		}

		// WebView2 on WebAssembly is an iframe: focus moving into its document is reported on the iframe element.
		public sealed class HtmlIframeInput : FocusableNativeElement
		{
			private const string Input = "element.contentDocument.querySelector('input')";

			private readonly BrowserHtmlElement _element = BrowserHtmlElement.CreateHtmlElement("iframe");

			public HtmlIframeInput() => _element.SetHtmlAttribute("srcdoc", "<input>");

			public override object Content => _element;

			public override bool HasFocus => _element.ExecuteJavascript($"return String(document.activeElement === element && element.contentDocument.activeElement === {Input})") == "true";

			public override void Focus() => _element.ExecuteJavascript($"{Input}.focus()");

			public override void Blur() => _element.ExecuteJavascript("element.contentDocument?.activeElement?.blur(); element.blur()");

			// srcdoc loads asynchronously.
			public override Task WaitForReady() => WindowHelper.WaitFor(
				() => _element.ExecuteJavascript($"return String(element.contentDocument?.readyState === 'complete' && !!{Input})") == "true",
				message: "The iframe document should be loaded.");

			protected override void DisposeNative() => _element.Dispose();
		}

		// The runtime tests don't reference Mono.Android, hence the reflection.
		private sealed class AndroidEditText : FocusableNativeElement
		{
			private readonly IDisposable _view;

			[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties, "Android.Widget.EditText", "Mono.Android")]
			[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
			[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this is used on.")]
			public AndroidEditText()
			{
				var host = XamlRootMap.GetHostForRoot(WindowHelper.XamlRoot)!;
				var activity = host.GetType()
					.GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
					.GetValue(host)!;
				var editTextType = Type.GetType("Android.Widget.EditText, Mono.Android", throwOnError: true)!;
				_view = (IDisposable)Activator.CreateInstance(editTextType, activity)!;
			}

			public override object Content => _view;

			public override bool HasFocus => (bool)Get("IsFocused");

			public override void Focus() => _view.GetType().GetMethod("RequestFocus", Type.EmptyTypes)!.Invoke(_view, null);

			public override void Blur() => _view.GetType().GetMethod("ClearFocus", Type.EmptyTypes)!.Invoke(_view, null);

			public override Task WaitForReady() => WindowHelper.WaitFor(() => (bool)Get("IsAttachedToWindow"), message: "The hosted view should be attached.");

			protected override void DisposeNative() => _view.Dispose();

			private object Get(string property) => _view.GetType().GetProperty(property)!.GetValue(_view)!;
		}

		private sealed class Win32Edit : FocusableNativeElement
		{
			private const uint WS_CHILD = 0x40000000;
			private const uint WS_VISIBLE = 0x10000000;

			private readonly IntPtr _parent;
			private readonly IntPtr _hwnd;

			public Win32Edit()
			{
				_parent = ((Win32NativeWindow)WindowHelper.XamlRoot.HostWindow!.NativeWindow!).Hwnd;
				_hwnd = CreateWindowExW(0, "EDIT", "", WS_CHILD | WS_VISIBLE, 0, 0, 200, 50, _parent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				Assert.AreNotEqual(IntPtr.Zero, _hwnd, $"CreateWindowExW failed: {Marshal.GetLastPInvokeError()}");
			}

			public override object Content => new Win32NativeWindow(_hwnd);

			public override bool HasFocus => GetFocus() == _hwnd;

			public override void Focus() => SetFocus(_hwnd);

			public override void Blur() => SetFocus(_parent);

			// The hosting only shows the window once a render has consumed its arrange.
			public override Task WaitForReady() => WindowHelper.WaitFor(() => IsWindowVisible(_hwnd), message: "The hosted window should be shown.");

			protected override void DisposeNative() => DestroyWindow(_hwnd);

			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

			[DllImport("user32.dll")]
			private static extern IntPtr SetFocus(IntPtr hwnd);

			[DllImport("user32.dll")]
			private static extern IntPtr GetFocus();

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool IsWindowVisible(IntPtr hwnd);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool DestroyWindow(IntPtr hwnd);
		}
	}
}
#endif
