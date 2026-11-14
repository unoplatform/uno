using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

#if !WINAPPSDK
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Islands;
using Windows.ApplicationModel.Core;
using Windows.UI;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using Colors = Microsoft.UI.Colors;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Window
{
	[TestCleanup]
	public void CleanupTest()
	{
		TestServices.WindowHelper.CloseAllSecondaryWindows();
	}

#if !HAS_UNO_WINUI && !WINAPPSDK
	[TestMethod]
	[RunsOnUIThread]
	public void When_Primary_Window_UWP()
	{
		// The current window on UWP should be a CoreWindow.
		var mainVisualTreeXamlRoot = WinUICoreServices.Instance.MainVisualTree?.XamlRoot;
		Assert.AreEqual(Window.Current.Content.XamlRoot, mainVisualTreeXamlRoot);
	}
#endif

#if HAS_UNO_WINUI || WINAPPSDK

	private bool SupportsMultipleWindows() =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	private void AssertSupportsMultipleWindows()
	{
		if (!SupportsMultipleWindows())
		{
			Assert.Inconclusive("This test can only run in an environment with multiwindow support");
		}
	}

#if HAS_UNO
	private void AssertDoesNotSupportMultipleWindows()
	{
		if (SupportsMultipleWindows())
		{
			Assert.Inconclusive("This test can only run in an environment without multiwindow support");
		}
	}

	private static void AssertIsFullFledgedApp()
	{
		if (!CoreApplication.IsFullFledgedApp)
		{
			Assert.Inconclusive("This test can only be run in a full-fledged app");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_CreateNewWindow_Unsupported()
	{
		AssertIsFullFledgedApp();
		AssertDoesNotSupportMultipleWindows();

		var act = () => new Window(WindowType.DesktopXamlSource);
		act.Should().Throw<InvalidOperationException>();
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Create_Multiple_Windows()
	{
		AssertIsFullFledgedApp();
		AssertSupportsMultipleWindows();

		var startingNumberOfWindows = ApplicationHelper.Windows.Count;

		for (int i = 0; i < 10; i++)
		{
			var sut = new Window(WindowType.DesktopXamlSource);
			sut.Close();
		}

		var endNumberOfWindows = ApplicationHelper.Windows.Count;
		Assert.AreEqual(startingNumberOfWindows, endNumberOfWindows);
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24679")]
	public async Task When_Secondary_Window_Closed_Then_Initial_Window_Visible()
	{
		AssertSupportsMultipleWindows();

		var initialWindow = TestServices.WindowHelper.CurrentTestWindow;
		var sut = new Window();

		var activated = false;
		sut.Activated += (s, e) => activated = true;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => activated);

		sut.Close();

		// On iOS, closing the scene that was in front must not leave the app without a foreground scene.
		await TestServices.WindowHelper.WaitFor(() => initialWindow.Visible, timeoutMS: 15000);
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24679")]
	public async Task When_Secondary_Window_Closed_Before_Shown_Then_Initial_Window_Visible()
	{
		AssertSupportsMultipleWindows();

		var initialWindow = TestServices.WindowHelper.CurrentTestWindow;
		var sut = new Window();

		sut.Activate();
		sut.Close();

		// On iOS the scene requested by Activate still connects after the close and gets discarded,
		// which must not take the app's foreground scene with it.
		await Task.Delay(TimeSpan.FromSeconds(2));
		await TestServices.WindowHelper.WaitFor(() => initialWindow.Visible, timeoutMS: 15000);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Secondary_Window_No_Background_Light_Dark()
	{
		AssertSupportsMultipleWindows();

		// Use application-level dark theme. Element-level theming on the main
		// window root no longer syncs to Application.Current.RequestedTheme,
		// so secondary windows must be tested with the application theme API.
		using var _ = ThemeHelper.UseApplicationDarkTheme();
		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.Black);
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_No_Background_Light()
	{
		AssertSupportsMultipleWindows();

		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.White);
	}

#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Secondary_Window_No_Background_Switch_Theme()
	{
		AssertSupportsMultipleWindows();

		var sut = new NoBackgroundWindow();

		await VerifyWindowBackgroundAsync(sut, false, Colors.White);

		// Use application-level dark theme to test that secondary window root
		// backgrounds update when the application theme changes.
		using var _ = ThemeHelper.UseApplicationDarkTheme();
		await VerifyWindowBackgroundAsync(sut, true, Colors.Black);
	}
#endif

	private static async Task VerifyWindowBackgroundAsync(Window sut, bool wasActivated, Color expectedColor)
	{
		if (!wasActivated)
		{
			bool activated = false;
			sut.Activated += (s, e) => activated = true;
			sut.Activate();

			await TestServices.WindowHelper.WaitFor(() => activated);
		}

		await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);

		var rootElement = sut.Content.XamlRoot.VisualTree.RootElement;
		Assert.IsInstanceOfType(rootElement, typeof(Panel));
		var rootElementAsPanel = (Panel)rootElement;
		var rootElementBackground = rootElementAsPanel.Background;
		Assert.IsInstanceOfType(rootElementBackground, typeof(SolidColorBrush));
		var rootElementBackgroundAsSolidColorBrush = (SolidColorBrush)rootElementBackground;
		Assert.AreEqual(expectedColor, rootElementBackgroundAsSolidColorBrush.Color);
	}

#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24189")]
	public async Task When_Transparent_Background_Then_Root_Keeps_Its_Brush()
	{
		var content = new Border { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = content;
		await TestServices.WindowHelper.WaitForLoaded(content);

		var root = (XamlIslandRoot)content.XamlRoot.VisualTree.RootElement;
		var background = root.Background;
		var previousMode = root.BackdropBackground;

		Assert.IsInstanceOfType(background, typeof(SolidColorBrush));

		try
		{
			root.SetBackdropBackground(BackdropBackgroundMode.Transparent);

			// Only the rendered primitive changes - the property keeps its theme brush, so a later
			// theme change still resolves against it.
			Assert.IsTrue(root.HasTransparentBackground);
			Assert.AreSame(background, root.Background);

			root.SetBackdropBackground(BackdropBackgroundMode.Fallback);

			Assert.IsFalse(root.HasTransparentBackground);
			Assert.AreSame(background, root.Background);

			root.SetBackdropBackground(BackdropBackgroundMode.None);

			Assert.AreSame(background, root.Background);
		}
		finally
		{
			root.SetBackdropBackground(previousMode);
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24189")]
	public async Task When_SystemBackdrop_Set_Then_Content_Background_Is_Untouched()
	{
		var red = new SolidColorBrush(Colors.Red);
		var content = new Grid { Background = red, Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = content;
		await TestServices.WindowHelper.WaitForLoaded(content);

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var previousBackdrop = window.SystemBackdrop;

		try
		{
			window.SystemBackdrop = new MicaBackdrop();
			await TestServices.WindowHelper.WaitForIdle();

			// WinUI leaves app content alone when a backdrop is applied, so an opaque page keeps
			// hiding the material rather than being rewritten to transparent.
			Assert.AreSame(red, content.Background);

			window.SystemBackdrop = null;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreSame(red, content.Background);
		}
		finally
		{
			window.SystemBackdrop = previousBackdrop;
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_Vulkan_Then_Device_Is_DXGIs_First_Hardware_Adapter()
	{
		// WinUI renders on DXGI's first hardware adapter: the primary display's, or the app's choice in the Windows graphics settings.
		var wrapper = TestServices.WindowHelper.CurrentTestWindow.NativeWrapper;
		var context = GetField(wrapper, "_context");
		if (context?.GetType().GetProperty("Kind")?.GetValue(context)?.ToString() != "Vulkan")
		{
			Assert.Inconclusive("The window did not negotiate a Vulkan context.");
		}

		var vulkan = GetField(context, "_vk");
		var deviceLuid = (long?)vulkan!.GetType().GetProperty("DeviceLuid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vulkan);

		if (deviceLuid is null)
		{
			Assert.Inconclusive("The driver doesn't report an adapter LUID.");
		}

		Assert.AreEqual(GetFirstHardwareAdapter()?.AdapterLuid, deviceLuid);

		static object GetField(object target, string name)
			=> target?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_WebGpu_Then_Adapter_Is_DXGIs_First_Hardware_Adapter()
	{
		const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

		var wrapper = TestServices.WindowHelper.CurrentTestWindow.NativeWrapper;
		var context = wrapper.GetType().GetField("_context", Private)!.GetValue(wrapper);
		if (context?.GetType().GetProperty("Kind")?.GetValue(context)?.ToString() != "WebGpu")
		{
			Assert.Inconclusive("The window did not negotiate a WebGPU context.");
		}

		var device = context.GetType().GetField("_device", Private)!.GetValue(context)!;
		if ((bool)device.GetType().GetProperty("IsSoftwareVulkan")!.GetValue(device)!)
		{
			Assert.Inconclusive("The adapter rasterizes on the CPU, which DXGI doesn't list as hardware.");
		}

		var adapterInfo = context.GetType().GetProperty("AdapterInfo", Private)!.GetValue(context)!;
		var expected = GetFirstHardwareAdapter()!.Value;

		// wgpu's OpenGL backend reports only a name.
		var vendorId = (uint)adapterInfo.GetType().GetProperty("VendorId")!.GetValue(adapterInfo)!;
		if (vendorId != 0)
		{
			Assert.AreEqual(expected.VendorId, vendorId);
			Assert.AreEqual(expected.DeviceId, (uint)adapterInfo.GetType().GetProperty("DeviceId")!.GetValue(adapterInfo)!);
		}
		else
		{
			StringAssert.StartsWith((string)adapterInfo.GetType().GetProperty("Name")!.GetValue(adapterInfo)!, expected.Description);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	[DataRow(0x8086u, 0xA7A0u, null, true, DisplayName = "Same ids")]
	[DataRow(0x8086u, 0x1234u, null, false, DisplayName = "Another device of the vendor")]
	[DataRow(0x10DEu, 0x2838u, null, false, DisplayName = "Another vendor")]
	[DataRow(0x8086u, 0u, "Intel Whatever The Driver Says", true, DisplayName = "OpenGL: only a vendor is known, and it matches")]
	[DataRow(0x10DEu, 0u, "NVIDIA RTX 4000 Ada Generation Laptop GPU/PCIe/SSE2", false, DisplayName = "OpenGL: only a vendor is known, and it differs")]
	[DataRow(0u, 0u, "Intel(R) Iris(R) Xe Graphics", true, DisplayName = "wgpu's OpenGL: named the same")]
	[DataRow(0u, 0u, "NVIDIA RTX 4000 Ada Generation Laptop GPU/PCIe/SSE2", false, DisplayName = "wgpu's OpenGL: named differently")]
	[DataRow(0u, 0u, null, false, DisplayName = "Nothing known, so the fallback rather than black")]
	public void When_Comparing_Renderer_With_Display_Adapter(uint vendorId, uint deviceId, string name, bool expected)
	{
		var type = Type.GetType("Uno.UI.Runtime.Win32.Win32DisplayAdapter, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var display = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)!
			.Invoke(null, [@"PCI\VEN_8086&DEV_A7A0&SUBSYS_0B041028&REV_04", "Intel(R) Iris(R) Xe Graphics"])!;

		Assert.AreEqual(0x8086u, type.GetProperty("VendorId")!.GetValue(display));
		Assert.AreEqual(0xA7A0u, type.GetProperty("DeviceId")!.GetValue(display));
		Assert.AreEqual(expected, type.GetMethod("Matches", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(display, [vendorId, deviceId, name]));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_Display_Adapter_Is_Not_On_PCI_Then_Only_Its_Name_Is_Known()
	{
		var type = Type.GetType("Uno.UI.Runtime.Win32.Win32DisplayAdapter, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var display = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)!
			.Invoke(null, [@"SWD\REMOTEDISPLAYENUM\RdpIdd_IndirectDisplay", "Microsoft Remote Display Adapter"])!;

		Assert.AreEqual(0u, type.GetProperty("VendorId")!.GetValue(display));
		Assert.AreEqual(0u, type.GetProperty("DeviceId")!.GetValue(display));

		var matches = type.GetMethod("Matches", BindingFlags.Instance | BindingFlags.NonPublic)!;
		Assert.IsFalse((bool)matches.Invoke(display, [0x8086u, 0xA7A0u, "Intel(R) Iris(R) Xe Graphics"])!);
		Assert.IsTrue((bool)matches.Invoke(display, [0u, 0u, "Microsoft Remote Display Adapter"])!);
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Swapchain_Missing_Then_Next_Frame_Recovers()
	{
		const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

		var wrapper = TestServices.WindowHelper.CurrentTestWindow.NativeWrapper;
		var context = wrapper.GetType().GetField("_context", Private)!.GetValue(wrapper);
		if (context?.GetType().GetProperty("Kind")?.GetValue(context)?.ToString() != "Vulkan")
		{
			Assert.Inconclusive("The window did not negotiate a Vulkan context.");
		}

		var vulkan = context.GetType().GetField("_vk", Private)!.GetValue(context)!;
		var display = vulkan.GetType().GetField("_display", Private)!.GetValue(vulkan)!;
		var swapchain = display.GetType().GetField("_swapchain", Private)!;
		ulong SwapchainHandle()
		{
			var value = swapchain.GetValue(display)!;
			return (ulong)value.GetType().GetField("Handle")!.GetValue(value)!;
		}

		// What a failed RecreateSurface leaves behind during a display transition.
		using (var deviceLock = (IDisposable)vulkan.GetType().GetMethod("Lock")!.Invoke(vulkan, null)!)
		{
			var deviceApi = vulkan.GetType().GetProperty("DeviceApi")!.GetValue(vulkan)!;
			deviceApi.GetType().GetMethod("DeviceWaitIdle")!.Invoke(deviceApi, [vulkan.GetType().GetProperty("DeviceHandle")!.GetValue(vulkan)]);
			display.GetType().GetMethod("DestroyCurrentImageViews", Private)!.Invoke(display, null);
			display.GetType().GetMethod("DestroySwapchain", Private)!.Invoke(display, null);
		}

		Assert.AreEqual(0UL, SwapchainHandle());

		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) });

		await TestServices.WindowHelper.WaitFor(() => SwapchainHandle() != 0);
		await TestServices.WindowHelper.WaitFor(() => (bool)context.GetType().GetProperty("PresentedLastFrame")!.GetValue(context)!);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_Probing_Vulkan_Devices_Then_Software_Comes_Last_In_Loader_Order()
	{
		var getProbeOrder = Type.GetType("Uno.UI.Runtime.Vulkan.Interop.VulkanDevice, Uno.UI.Runtime", throwOnError: true)!
			.GetMethod("GetProbeOrder", BindingFlags.Static | BindingFlags.NonPublic)!;
		var typeEnum = getProbeOrder.GetParameters()[0].ParameterType.GetElementType()!;

		// Loader order: software last, otherwise untouched, and a lone software device still gets probed.
		CollectionAssert.AreEqual(new[] { 1, 2, 0 }, Order(false, "CPU", "INTEGRATED_GPU", "DISCRETE_GPU"));
		CollectionAssert.AreEqual(new[] { 0, 1 }, Order(false, "DISCRETE_GPU", "INTEGRATED_GPU"));
		CollectionAssert.AreEqual(new[] { 0 }, Order(false, "CPU"));

		// Discrete first, for the platforms that ask for it.
		CollectionAssert.AreEqual(new[] { 1, 0, 2 }, Order(true, "INTEGRATED_GPU", "DISCRETE_GPU", "CPU"));

		int[] Order(bool preferDiscrete, params string[] names)
		{
			var types = Array.CreateInstance(typeEnum, names.Length);
			for (var i = 0; i < names.Length; i++)
			{
				types.SetValue(Enum.Parse(typeEnum, "VK_PHYSICAL_DEVICE_TYPE_" + names[i]), i);
			}

			return (int[])getProbeOrder.Invoke(null, [types, preferDiscrete])!;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(ElementTheme.Dark, (byte)0x20)]
	[DataRow(ElementTheme.Light, (byte)0xF3)]
	public async Task When_SystemBackdrop_Not_Rendered_Then_Root_Paints_Page_Background(ElementTheme theme, byte expected)
	{
		var content = new Border { Width = 100, Height = 100 };
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(content);

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var previousBackdrop = window.SystemBackdrop;

		// The fallback follows the theme of the window's content, as MUX's SystemBackdropConfiguration does.
		var windowContent = (FrameworkElement)window.Content;
		var previousTheme = windowContent.RequestedTheme;
		try
		{
			windowContent.RequestedTheme = theme;

			// No head renders a custom backdrop, just as Android, iOS, WebAssembly and X11 don't render Mica.
			window.SystemBackdrop = new UnrenderedBackdrop();
			await TestServices.WindowHelper.WaitForIdle();

			AssertPaintsPageBackground((XamlIslandRoot)content.XamlRoot.VisualTree.RootElement, expected);
		}
		finally
		{
			window.SystemBackdrop = previousBackdrop;
			windowContent.RequestedTheme = previousTheme;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(ElementTheme.Dark, (byte)0x20)]
	[DataRow(ElementTheme.Light, (byte)0xF3)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Window_On_Another_Adapters_Display_Then_Backdrop_Falls_Back_Until_It_Returns(ElementTheme theme, byte expected)
	{
		const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

		var wrapper = TestServices.WindowHelper.CurrentTestWindow.NativeWrapper;
		var context = wrapper.GetType().GetField("_context", Private)!.GetValue(wrapper);
		var kind = context?.GetType().GetProperty("Kind")?.GetValue(context)?.ToString();

		// The name a display would need to match each context, or the LUID for a Vulkan device.
		long deviceLuid = 0;
		string rendererName = null;
		switch (kind)
		{
			case "Vulkan":
				var vulkan = context.GetType().GetField("_vk", Private)!.GetValue(context)!;
				if (vulkan.GetType().GetProperty("DeviceLuid", Private)!.GetValue(vulkan) is not long luid)
				{
					Assert.Inconclusive("The driver doesn't report an adapter LUID.");
					return;
				}
				deviceLuid = luid;
				break;
			case "OpenGL":
				rendererName = (string)context.GetType().GetField("_renderer", Private)!.GetValue(context)!;
				break;
			case "WebGpu":
				var adapterInfo = context.GetType().GetProperty("AdapterInfo", Private)!.GetValue(context)!;
				rendererName = (string)adapterInfo.GetType().GetProperty("Name")!.GetValue(adapterInfo)!;
				if ((bool)adapterInfo.GetType().GetProperty("IsD3D12")!.GetValue(adapterInfo)!)
				{
					Assert.Inconclusive("wgpu's D3D12 path always falls back.");
				}
				break;
			default:
				Assert.Inconclusive("The window did not negotiate a context bound to an adapter.");
				return;
		}

		var content = new Border { Width = 100, Height = 100 };
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(content);
		var root = (XamlIslandRoot)content.XamlRoot.VisualTree.RootElement;

		var hwnd = ((Uno.UI.NativeElementHosting.Win32NativeWindow)wrapper.NativeWindow).Hwnd;
		var luidOverride = Type.GetType("Uno.UI.Runtime.Win32.Vulkan.Win32VulkanSurfaceFactory, Uno.UI.Runtime.Win32", throwOnError: true)!
			.GetProperty("PresentingAdapterLuidOverride", BindingFlags.Static | BindingFlags.NonPublic)!;
		var displayAdapterType = Type.GetType("Uno.UI.Runtime.Win32.Win32DisplayAdapter, Uno.UI.Runtime.Win32", throwOnError: true)!;
		var displayAdapterOverride = displayAdapterType.GetProperty("Override", BindingFlags.Static | BindingFlags.NonPublic)!;
		object NamedAdapter(string name) => displayAdapterType.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, ["", name]);

		void SimulateDisplayOn(bool contextsAdapter)
		{
			luidOverride.SetValue(null, contextsAdapter ? deviceLuid : deviceLuid + 1);
			displayAdapterOverride.SetValue(null, NamedAdapter(contextsAdapter && rendererName is not null ? rendererName : "Another Adapter"));
		}

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var previousBackdrop = window.SystemBackdrop;
		var windowContent = (FrameworkElement)window.Content;
		var previousTheme = windowContent.RequestedTheme;
		var previousSize = window.AppWindow.Size;

		// An OS without Mica stays on the fallback throughout.
		var materialMode = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported() ? BackdropBackgroundMode.Transparent : BackdropBackgroundMode.Fallback;
		try
		{
			windowContent.RequestedTheme = theme;
			SimulateDisplayOn(contextsAdapter: true);
			window.SystemBackdrop = new MicaBackdrop();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(materialMode, root.BackdropBackground, "The rendering adapter drives the display, so the material shows.");

			// What unplugging a screen or dragging the window to another adapter's screen ends in.
			SimulateDisplayOn(contextsAdapter: false);
			SendMessage(hwnd, WM_DISPLAYCHANGE, 0, 0);
			await TestServices.WindowHelper.WaitForIdle();
			AssertPaintsPageBackground(root, expected);

			SimulateDisplayOn(contextsAdapter: true);
			SendMessage(hwnd, WM_DISPLAYCHANGE, 0, 0);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(materialMode, root.BackdropBackground, "The material returns with the adapter.");

			// A resize alone can change the window's monitor, with no move: the largest overlap decides it.
			var backdropMonitor = wrapper.GetType().GetField("_backdropMonitor", BindingFlags.Instance | BindingFlags.NonPublic)!;
			backdropMonitor.SetValue(wrapper, Activator.CreateInstance(backdropMonitor.FieldType));
			SimulateDisplayOn(contextsAdapter: false);
			window.AppWindow.Resize(new Windows.Graphics.SizeInt32(previousSize.Width + 1, previousSize.Height));
			await TestServices.WindowHelper.WaitForIdle();
			AssertPaintsPageBackground(root, expected);
		}
		finally
		{
			luidOverride.SetValue(null, null);
			displayAdapterOverride.SetValue(null, null);
			window.AppWindow.Resize(previousSize);
			window.SystemBackdrop = previousBackdrop;
			windowContent.RequestedTheme = previousTheme;
			SendMessage(hwnd, WM_DISPLAYCHANGE, 0, 0);
		}
	}

	private static DXGI_ADAPTER_DESC1? GetFirstHardwareAdapter()
	{
		const int EnumAdapters1Slot = 12, GetDesc1Slot = 10;
		const uint WarpVendorId = 0x1414, WarpDeviceId = 0x8C;

		var iidFactory1 = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
		System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(CreateDXGIFactory1(ref iidFactory1, out var factory));
		try
		{
			var enumAdapters = VTableMethod<EnumAdapters1Delegate>(factory, EnumAdapters1Slot);
			for (uint i = 0; enumAdapters(factory, i, out var adapter) >= 0; i++)
			{
				try
				{
					System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(VTableMethod<GetDesc1Delegate>(adapter, GetDesc1Slot)(adapter, out var desc));
					if (desc.VendorId != WarpVendorId || desc.DeviceId != WarpDeviceId)
					{
						return desc;
					}
				}
				finally
				{
					System.Runtime.InteropServices.Marshal.Release(adapter);
				}
			}
		}
		finally
		{
			System.Runtime.InteropServices.Marshal.Release(factory);
		}

		return null;

		static T VTableMethod<T>(nint comObject, int slot) where T : Delegate
			=> System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<T>(
				System.Runtime.InteropServices.Marshal.ReadIntPtr(System.Runtime.InteropServices.Marshal.ReadIntPtr(comObject), slot * nint.Size));
	}

	private delegate int EnumAdapters1Delegate(nint factory, uint index, out nint adapter);

	private delegate int GetDesc1Delegate(nint adapter, out DXGI_ADAPTER_DESC1 desc);

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
	private struct DXGI_ADAPTER_DESC1
	{
		[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Description;
		public uint VendorId, DeviceId, SubSysId, Revision;
		public nuint DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
		public long AdapterLuid;
		public uint Flags;
	}

	[System.Runtime.InteropServices.DllImport("dxgi.dll")]
	private static extern int CreateDXGIFactory1(ref Guid riid, out nint factory);

	private static void AssertPaintsPageBackground(XamlIslandRoot root, byte expected)
	{
		Assert.AreEqual(BackdropBackgroundMode.Fallback, root.BackdropBackground);

		var brush = (Microsoft.UI.Composition.CompositionColorBrush)((Microsoft.UI.Composition.BorderVisual)root.Visual).BackgroundBrush;
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, expected, expected, expected), brush.Color);
	}

	private sealed class UnrenderedBackdrop : SystemBackdrop;

	private const uint WM_DISPLAYCHANGE = 0x007E;

	[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
	private static extern nint SendMessage(nint hWnd, uint msg, nuint wParam, nint lParam);
#endif

#endif

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Opens()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		try
		{
			bool activated = false;
			sut.Content = new Border();
			sut.Activated += (s, e) => activated = true;
			sut.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			Assert.IsTrue(activated);
			await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);
			Assert.IsGreaterThan(0, sut.Bounds.Width);
			Assert.IsGreaterThan(0, sut.Bounds.Height);
		}
		finally
		{
			sut.Close();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Content_Loads()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		bool loaded = false;
		var border = new Border();
		var button = new Button();
		button.Content = "Hello!";
		border.Child = button;
		sut.Content = border;
		button.Loaded += (s, e) => loaded = true;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => loaded);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_Content_Non_Zero_Size()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		var button = new Button();
		button.Content = "Hello!";
		sut.Content = button;
		sut.Activate();
		await TestServices.WindowHelper.WaitFor(() => button.ActualWidth > 0);
		Assert.IsGreaterThan(0, button.ActualWidth);
		Assert.IsGreaterThan(0, button.ActualHeight);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Secondary_Window_From_Xaml()
	{
		AssertSupportsMultipleWindows();

		var sut = new RedWindow();
		sut.Activate();
		await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);

		var initialScreenshot = await UITestHelper.ScreenShot(sut.Content as FrameworkElement);

		var color = initialScreenshot.GetPixel(initialScreenshot.Width / 2, initialScreenshot.Height / 2);
		color.Should().Be(Colors.Red);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RequestedTheme_Set_Explicitly()
	{
		AssertSupportsMultipleWindows();

		var darkThemeDisposable = ThemeHelper.UseDarkTheme();
		var sut = new Window();
		try
		{
			sut.Content = new Border() { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
			sut.Activate();
			await TestServices.WindowHelper.WaitForLoaded(sut.Content as FrameworkElement);

			Assert.AreEqual(ApplicationTheme.Light, Application.Current.RequestedTheme);
		}
		finally
		{
			// Reset the theme to avoid affecting other tests
			darkThemeDisposable.Dispose();
			sut.Close();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Window_Close_Programmatically_Does_Not_Trigger_AppWindow_Closing()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		var content = new Border() { Width = 100, Height = 100 };
		sut.Content = content;
		bool closingTriggered = false;
		sut.AppWindow.Closing += (s, e) => closingTriggered = true;

		sut.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);

		sut.Close();

		closingTriggered.Should().BeFalse();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Window_Close_Programmatically_Triggers_Window_Closed()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		var content = new Border() { Width = 100, Height = 100 };
		sut.Content = content;
		bool closedTriggered = false;

		sut.Closed += (s, e) => closedTriggered = true;

		sut.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);

		sut.Close();

		closedTriggered.Should().BeTrue();
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaIOS)] // Flaky on iOS Skia https://github.com/unoplatform/uno/issues/9080
	public async Task When_Window_Closed_Is_Handled()
	{
		AssertSupportsMultipleWindows();

		bool contentUnloaded = false;
		var sut = new Window();
		var content = new Border() { Width = 100, Height = 100 };
		content.Unloaded += (s, e) => contentUnloaded = true;
		sut.Content = content;
		bool closedTriggered = false;

		bool shouldHandle = true;
		sut.Closed += (s, e) =>
		{
			e.Handled = shouldHandle;
			closedTriggered = true;
		};

		sut.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);

		sut.Close();

		closedTriggered.Should().BeTrue();

		// The window should still be open
		Assert.IsTrue(sut.Visible);

		closedTriggered = false;
		shouldHandle = false;

		sut.Close();
		closedTriggered.Should().BeTrue();

		// The window should now be closed
		Assert.IsFalse(sut.Visible);

		await TestServices.WindowHelper.WaitFor(() => contentUnloaded);
	}

	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
	public async Task When_Window_Close_Programmatically_Event_Order()
	{
		AssertSupportsMultipleWindows();

		var sut = new Window();
		var content = new Border() { Width = 100, Height = 100 };
		sut.Content = content;
		string eventOrder = "";
		content.Unloaded += (s, e) => eventOrder += "(Unloaded)";
		sut.Closed += (s, e) => eventOrder += "(Window.Closed)";
		sut.AppWindow.Closing += (s, e) => eventOrder += "(AppWindow.Closing)";

		sut.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);

		sut.Close();

		await TestServices.WindowHelper.WaitFor(() => eventOrder.Contains("(Window.Closed)"));
		await TestServices.WindowHelper.WaitFor(() => eventOrder.Contains("(Unloaded)"));

		eventOrder.Should().BeEquivalentTo("(Window.Closed)(Unloaded)");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Window_Closed_Handler_In_Xaml()
	{
		AssertSupportsMultipleWindows();

		var window = new WindowClosed();
		var content = new Border() { Width = 100, Height = 100 };
		window.Content = content;
		window.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);
		window.Close();

		await TestServices.WindowHelper.WaitFor(() => window.ClosedExecuted);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	[RunsOnUIThread]
	public async Task When_FullScreen_After_Activate()
	{
		var window = new Window();
		var content = new Border() { Width = 100, Height = 100 };
		window.Content = content;
		window.Activate();
		await TestServices.WindowHelper.WaitForLoaded(content);
		var bounds1 = window.Bounds;
		window.Close();
		var window2 = new Window();
		window2.Content = content;
		window2.Activate();
		window2.AppWindow.SetPresenter(FullScreenPresenter.Create());
		await TestServices.WindowHelper.WaitForLoaded(content);
		await Task.Delay(TimeSpan.FromMilliseconds(1000));
		try
		{
			var bounds2 = window2.Bounds;
			bounds1.Width.Should().BeLessThan(bounds2.Width);
			bounds1.Height.Should().BeLessThan(bounds2.Height);
		}
		finally
		{
			window2.Close();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Window_Title_In_Xaml()
	{
		AssertSupportsMultipleWindows();

		var window = new WindowClosed();
		var content = new Border() { Width = 100, Height = 100 };
		window.Content = content;
		window.Activate();

		await TestServices.WindowHelper.WaitForLoaded(content);
		Assert.AreEqual("Test title", window.Title);
	}
#endif

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaUIKit)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Scene_Lifecycle_Window_Hosted_In_Application_Scene()
	{
#if HAS_UNO
		// UIKit and the runtime are read through reflection, as the Skia runtime tests are built without the Apple
		// bindings. The iOS SamplesApp declares a scene manifest. The tvOS one has none and sets UnoUseSceneLifecycle,
		// so it runs the scene lifecycle Uno adopts.
		static object GetProperty(object target, string name) => target?.GetType().GetProperty(name)?.GetValue(target);

		var usesSceneLifecycle = Type.GetType("Uno.UI.Runtime.AppleUIKit.AppleUIKitLifecycle, Uno.UI.Runtime.AppleUIKit")
			?.GetProperty("UsesSceneLifecycle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
			?.GetValue(null);

		Assert.IsNotNull(usesSceneLifecycle, "AppleUIKitLifecycle.UsesSceneLifecycle was not found.");

		if (usesSceneLifecycle is false)
		{
			Assert.Inconclusive("The app runs the app lifecycle, where the window's scene has no delegate.");
		}

		var nativeWindow = Window.InitialWindow!.NativeWindow;
		var scene = GetProperty(nativeWindow, "WindowScene");

		Assert.IsNotNull(scene, "The window is not shown in a scene.");

		var sceneDelegate = GetProperty(scene, "Delegate");

		Assert.IsNotNull(sceneDelegate, "The application scene has no delegate.");
		Assert.AreSame(nativeWindow, GetProperty(sceneDelegate, "Window"), "The scene delegate does not host the window.");
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaUIKit)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public async Task When_Url_Opened_Then_Activation_Reported()
	{
#if HAS_UNO
		if (OperatingSystem.IsTvOS())
		{
			Assert.Inconclusive("tvOS doesn't open an app's own URL.");
		}

		// The SamplesApp registers this scheme, so the URL comes back to the app. The iOS SamplesApp declares a scene
		// manifest, so its scene delegate reports it. Without a manifest, the scene delegate Uno adopts forwards it to
		// the application delegate, whose base implementation reports it. The SamplesApp shows no dialog for the
		// runtime-tests host.
		var uri = new Uri($"uno-samples-test://runtime-tests/activation/{Guid.NewGuid():N}");
		var activated = new TaskCompletionSource<bool>();

		void OnActivated(object sender, Microsoft.Windows.AppLifecycle.AppActivationArguments args)
		{
			if (args.Data is global::Windows.ApplicationModel.Activation.ProtocolActivatedEventArgs { Uri: { } activationUri } && activationUri == uri)
			{
				activated.TrySetResult(true);
			}
		}

		var appInstance = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
		appInstance.Activated += OnActivated;

		try
		{
			Assert.IsTrue(await global::Windows.System.Launcher.LaunchUriAsync(uri), "The URL was not opened.");
			Assert.AreSame(activated.Task, await Task.WhenAny(activated.Task, Task.Delay(TimeSpan.FromSeconds(10))), "No activation was reported for the URL.");
		}
		finally
		{
			appInstance.Activated -= OnActivated;
		}
#endif
	}
}
