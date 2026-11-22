#if HAS_UNO
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Sends Back, keys and touches through the Android activity the way the system does: the Back key and back swipe (through the
/// activity's back dispatcher with predictive back, API 36+), and a hardware keyboard.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
[Ignore("Builds and dispatches Android input events through reflection, which NativeAOT trims away.")]
#endif
public class Given_ApplicationActivity_Keys
{
	// Android.Views.Keycode values
	private const int KeycodeBack = 4;
	private const int KeycodeF5 = 135;

	// Android.Views.MotionEventActions values
	private const int MotionEventActionDown = 0;
	private const int MotionEventActionUp = 1;
	private const int MotionEventActionCancel = 3;

	[TestMethod]
	public async Task When_Back_With_Nothing_Open_Then_BackRequested()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(new Button { Content = "Nothing open" });

		using var back = new BackRequestedCounter();
		await SendBack();

		Assert.AreEqual(1, back.Count, "Back with nothing open must reach BackRequested.");
	}

	[TestMethod]
	public async Task When_Back_KeyDown_Handled_Then_No_BackRequested()
	{
		if (IsPredictiveBack)
		{
			Assert.Inconclusive("With predictive back (API 36+), the system takes Back before it becomes a key event.");
		}

		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var button = new Button { Content = "Handles Back" };
		button.KeyDown += (_, e) => e.Handled |= e.Key == Windows.System.VirtualKey.GoBack;
		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		using var back = new BackRequestedCounter();
		await SendBack();

		Assert.AreEqual(0, back.Count, "An app that handles the Back key-down keeps Back from the system.");
	}

	[TestMethod]
	public async Task When_Back_With_ComboBox_Open_Then_Only_DropDown_Closes()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();

		using var back = new BackRequestedCounter();
		await SendBack();

		Assert.IsFalse(comboBox.IsDropDownOpen, "Back must close the drop-down.");
		Assert.AreEqual(1, comboBox.SelectedIndex, "Back must not change the selection.");
		Assert.AreEqual(0, back.Count, "The Back that closed the drop-down must not also reach BackRequested.");
	}

	[TestMethod]
	public async Task When_Back_With_ComboBox_Open_That_Closes_On_SelectionChanged_Then_No_BackRequested()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();

		// As an app that swaps its page content when the selection changes: a selection change closes the drop-down.
		comboBox.SelectionChanged += (_, _) => comboBox.IsDropDownOpen = false;

		using var back = new BackRequestedCounter();
		await SendBack();

		// Checked first: this is what made such an app navigate back.
		Assert.AreEqual(0, back.Count, "The Back that closed the drop-down must not also reach BackRequested.");
		Assert.IsFalse(comboBox.IsDropDownOpen, "Back must close the drop-down.");
		Assert.AreEqual(1, comboBox.SelectedIndex, "Back must not change the selection.");
	}

	[TestMethod]
	public async Task When_Back_Swipe_Touch_Closed_ComboBox_Then_Its_Back_Does_Nothing_More()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		// With gesture navigation, the app gets the swipe's touch-down, then a cancel when the system takes the pointer over.
		var (x, y) = GetBackGestureEdgePoint();
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);
		Assert.IsFalse(comboBox.IsDropDownOpen, "The swipe's touch-down light-dismisses the drop-down.");

		await SendBack();
		Assert.AreEqual(0, back.Count, "The Back that ends the swipe must not also reach BackRequested.");

		await SendBack();
		Assert.AreEqual(1, back.Count, "The next Back must act as usual.");
	}

	[TestMethod]
	public async Task When_Tap_Closed_ComboBox_Then_Back_Raises_BackRequested()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		var (x, y) = GetPointOutsidePopups(1);
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionUp, x, y);
		Assert.IsFalse(comboBox.IsDropDownOpen, "The tap light-dismisses the drop-down.");

		await SendBack();
		Assert.AreEqual(1, back.Count, "A Back after a tap closed the drop-down must act as usual.");
	}

	[TestMethod]
	public async Task When_Back_Swipe_Touch_Closed_Last_Back_Handler_Then_Its_Back_Still_Comes_To_App()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		// No BackRequested subscriber: the drop-down's popup is the only back handler, and the swipe's touch-down closes it.
		var (x, y) = GetBackGestureEdgePoint();
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);
		Assert.IsFalse(comboBox.IsDropDownOpen, "The swipe's touch-down light-dismisses the drop-down.");

		// Checked before sending Back: if Back went to the system, it would finish the test app's activity.
		Assert.IsTrue(IsBackComingToApp(), "The Back that ends the swipe must still come to the app.");

		await SendBackAndReopenIfAppLeft("The Back that ends the swipe must not take the app away.");
	}

	[TestMethod]
	public async Task When_Touch_Outside_Back_Gesture_Area_Canceled_Then_Back_Raises_BackRequested()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		// A cancel can also come from palm rejection or a window taking focus: only a swipe from the edge is a back swipe.
		var (x, y) = GetPointOutsidePopups(0.5);
		Assert.IsFalse(IsInBackGestureArea(x), "The point must be outside the back gesture area.");
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);
		Assert.IsFalse(comboBox.IsDropDownOpen, "The touch-down light-dismisses the drop-down.");

		await SendBack();
		Assert.AreEqual(1, back.Count, "A Back after a canceled touch outside the back gesture area must act as usual.");
	}

	[TestMethod]
	public async Task When_Back_Comes_Long_After_Swipe_Touch_Closed_ComboBox_Then_Back_Raises_BackRequested()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		var (x, y) = GetBackGestureEdgePoint();
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);

		// A Back that comes seconds later (keyboard, accessibility) isn't the swipe's.
		await Task.Delay(2500);
		await SendBack();
		Assert.AreEqual(1, back.Count, "A Back long after the swipe must act as usual.");
	}

	[TestMethod]
	public async Task When_BackRequested_Handler_Unsubscribes_Without_Handling_Then_Activity_Stays()
	{
		if (!IsPredictiveBack)
		{
			Assert.Inconclusive("Below API 36, an unhandled BackRequested has always let the system go back.");
		}

		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(new Button { Content = "Page" });

		// As a page that navigates in its handler, without setting Handled, and unsubscribes when it's navigated from.
		var navigationManager = SystemNavigationManager.GetForCurrentView();
		var raised = 0;
		void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			raised++;
			navigationManager.BackRequested -= OnBackRequested;
		}
		navigationManager.BackRequested += OnBackRequested;

		try
		{
			await SendBackAndReopenIfAppLeft("A BackRequested subscriber took this Back: the app must stay.");
		}
		finally
		{
			navigationManager.BackRequested -= OnBackRequested;
		}

		Assert.AreEqual(1, raised);
	}

	[TestMethod]
	public async Task When_Back_Swipe_That_Closed_ComboBox_Is_Held_Then_Its_Back_Does_Nothing_More()
	{
		if (!IsPredictiveBack)
		{
			Assert.Inconclusive("The system reports the back swipe's progress to the app with predictive back (API 36+) only.");
		}

		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		var (x, y) = GetBackGestureEdgePoint();
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);
		await SendBackStarted(x, y);

		// The user holds the predictive back preview for longer than the time a key-based Back gets.
		await Task.Delay(2500);
		await SendBack();
		Assert.AreEqual(0, back.Count, "The Back that ends a held swipe must not also reach BackRequested.");
	}

	[TestMethod]
	public async Task When_Back_Swipe_That_Closed_ComboBox_Is_Cancelled_Then_Next_Back_Raises_BackRequested()
	{
		if (!IsPredictiveBack)
		{
			Assert.Inconclusive("The system reports the back swipe's progress to the app with predictive back (API 36+) only.");
		}

		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var comboBox = await LoadOpenComboBox();
		using var back = new BackRequestedCounter();
		using var lightDismiss = CloseOnTouchDown(comboBox);

		var (x, y) = GetBackGestureEdgePoint();
		await SendTouch(MotionEventActionDown, x, y);
		await SendTouch(MotionEventActionCancel, x, y);
		await SendBackStarted(x, y);
		await SendBackCancelled();

		await SendBack();
		Assert.AreEqual(1, back.Count, "After a cancelled swipe, Back must act as usual.");
	}

	[TestMethod]
	public async Task When_Back_With_ComboBox_Open_In_ContentDialog_Then_Only_DropDown_Closes()
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(new Button { Content = "Page" });

		var comboBox = CreateComboBox();
		var dialog = new ContentDialog
		{
			Content = comboBox,
			CloseButtonText = "Close",
			XamlRoot = TestServices.WindowHelper.XamlRoot,
		};
		var closed = false;
		dialog.Closed += (_, _) => closed = true;

		using var back = new BackRequestedCounter();
		var showTask = dialog.ShowAsync();
		try
		{
			await TestServices.WindowHelper.WaitForLoaded(comboBox);

			comboBox.IsDropDownOpen = true;
			await TestServices.WindowHelper.WaitFor(() => GetOpenPopups().Count() > 1, message: "the drop-down should open");

			await SendBack();

			Assert.IsFalse(comboBox.IsDropDownOpen, "Back must close the drop-down.");
			Assert.AreEqual(1, comboBox.SelectedIndex, "Back must not change the selection.");
			Assert.IsFalse(closed, "The Back that closed the drop-down must not also close the dialog.");

			await SendBack();

			await TestServices.WindowHelper.WaitFor(() => closed, message: "Back with only the dialog open must close it");
			Assert.AreEqual(0, back.Count, "The dialog handles both Backs.");
		}
		finally
		{
			dialog.Hide();
			await showTask;
		}
	}

	[TestMethod]
	[DataRow("Flyout")]
	[DataRow("MenuFlyout")]
	[DataRow("DatePickerFlyout")]
	[DataRow("TimePickerFlyout")]
	public async Task When_Back_With_Flyout_Open_Then_Only_Flyout_Closes(string kind)
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var target = new Button { Content = "Target" };
		await UITestHelper.Load(target);

		FlyoutBase flyout = kind switch
		{
			"Flyout" => new Flyout { Content = new Button { Content = "Inside" } },
			"MenuFlyout" => new MenuFlyout { Items = { new MenuFlyoutItem { Text = "One" }, new MenuFlyoutItem { Text = "Two" } } },
			"DatePickerFlyout" => new DatePickerFlyout(),
			"TimePickerFlyout" => new TimePickerFlyout(),
			_ => throw new ArgumentOutOfRangeException(nameof(kind)),
		};

		var isOpen = false;
		flyout.Opened += (_, _) => isOpen = true;
		flyout.Closed += (_, _) => isOpen = false;

		try
		{
			flyout.ShowAt(target);
			await TestServices.WindowHelper.WaitFor(() => isOpen, message: $"the {kind} should open");
			await TestServices.WindowHelper.WaitForIdle();

			using var back = new BackRequestedCounter();
			await SendBack();

			Assert.IsFalse(isOpen, $"Back must close the {kind} (BackRequested raised {back.Count} times).");
			Assert.AreEqual(0, back.Count, $"The Back that closed the {kind} must not also reach BackRequested.");
		}
		finally
		{
			flyout.Hide();
		}
	}

	[TestMethod]
	[DataRow(KeycodeBack)]
	[DataRow(KeycodeF5)]
	public async Task When_Key_Types_Nothing_Then_TextBox_Text_Unchanged(int keycode)
	{
		using var resetWindowContent = UITestHelper.ResetWindowContent();
		var textBox = new TextBox { Text = "abc" };
		await UITestHelper.Load(textBox);

		textBox.Focus(FocusState.Programmatic);
		textBox.Select(textBox.Text.Length, 0);
		await TestServices.WindowHelper.WaitForIdle();

		using var back = new BackRequestedCounter();
		await SendKey(keycode);

		Assert.AreEqual("abc", textBox.Text);
	}

	private static ComboBox CreateComboBox()
		=> new ComboBox
		{
			ItemsSource = new[] { "Day", "Week", "Month" },
			SelectedIndex = 1,
		};

	private static System.Collections.Generic.IEnumerable<Popup> GetOpenPopups()
		=> VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

	/// <summary>
	/// Sends a key press and release through the activity, one event per dispatcher pass as the system delivers them.
	/// </summary>
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, "Android.Views.KeyEvent", "Mono.Android")]
	[DynamicDependency("DispatchKeyEvent", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static async Task SendKey(int keycode)
	{
		var activity = GetWindowActivity();
		Assert.IsNotNull(activity);

		var keyEventType = Type.GetType("Android.Views.KeyEvent, Mono.Android");
		var actionsType = Type.GetType("Android.Views.KeyEventActions, Mono.Android");
		var keycodeType = Type.GetType("Android.Views.Keycode, Mono.Android");
		Assert.IsNotNull(keyEventType);
		Assert.IsNotNull(actionsType);
		Assert.IsNotNull(keycodeType);

		var constructor = keyEventType.GetConstructor(new[] { actionsType, keycodeType });
		var dispatch = activity.GetType().GetMethod("DispatchKeyEvent", new[] { keyEventType });
		Assert.IsNotNull(constructor);
		Assert.IsNotNull(dispatch);

		foreach (var action in new[] { 0 /* Down */, 1 /* Up */ })
		{
			var keyEvent = constructor.Invoke(new[] { Enum.ToObject(actionsType, action), Enum.ToObject(keycodeType, keycode) });
			dispatch.Invoke(activity, new[] { keyEvent });
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	// Closes the drop-down when a touch-down reaches the visual tree, as a light-dismiss press outside it does. In the runtime
	// test window, a touch outside the drop-down lands on the test runner's SplitView light-dismiss layer, not on the drop-down's
	// PopupPanel, so the drop-down's own light-dismiss can't be driven from here.
	// When disposed, a tap outside the back gesture area ends any back swipe state, so a failing test can't leave it to the next.
	private static IDisposable CloseOnTouchDown(ComboBox comboBox)
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var root = xamlRoot.VisualTree.RootElement;
		var handler = new PointerEventHandler((_, _) => comboBox.IsDropDownOpen = false);
		root.AddHandler(UIElement.PointerPressedEvent, handler, true);
		return Uno.Disposables.Disposable.Create(() =>
		{
			root.RemoveHandler(UIElement.PointerPressedEvent, handler);

			var x = (float)(xamlRoot.Size.Width * 0.5 * xamlRoot.RasterizationScale);
			var y = (float)(xamlRoot.Size.Height * 0.75 * xamlRoot.RasterizationScale);
			try
			{
				DispatchTouch(MotionEventActionDown, x, y);
				DispatchTouch(MotionEventActionUp, x, y);
			}
			catch (Exception)
			{
				// Best effort: don't hide the test's own failure.
			}
		});
	}

	private static bool IsPredictiveBack => OperatingSystem.IsAndroidVersionAtLeast(36);

	private static async Task<ComboBox> LoadOpenComboBox()
	{
		var comboBox = CreateComboBox();
		await UITestHelper.Load(comboBox);

		// As a tap does, so the keys go to the ComboBox.
		comboBox.Focus(FocusState.Pointer);
		comboBox.IsDropDownOpen = true;
		await TestServices.WindowHelper.WaitFor(() => GetOpenPopups().Any(), message: "the drop-down should open");

		return comboBox;
	}

	/// <summary>
	/// Sends Back the way the system does: with predictive back (API 36+) through the activity's back dispatcher, before
	/// that as a key press.
	/// </summary>
	[DynamicDependency("get_OnBackPressedDispatcher", "AndroidX.Activity.ComponentActivity", "Xamarin.AndroidX.Activity")]
	[DynamicDependency("OnBackPressed", "AndroidX.Activity.OnBackPressedDispatcher", "Xamarin.AndroidX.Activity")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static async Task SendBack()
	{
		if (!IsPredictiveBack)
		{
			await SendKey(KeycodeBack);
			return;
		}

		var dispatcher = GetMember(GetWindowActivity(), "OnBackPressedDispatcher");
		Assert.IsNotNull(dispatcher);
		dispatcher.GetType().GetMethod("OnBackPressed", Type.EmptyTypes)!.Invoke(dispatcher, null);
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Whether a Back now would reach the app rather than make the system go back.
	[DynamicDependency("get_HasEnabledCallbacks", "AndroidX.Activity.OnBackPressedDispatcher", "Xamarin.AndroidX.Activity")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static bool IsBackComingToApp()
	{
		if (!IsPredictiveBack)
		{
			// Before predictive back, Back always reaches ApplicationActivity.OnBackPressed.
			return true;
		}

		// The binding exposes hasEnabledCallbacks() as a property.
		var dispatcher = GetMember(GetWindowActivity(), "OnBackPressedDispatcher");
		return GetMember(dispatcher, "HasEnabledCallbacks") is true;
	}

	/// <summary>
	/// Sends Back, and if the system went back (finishing the activity, or on Android 12+ moving the app to the background),
	/// brings the app to the front again so the following tests still have a window, then fails.
	/// </summary>
	[DynamicDependency("get_Lifecycle", "AndroidX.Activity.ComponentActivity", "Xamarin.AndroidX.Activity")]
	[DynamicDependency("get_CurrentState", "AndroidX.Lifecycle.Lifecycle", "Xamarin.AndroidX.Lifecycle.Common")]
	[DynamicDependency("get_IsFinishing", "Android.App.Activity", "Mono.Android")]
	[DynamicDependency("get_ApplicationContext", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("get_PackageManager", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("get_PackageName", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("StartActivity(Android.Content.Intent)", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("GetLaunchIntentForPackage", "Android.Content.PM.PackageManager", "Mono.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static async Task SendBackAndReopenIfAppLeft(string message)
	{
		var activity = GetWindowActivity();
		await SendBack();

		// The system moves the task back asynchronously. Recovery is best effort: the app may only start an activity while its
		// closing window is still visible, so it relaunches as soon as the activity leaves.
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		while (GetMember(activity, "IsFinishing") is not true && IsResumed(activity))
		{
			if (stopwatch.ElapsedMilliseconds >= 1000)
			{
				return;
			}

			await Task.Delay(50);
		}

		var context = GetMember(activity, "ApplicationContext");
		var packageManager = GetMember(activity, "PackageManager");
		var launchIntent = packageManager.GetType()
			.GetMethod("GetLaunchIntentForPackage", new[] { typeof(string) })!
			.Invoke(packageManager, new[] { GetMember(activity, "PackageName") });
		context.GetType().GetMethod("StartActivity", new[] { launchIntent.GetType() })!.Invoke(context, new[] { launchIntent });
		await TestServices.WindowHelper.WaitFor(() => GetWindowActivity() is { } current && GetMember(current, "IsFinishing") is not true && IsResumed(current), timeoutMS: 15000);

		Assert.Fail(message);

		static bool IsResumed(object activity)
			=> GetMember(GetMember(activity, "Lifecycle"), "CurrentState")?.ToString() == "RESUMED";
	}

	// Reports a predictive back swipe's start, then its cancellation, to the activity's back dispatcher, as the system does.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, "AndroidX.Activity.BackEventCompat", "Xamarin.AndroidX.Activity")]
	[DynamicDependency("DispatchOnBackStarted", "AndroidX.Activity.OnBackPressedDispatcher", "Xamarin.AndroidX.Activity")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static async Task SendBackStarted(float x, float y)
	{
		var backEventType = Type.GetType("AndroidX.Activity.BackEventCompat, Xamarin.AndroidX.Activity");
		Assert.IsNotNull(backEventType);
		var backEvent = backEventType.GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(int) })!
			.Invoke(new object[] { x, y, 0f, 1 /* BackEventCompat.EdgeRight */ });

		var dispatcher = GetMember(GetWindowActivity(), "OnBackPressedDispatcher");
		dispatcher.GetType().GetMethod("DispatchOnBackStarted", new[] { backEventType })!.Invoke(dispatcher, new[] { backEvent });
		await TestServices.WindowHelper.WaitForIdle();
	}

	[DynamicDependency("DispatchOnBackCancelled", "AndroidX.Activity.OnBackPressedDispatcher", "Xamarin.AndroidX.Activity")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static async Task SendBackCancelled()
	{
		var dispatcher = GetMember(GetWindowActivity(), "OnBackPressedDispatcher");
		dispatcher.GetType().GetMethod("DispatchOnBackCancelled", Type.EmptyTypes)!.Invoke(dispatcher, null);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[DynamicDependency("IsInBackGestureArea", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static bool IsInBackGestureArea(float x)
	{
		var activity = GetWindowActivity();
		return GetMethod(activity, "IsInBackGestureArea")!.Invoke(activity, new object[] { x }) is true;
	}

	// A point on the window's right edge, where a back swipe starts, outside the open popups, in window pixels.
	private static (float X, float Y) GetBackGestureEdgePoint()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var x = (float)(xamlRoot.Size.Width * xamlRoot.RasterizationScale) - 1;
		if (!IsInBackGestureArea(x))
		{
			Assert.Inconclusive("There's no back gesture area: 3-button navigation, or Android older than 10.");
		}

		return GetPointOutsidePopups(1);
	}

	// A point at the given fraction of the window's width, outside the open popups, in window pixels.
	private static (float X, float Y) GetPointOutsidePopups(double widthFraction)
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;
		var point = new Windows.Foundation.Point(Math.Min(xamlRoot.Size.Width * widthFraction, xamlRoot.Size.Width - 1 / xamlRoot.RasterizationScale), xamlRoot.Size.Height * 0.75);
		foreach (var popup in GetOpenPopups())
		{
			var child = (FrameworkElement)popup.Child;
			var bounds = child.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, child.ActualWidth, child.ActualHeight));
			Assert.IsFalse(bounds.Contains(point), $"The point {point} must be outside the popup ({bounds}).");
		}

		return ((float)(point.X * xamlRoot.RasterizationScale), (float)(point.Y * xamlRoot.RasterizationScale));
	}

	// The window's activity is the app's subclass of ApplicationActivity, so private members are looked up on the base types too.
	private static object GetMember(object instance, string name)
	{
		for (var type = instance?.GetType(); type is not null; type = type.BaseType)
		{
			if (type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } property)
			{
				return property.GetValue(instance);
			}
		}

		return null;
	}

	private static MethodInfo GetMethod(object instance, string name)
	{
		for (var type = instance.GetType(); type is not null; type = type.BaseType)
		{
			if (type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } method)
			{
				return method;
			}
		}

		return null;
	}

	/// <summary>
	/// Sends a single-finger touch event through the activity.
	/// </summary>
	private static async Task SendTouch(int action, float x, float y)
	{
		DispatchTouch(action, x, y);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[DynamicDependency("Obtain", "Android.Views.MotionEvent", "Mono.Android")]
	[DynamicDependency("Recycle", "Android.Views.MotionEvent", "Mono.Android")]
	[DynamicDependency("DispatchTouchEvent", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform these tests run on.")]
	private static void DispatchTouch(int action, float x, float y)
	{
		var activity = GetWindowActivity();
		Assert.IsNotNull(activity);

		var motionEventType = Type.GetType("Android.Views.MotionEvent, Mono.Android");
		Assert.IsNotNull(motionEventType);

		// MotionEvent.Obtain(long downTime, long eventTime, MotionEventActions action, float x, float y, MetaKeyStates metaState)
		var obtain = motionEventType.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m =>
			m.Name == "Obtain"
			&& m.GetParameters() is { Length: 6 } p
			&& p[0].ParameterType == typeof(long)
			&& p[2].ParameterType.Name == "MotionEventActions"
			&& p[3].ParameterType == typeof(float));
		var dispatch = activity.GetType().GetMethod("DispatchTouchEvent", new[] { motionEventType });
		Assert.IsNotNull(obtain);
		Assert.IsNotNull(dispatch);

		var parameters = obtain.GetParameters();
		var now = Environment.TickCount64;
		var motionEvent = obtain.Invoke(null, new[]
		{
			(object)now,
			now,
			Enum.ToObject(parameters[2].ParameterType, action),
			x,
			y,
			Enum.ToObject(parameters[5].ParameterType, 0),
		});
		dispatch.Invoke(activity, new[] { motionEvent });
		motionEventType.GetMethod("Recycle", Type.EmptyTypes)!.Invoke(motionEvent, null);
	}

	// The runtime tests don't reference Mono.Android or the Android host, hence the reflection.
	private static object GetWindowActivity()
	{
		var host = TestServices.WindowHelper.XamlRoot is { } xamlRoot
			? typeof(XamlRoot).Assembly.GetType("Uno.UI.Hosting.XamlRootMap")
				?.GetMethod("GetHostForRoot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
				?.Invoke(null, new object[] { xamlRoot })
			: null;

		return host?.GetType()
			.GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?.GetValue(host);
	}

	/// <summary>
	/// Counts BackRequested and handles it, so an unhandled Back doesn't finish the test app's activity.
	/// </summary>
	private sealed class BackRequestedCounter : IDisposable
	{
		public BackRequestedCounter() => SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

		public int Count { get; private set; }

		public void Dispose() => SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;

		private void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			Count++;
			e.Handled = true;
		}
	}
}
#endif
