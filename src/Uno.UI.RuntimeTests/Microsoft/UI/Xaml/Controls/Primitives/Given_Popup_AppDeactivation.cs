#if HAS_UNO
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Helpers;
using NativeWindowWrapperBase = Uno.UI.Xaml.Controls.NativeWindowWrapperBase;

using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives;

// On Android the app is paused through the activity, which is what a system picker or the Home button does.
// Elsewhere the window is deactivated, the event the pause and the iOS resign-active are reported as.
[TestClass]
[RunsOnUIThread]
public class Given_Popup_AppDeactivation
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)] // An island has no window to report a state for.
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPause", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnResume", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPostResume", "AndroidX.AppCompat.App.AppCompatActivity", "Xamarin.AndroidX.AppCompat")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "The Android host only exists on Android, the only platform it is looked up on.")]
	public async Task When_Paused_And_Flyout_Closing_Canceled_Then_Flyout_Stays_Open()
	{
		using var activation = ActivateWindow();
		var (_, flyout) = await ShowFlyout();
		var closingCount = 0;
		void OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
		{
			closingCount++;
			args.Cancel = true;
		}

		flyout.Closing += OnClosing;
		try
		{
			await PauseAndResume(() =>
			{
				Assert.IsTrue(flyout.IsOpen, "The flyout cancelled its Closing, it must stay open while the app is paused.");
				Assert.IsTrue(closingCount > 0, "The flyout must be offered the close through its Closing event.");
			});

			Assert.IsTrue(flyout.IsOpen, "The flyout must still be open after the app resumed.");
		}
		finally
		{
			flyout.Closing -= OnClosing;
			flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)] // An island has no window to report a state for.
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPause", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnResume", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPostResume", "AndroidX.AppCompat.App.AppCompatActivity", "Xamarin.AndroidX.AppCompat")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "The Android host only exists on Android, the only platform it is looked up on.")]
	public async Task When_Paused_And_PreventLightDismissOnWindowDeactivated_Then_Popups_Stay_Open()
	{
		using var activation = ActivateWindow();
		var original = FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated;
		var (owner, flyout) = await ShowFlyout();
		var popup = new Popup
		{
			IsLightDismissEnabled = true,
			Child = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) },
			XamlRoot = owner.XamlRoot,
		};

		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForIdle();
			FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated = true;

			await PauseAndResume(() =>
			{
				Assert.IsTrue(flyout.IsOpen, "The flyout must stay open while the app is paused.");
				Assert.IsTrue(popup.IsOpen, "The light dismiss popup must stay open while the app is paused.");
			});

			Assert.IsTrue(flyout.IsOpen, "The flyout must still be open after the app resumed.");
			Assert.IsTrue(popup.IsOpen, "The light dismiss popup must still be open after the app resumed.");
		}
		finally
		{
			FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated = original;
			popup.IsOpen = false;
			flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)] // An island has no window to report a state for.
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPause", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnResume", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPostResume", "AndroidX.AppCompat.App.AppCompatActivity", "Xamarin.AndroidX.AppCompat")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "The Android host only exists on Android, the only platform it is looked up on.")]
	public async Task When_Paused_Then_ComboBox_Closes()
	{
		using var activation = ActivateWindow();
		var comboBox = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" } };
		await UITestHelper.Load(comboBox);

		try
		{
			comboBox.IsDropDownOpen = true;
			await TestServices.WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(comboBox.XamlRoot).Count > 0);

			await PauseAndResume(() =>
				Assert.IsFalse(comboBox.IsDropDownOpen, "The drop down must close when the app is paused."));
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)] // An island has no window to report a state for.
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPause", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnResume", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("OnPostResume", "AndroidX.AppCompat.App.AppCompatActivity", "Xamarin.AndroidX.AppCompat")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "The Android host only exists on Android, the only platform it is looked up on.")]
	public async Task When_Paused_Then_MenuFlyout_And_Flyout_Close()
	{
		using var activation = ActivateWindow();
		var menuOwner = new Button { Content = "Menu" };
		var flyoutOwner = new Button { Content = "Flyout" };
		await UITestHelper.Load(new StackPanel { Children = { menuOwner, flyoutOwner } });

		var menuFlyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Item" } } };
		var flyout = new Flyout { Content = new Button { Content = "Inner" } };

		try
		{
			menuFlyout.ShowAt(menuOwner);
			flyout.ShowAt(flyoutOwner);
			await TestServices.WindowHelper.WaitFor(() => menuFlyout.IsOpen && flyout.IsOpen);

			await PauseAndResume(() =>
			{
				Assert.IsFalse(menuFlyout.IsOpen, "The menu flyout must close when the app is paused.");
				Assert.IsFalse(flyout.IsOpen, "The flyout must close when the app is paused.");
			});
		}
		finally
		{
			menuFlyout.Hide();
			flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static async Task<(Button owner, Flyout flyout)> ShowFlyout()
	{
		var owner = new Button { Content = "Owner" };
		await UITestHelper.Load(owner);

		var flyout = new Flyout { Content = new Button { Content = "Inner" } };
		flyout.ShowAt(owner);
		await TestServices.WindowHelper.WaitFor(() => flyout.IsOpen);
		await TestServices.WindowHelper.WaitForIdle();

		return (owner, flyout);
	}

	private static async Task PauseAndResume(Action whilePaused)
	{
		if (OperatingSystem.IsAndroid())
		{
			// The runtime tests don't reference Mono.Android or the Android host, hence the reflection.
			var activity = GetWindowActivity();
			Assert.IsNotNull(activity);

			InvokeLifecycleMethod(activity, "OnPause");
			try
			{
				await TestServices.WindowHelper.WaitForIdle();
				whilePaused();
			}
			finally
			{
				// FragmentActivity moves its lifecycle back to RESUMED in onPostResume, not in onResume.
				InvokeLifecycleMethod(activity, "OnResume");
				InvokeLifecycleMethod(activity, "OnPostResume");
			}
		}
		else
		{
			var nativeWindow = GetNativeWindow();
			nativeWindow.ActivationState = CoreWindowActivationState.Deactivated;
			try
			{
				await TestServices.WindowHelper.WaitForIdle();
				whilePaused();
			}
			finally
			{
				nativeWindow.ActivationState = CoreWindowActivationState.CodeActivated;
			}
		}

		await TestServices.WindowHelper.WaitForIdle();
	}

	// The test window may be inactive, and only a popup opened in an active window sees the deactivation.
	// On Android the resumed activity already keeps the window active.
	private static IDisposable ActivateWindow()
	{
		if (OperatingSystem.IsAndroid())
		{
			return Disposable.Empty;
		}

		var nativeWindow = GetNativeWindow();
		var initialState = nativeWindow.ActivationState;
		nativeWindow.ActivationState = CoreWindowActivationState.CodeActivated;

		return Disposable.Create(() => nativeWindow.ActivationState = initialState);
	}

	private static NativeWindowWrapperBase GetNativeWindow()
	{
		var nativeWindow = TestServices.WindowHelper.XamlRoot.HostWindow?.NativeWrapper as NativeWindowWrapperBase;
		Assert.IsNotNull(nativeWindow);
		return nativeWindow;
	}

	private static void InvokeLifecycleMethod(object activity, string name)
		=> activity.GetType()
			.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes)!
			.Invoke(activity, null);

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
}
#endif
