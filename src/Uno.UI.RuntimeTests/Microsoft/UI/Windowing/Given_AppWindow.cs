#if HAS_UNO_WINUI || WINDOWS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Combinatorial.MSTest;
using AwesomeAssertions;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.Graphics;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

[TestClass]
[RunsOnUIThread]
#if !__SKIA__ && !WINDOWS
[Ignore]
#endif
public class Given_AppWindow
{
	[TestMethod]
	public async Task When_Resize()
	{
		if (OperatingSystem.IsLinux())
		{
			// Currently failing in CI #9080
			Assert.Inconclusive();
		}

		AssertSizingSupport();

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalSize = appWindow.Size;
		try
		{
			// Smaller, as a window cannot grow taller than the screen.
			var adjustedSize = new SizeInt32() { Width = originalSize.Width - 10, Height = originalSize.Height - 10 };
			appWindow.Resize(adjustedSize);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidSizeChange);
			Assert.AreEqual(appWindow.Size, adjustedSize,
				$"Expected appWindow.Size {{ Width={appWindow.Size.Width}, Height={appWindow.Size.Height} }} == adjustedSize {{ Width={adjustedSize.Width}, Height={adjustedSize.Height} }}");
		}
		finally
		{
			appWindow.Resize(originalSize);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Size.Equals(originalSize));
		}
	}

	[TestMethod]
	public async Task When_Move()
	{
		AssertPositioningSupport();

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalPosition = appWindow.Position;
		try
		{
			var adjustedPosition = new PointInt32() { X = originalPosition.X + 10, Y = originalPosition.Y + 10 };
			appWindow.Move(adjustedPosition);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidPositionChange);
			Assert.AreEqual(appWindow.Position, adjustedPosition,
				$"Expected appWindow.Position {{ X={appWindow.Position.X}, Y={appWindow.Position.Y} }} == adjustedPosition {{ X={adjustedPosition.X}, Y={adjustedPosition.Y} }}");
		}
		finally
		{
			appWindow.Move(originalPosition);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Position.Equals(originalPosition));
		}
	}

#if __UNO__
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25650")]
	public async Task When_Move_Down_Then_Window_Moves_Down_On_Screen()
	{
		AssertPositioningSupport();

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var appWindow = window.AppWindow;
		var originalPosition = appWindow.Position;
		// Asks the platform where the window content is, so it does not just read back the value Move stored.
		var before = window.NativeWrapper!.ConvertLocalToScreen(default);
		try
		{
			// Downward, as the top of the screen can stop a window from moving up.
			appWindow.Move(new PointInt32(originalPosition.X, originalPosition.Y + 20));
			await TestServices.WindowHelper.WaitFor(() => appWindow.Position.Y != originalPosition.Y);

			var after = window.NativeWrapper!.ConvertLocalToScreen(default);
			Assert.AreEqual(before.Y + 20, after.Y, $"Window content moved from ({before.X}, {before.Y}) to ({after.X}, {after.Y}) on screen");
			Assert.AreEqual(before.X, after.X, $"Window content moved from ({before.X}, {before.Y}) to ({after.X}, {after.Y}) on screen");
		}
		finally
		{
			appWindow.Move(originalPosition);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Position.Equals(originalPosition));
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25650")]
	public async Task When_Resize_Then_Top_Left_Stays()
	{
		if (OperatingSystem.IsLinux())
		{
			// Resizing is currently failing in CI #9080
			Assert.Inconclusive();
		}

		AssertPositioningSupport();

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var appWindow = window.AppWindow;
		var originalSize = appWindow.Size;
		var originalPosition = appWindow.Position;
		var before = window.NativeWrapper!.ConvertLocalToScreen(default);
		try
		{
			// Smaller, as a window cannot grow taller than the screen.
			appWindow.Resize(new SizeInt32(originalSize.Width - 20, originalSize.Height - 20));
			await TestServices.WindowHelper.WaitFor(() => !appWindow.Size.Equals(originalSize));

			Assert.AreEqual(originalPosition, appWindow.Position, $"Position changed to ({appWindow.Position.X}, {appWindow.Position.Y})");
			var after = window.NativeWrapper!.ConvertLocalToScreen(default);
			Assert.AreEqual(before, after, $"Window content moved from ({before.X}, {before.Y}) to ({after.X}, {after.Y}) on screen");
		}
		finally
		{
			appWindow.Resize(originalSize);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Size.Equals(originalSize));
		}
	}
#endif

	[TestMethod]
	public async Task When_Resize_Before_Activate()
	{
		AssertSizingSupport();

		var newWindow = new Window { Content = new Grid() };
		var appWindow = newWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalSize = appWindow.Size;
		try
		{
			// Small enough for any screen: macOS fits a window to the screen's usable area when it is first shown.
			var adjustedSize = new SizeInt32() { Width = 600, Height = 400 };
			appWindow.Resize(adjustedSize);
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() => args is not null));
			Assert.IsTrue(args.DidSizeChange);
			Assert.AreEqual(appWindow.Size, adjustedSize,
				$"Expected appWindow.Size {{ Width={appWindow.Size.Width}, Height={appWindow.Size.Height} }} == adjustedSize {{ Width={adjustedSize.Width}, Height={adjustedSize.Height} }}");

			await ActivateAndWaitForBounds(newWindow, () =>
			{
				Assert.AreEqual(adjustedSize, appWindow.Size,
					$"Expected appWindow.Size {{ Width={appWindow.Size.Width}, Height={appWindow.Size.Height} }} == adjustedSize {{ Width={adjustedSize.Width}, Height={adjustedSize.Height} }}");
				Assert.AreEqual(OverlappedPresenterState.Restored, ((OverlappedPresenter)appWindow.Presenter).State);
			});
		}
		finally
		{
			appWindow.Changed -= OnChanged;
			newWindow.Close();
		}
	}

	[TestMethod]
	public async Task When_Move_Before_Activate()
	{
		AssertPositioningSupport();

		var newWindow = new Window();
		var appWindow = newWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalPosition = appWindow.Position;
		var activated = false;
		try
		{
			var adjustedPosition = new PointInt32() { X = 40, Y = 40 };
			appWindow.Move(adjustedPosition);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidPositionChange);
			Assert.AreEqual(appWindow.Position, adjustedPosition,
				$"Expected appWindow.Position {{ X={appWindow.Position.X}, Y={appWindow.Position.Y} }} == adjustedPosition {{ X={adjustedPosition.X}, Y={adjustedPosition.Y} }}");

			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			Assert.AreEqual(appWindow.Position, adjustedPosition,
				$"Expected appWindow.Position {{ X={appWindow.Position.X}, Y={appWindow.Position.Y} }} == adjustedPosition {{ X={adjustedPosition.X}, Y={adjustedPosition.Y} }}");
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	[TestMethod]
	[CombinatorialData]
	public async Task When_Resize_Twice_Before_Activate(bool extendContentIntoTitleBar)
	{
		AssertSizingSupport();

		var newWindow = new Window
		{
			Content = new Grid(),
			ExtendsContentIntoTitleBar = extendContentIntoTitleBar
		};
		var appWindow = newWindow.AppWindow;
		var resized = false;
		var requestedSize = new SizeInt32(720, 560);
		void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
		{
			resized |= args.DidSizeChange && sender.Size.Equals(requestedSize);
		}
		appWindow.Changed += OnChanged;
		try
		{
			appWindow.Resize(new SizeInt32(640, 480));
			appWindow.Resize(requestedSize);
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() => resized));
			Assert.AreEqual(requestedSize, appWindow.Size);
			Assert.IsFalse(appWindow.IsVisible);

			await ActivateAndWaitForBounds(newWindow, () =>
			{
				Assert.AreEqual(requestedSize, appWindow.Size);
				Assert.AreEqual(OverlappedPresenterState.Restored, ((OverlappedPresenter)appWindow.Presenter).State);
			});
		}
		finally
		{
			appWindow.Changed -= OnChanged;
			newWindow.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_Resize_Before_Activate_Then_Maximize()
	{
		AssertSizingSupport();

		var newWindow = new Window { Content = new Grid() };
		var appWindow = newWindow.AppWindow;
		var requestedSize = new SizeInt32(320, 240);
		try
		{
			appWindow.Resize(requestedSize);
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() => appWindow.Size.Equals(requestedSize)));
			var presenter = (OverlappedPresenter)appWindow.Presenter;
			presenter.Maximize();
			await ActivateAndWaitForBounds(newWindow, () => { });
			// CodeActivated can precede the UI application of the compositor's maximize configure.
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(
				() => presenter.State == OverlappedPresenterState.Maximized,
				timeoutMS: 5000, message: "Compositor maximize configure applied to the presenter"));
			Assert.AreEqual(OverlappedPresenterState.Maximized, presenter.State);
			// A nonzero maximized configure overrides the floating size requested before mapping.
			Assert.AreNotEqual(requestedSize, appWindow.Size);
		}
		finally
		{
			newWindow.Close();
		}
	}

	[TestMethod]
	// Wayland: the compositor decides the mapped size; When_Resize_Before_Activate_Then_Maximize covers it.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_First_Activation_Observes_Applied_Bounds()
	{
		AssertSizingSupport();

		var newWindow = new Window { Content = new Grid() };
		var appWindow = newWindow.AppWindow;
		var requestedSize = new SizeInt32(320, 240);
		try
		{
			appWindow.Resize(requestedSize);
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() => appWindow.Size.Equals(requestedSize)));
			var presenter = (OverlappedPresenter)appWindow.Presenter;
			await ActivateAndWaitForBounds(newWindow, () =>
			{
				Assert.AreEqual(OverlappedPresenterState.Restored, presenter.State);
				Assert.AreEqual(requestedSize, appWindow.Size);
			});
		}
		finally
		{
			newWindow.Close();
		}
	}

	[TestMethod]
	public async Task When_First_Activation_Is_CodeActivated()
	{
		AssertSizingSupport();

		var newWindow = new Window { Content = new Grid() };
		try
		{
			var states = await ActivateAndWaitForBounds(newWindow, () => { });
			Assert.AreEqual(WindowActivationState.CodeActivated, states[0], "Managed activation must precede focus-driven activation.");
			if (states.Contains(WindowActivationState.PointerActivated))
			{
				Assert.AreEqual(WindowActivationState.PointerActivated, states[1], "A focused window must raise PointerActivated after CodeActivated.");
			}
		}
		finally
		{
			newWindow.Close();
		}
	}

	[TestMethod]
	[CombinatorialData]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21435")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.NativeWinUI)] // Skia/X11: Fail in CI (#21194)
	public async Task When_Change_Window_State_Before_Activate(OverlappedPresenterState state)
	{
		AssertSizingSupport();
		var newWindow = new Window();
		var appWindow = newWindow.AppWindow;
		var activated = false;
		var stateAtActivation = OverlappedPresenterState.Restored;
		try
		{
			var overlappedPresenter = (OverlappedPresenter)appWindow.Presenter;
			Action act = state switch
			{
				OverlappedPresenterState.Maximized => () => overlappedPresenter.Maximize(),
				OverlappedPresenterState.Minimized => () => overlappedPresenter.Minimize(),
				_ => () => overlappedPresenter.Restore()
			};
			act.Should().NotThrow();
			newWindow.Activated += (s, e) =>
			{
				if (!activated && e.WindowActivationState != WindowActivationState.Deactivated)
				{
					stateAtActivation = overlappedPresenter.State;
					activated = true;
				}
			};
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated, timeoutMS: 5000);
			Assert.AreEqual(state, stateAtActivation);
			Assert.AreEqual(state, overlappedPresenter.State);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25726")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Window_Already_Has_Its_Maximized_Frame_Then_Maximize_Reports_Maximized()
	{
		// A window that fills a small screen is already the size maximizing gives it, as on CI's macOS VMs.
		AssertPositioningSupport();

		var newWindow = new Window();
		var appWindow = newWindow.AppWindow;
		var overlapped = (OverlappedPresenter)appWindow.Presenter;
		var activated = false;
		try
		{
			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			overlapped.Maximize();
			await TestServices.WindowHelper.WaitFor(() => overlapped.State == OverlappedPresenterState.Maximized);
			var maximizedPosition = appWindow.Position;
			var maximizedSize = appWindow.Size;

			overlapped.Restore();
			await TestServices.WindowHelper.WaitFor(() => overlapped.State == OverlappedPresenterState.Restored);
			appWindow.Move(maximizedPosition);
			appWindow.Resize(maximizedSize);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Position.Equals(maximizedPosition) && appWindow.Size.Equals(maximizedSize));

			overlapped.Maximize();
			await TestServices.WindowHelper.WaitFor(() => overlapped.State == OverlappedPresenterState.Maximized);

			overlapped.Restore();
			await TestServices.WindowHelper.WaitFor(() => overlapped.State == OverlappedPresenterState.Restored);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21745")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Maximized_With_Extended_TitleBar_Client_Is_Inset_From_Window()
	{
		AssertSizingSupport();

		var newWindow = new Window();
		newWindow.ExtendsContentIntoTitleBar = true;
		var appWindow = newWindow.AppWindow;
		var overlapped = (OverlappedPresenter)appWindow.Presenter;
		var activated = false;
		try
		{
			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			overlapped.Maximize();
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => overlapped.State == OverlappedPresenterState.Maximized);

			var size = appWindow.Size;             // outer window rect: overhangs the monitor when maximized
			var clientSize = appWindow.ClientSize; // client rect: must be inset back onto the visible work area

			// The maximized window rect overhangs the monitor by the resize frame on every edge. With the native
			// overhang correction the client is inset back inside the window, so it must be strictly smaller than
			// the window on both axes. Before the fix the client equalled the window (no inset), i.e. the chrome
			// bled off-screen and these would have been equal.
			clientSize.Width.Should().BeLessThan(size.Width);
			clientSize.Height.Should().BeLessThan(size.Height);

			// The inset equals the resize-frame thickness per side (a handful of DPI-scaled px), never the whole
			// window - guard against a degenerate or grossly over-large inset.
			(size.Width - clientSize.Width).Should().BeInRange(1, 120);
			(size.Height - clientSize.Height).Should().BeInRange(1, 120);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Activated_ClientSize_Is_Physical_Content_Size()
	{
		AssertPositioningSupport();

		var content = new Microsoft.UI.Xaml.Controls.Grid();
		var newWindow = new Window { Content = content };
		var activated = false;
		try
		{
			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated && content.ActualWidth > 0);

			// Without any resize or move: macOS used to report 0 until the next one.
			var xamlRoot = content.XamlRoot;
			var clientSize = newWindow.AppWindow.ClientSize;
			clientSize.Width.Should().BeCloseTo((int)(xamlRoot.Size.Width * xamlRoot.RasterizationScale), 2);
			clientSize.Height.Should().BeCloseTo((int)(xamlRoot.Size.Height * xamlRoot.RasterizationScale), 2);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			newWindow.Close();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25326")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Closed_Then_Presenter_Calls_Do_Nothing()
	{
		var newWindow = new Window();
		var overlapped = (OverlappedPresenter)newWindow.AppWindow.Presenter;
		var activated = false;
		try
		{
			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
		}
		finally
		{
			newWindow.Close();
		}

		// The NSWindow is freed once the close has completed; these calls used to reach it.
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(200);

		overlapped.Maximize();
		overlapped.Minimize();
		overlapped.Restore();
		overlapped.IsAlwaysOnTop = true;
		overlapped.IsResizable = false;
		overlapped.SetBorderAndTitleBar(false, false);
		overlapped.PreferredMinimumWidth = 100;

		Assert.AreEqual(OverlappedPresenterState.Restored, overlapped.State);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_Closed_During_Initial_Activation_No_Activation_Follows_Closed()
	{
		var window = new Window { Content = new Grid() };
		var events = new List<string>();
		var closed = false;
		window.Closed += (_, _) =>
		{
			closed = true;
			events.Add("Closed");
		};
		window.Activated += (_, args) =>
		{
			events.Add(args.WindowActivationState.ToString());
			if (args.WindowActivationState == WindowActivationState.CodeActivated)
			{
				window.Close();
			}
		};
		try
		{
			window.Activate();
			await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() => closed));
			await TestServices.WindowHelper.WaitForIdle();
			CollectionAssert.AreEqual(new[] { "CodeActivated", "Closed" }, events);
		}
		finally
		{
			if (!closed)
			{
				window.Close();
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWayland)]
	public async Task When_Scale_And_Presenter_Change_Together()
	{
#if __UNO__
		var content = new Grid();
		var window = new Window { Content = content };
		try
		{
			await ActivateAndWaitForBounds(window, () => { });
			var root = content.XamlRoot;
			var scale = (float)(root.RasterizationScale + 0.5);
			var wrapper = window.NativeWrapper;
			var wrapperType = wrapper.GetType();
			var applyConfigure = wrapperType.GetMethod("ApplyConfigurationUI", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(applyConfigure);
			var statesField = wrapperType.GetField("_uiStates", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(statesField);
			var maximized = Convert.ToUInt32(Enum.Parse(statesField.FieldType, "Maximized"));
			var states = Enum.ToObject(statesField.FieldType, Convert.ToUInt32(statesField.GetValue(wrapper)) ^ maximized);
			var expectedState = (Convert.ToUInt32(states) & maximized) != 0
				? OverlappedPresenterState.Maximized : OverlappedPresenterState.Restored;
			var configurationType = applyConfigure.GetParameters()[0].ParameterType;
			var layoutType = configurationType.GetProperty("Layout").PropertyType;
			var buttonsType = layoutType.GetProperty("Buttons").PropertyType;
			var layout = Activator.CreateInstance(layoutType, 400, 300, 0, 0, Enum.ToObject(buttonsType, 0));
			var generationField = wrapperType.GetField("_uiStateGeneration", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(generationField);
			var generation = (long)generationField.GetValue(wrapper) + 1;
			var configuration = Activator.CreateInstance(configurationType, new object[]
			{
				generation, layout, (double)scale, 0u, 1, states, string.Empty, false, null, 0L, true
			});
			var presenterChanges = 0;
			var sizeNotifications = new List<string>();
			var observedScale = 0.0;
			var observedBounds = default(Windows.Foundation.Rect);
			var observedClientSize = default(SizeInt32);
			window.AppWindow.Changed += (_, args) =>
			{
				if (args.DidPresenterChange)
				{
					presenterChanges++;
					observedScale = root.RasterizationScale;
					observedBounds = window.Bounds;
					observedClientSize = window.AppWindow.ClientSize;
				}
				if (args.DidSizeChange)
				{
					Assert.AreEqual(expectedState, ((OverlappedPresenter)window.AppWindow.Presenter).State);
					Assert.AreEqual(1, presenterChanges, "Presenter notification must precede size notifications.");
					sizeNotifications.Add("AppWindowSize");
				}
			};
			window.SizeChanged += (_, _) =>
			{
				Assert.AreEqual(expectedState, ((OverlappedPresenter)window.AppWindow.Presenter).State);
				Assert.AreEqual(1, presenterChanges, "Presenter notification must precede size notifications.");
				sizeNotifications.Add("WindowSize");
			};

			// Inject one combined configure without relying on compositor-specific output scale controls.
			// No serial means the synthetic snapshot cannot submit a compositor buffer.
			applyConfigure.Invoke(wrapper, new[] { configuration });

			Assert.AreEqual(1, presenterChanges);
			CollectionAssert.AreEqual(new[] { "AppWindowSize", "WindowSize" }, sizeNotifications);
			Assert.AreEqual((double)scale, observedScale, "Presenter observers must see the configured XamlRoot scale.");
			Assert.AreEqual(new Windows.Foundation.Rect(0, 0, 400, 300), observedBounds);
			Assert.IsTrue(Math.Abs(observedBounds.Width * observedScale - observedClientSize.Width) <= 1);
			Assert.IsTrue(Math.Abs(observedBounds.Height * observedScale - observedClientSize.Height) <= 1);

			var applyState = wrapperType.GetMethod("ApplyConfigurationStateUI", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(applyState);
			applyState.Invoke(wrapper, new[] { configuration });
			Assert.AreEqual(1, presenterChanges, "The independent state callback must not notify the same presenter change twice.");
		}
		finally
		{
			window.Close();
		}
#endif
	}

	/// <summary>Records the initial activation sequence, including focus callbacks queued while content loads.</summary>
	private static async Task<List<WindowActivationState>> ActivateAndWaitForBounds(Window window, Action assertBounds)
	{
		var appWindow = window.AppWindow;
		var activation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var states = new List<WindowActivationState>();
		var isWayland = RuntimeTestsPlatformHelper.CurrentPlatform == RuntimeTestPlatforms.SkiaWayland;
		void OnActivated(object sender, WindowActivatedEventArgs args)
		{
			states.Add(args.WindowActivationState);
			if (activation.Task.IsCompleted || args.WindowActivationState == WindowActivationState.Deactivated)
			{
				return;
			}
			try
			{
				// Every host raises the managed CodeActivated before any focus-driven PointerActivated.
				Assert.AreEqual(WindowActivationState.CodeActivated, states[0]);
				Assert.IsTrue(appWindow.ClientSize.Width > 0 && appWindow.ClientSize.Height > 0);
				Assert.IsTrue(appWindow.ClientSize.Width <= appWindow.Size.Width && appWindow.ClientSize.Height <= appWindow.Size.Height);
				assertBounds();
				activation.TrySetResult(true);
			}
			catch (Exception error)
			{
				activation.TrySetException(error);
			}
		}
		window.Activated += OnActivated;
		try
		{
			window.Activate();
			await WaitWithTimeout(activation.Task);
			var content = (FrameworkElement)window.Content;
			await WaitWithTimeout(TestServices.WindowHelper.WaitForLoaded(content, timeoutMS: 5000));
			if (isWayland)
			{
				await WaitWithTimeout(TestServices.WindowHelper.WaitFor(() =>
					content.XamlRoot is { } root &&
					Math.Abs(root.Size.Width * root.RasterizationScale - appWindow.ClientSize.Width) <= 1 &&
					Math.Abs(root.Size.Height * root.RasterizationScale - appWindow.ClientSize.Height) <= 1,
					timeoutMS: 5000, message: "XamlRoot bounds matching AppWindow.ClientSize"));
			}
			await TestServices.WindowHelper.WaitForIdle();
		}
		finally
		{
			window.Activated -= OnActivated;
		}
		return states;
	}

	private static async Task WaitWithTimeout(Task task)
	{
		Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))), "Timed out waiting for window state.");
		await task;
	}

	private void AssertPositioningSupport()
	{
		AssertSizingSupport();
		if (RuntimeTestsPlatformHelper.CurrentPlatform == RuntimeTestPlatforms.SkiaWayland)
		{
			Assert.Inconclusive("xdg-shell does not support positioning toplevel windows.");
		}
	}

	private void AssertSizingSupport()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows() &&
			!OperatingSystem.IsMacOS() ||
			TestServices.WindowHelper.IsXamlIsland ||
			IsGtk() ||
			RuntimeTestsPlatformHelper.CurrentPlatform == RuntimeTestPlatforms.SkiaFrameBuffer)
		{
			Assert.Inconclusive("This test only supported on Windows, macOS and Linux apps currently.");
		}
	}

	private bool IsGtk()
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly a in assemblies)
		{
			if (a.GetName().Name == "GtkSharp")
			{
				return true;
			}
		}

		return false;
	}
}
#endif
