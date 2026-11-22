#if HAS_UNO
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Sends Back and keys through the Android activity the way the system does: the Back key and back swipe (through the
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
