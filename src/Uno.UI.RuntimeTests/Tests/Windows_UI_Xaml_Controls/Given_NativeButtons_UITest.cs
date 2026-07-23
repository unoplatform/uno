#if HAS_INPUT_INJECTOR || WINAPPSDK

using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Migrated from SamplesApp.UITests UnoSamples_Test_NativeButtons.cs (Buttons_Native sample).
// The sample's "native style" chrome (wasm:Style/win:Style NativeDefaultButton/NativeDefaultToggleSwitch
// resources) has no Skia equivalent, but the behavior it was really exercising - Click/Tapped/Command
// firing through a real pointer tap and IsEnabled gating a Button/ToggleSwitch - is generic control
// logic, unrelated to native rendering, so it is reconstructed here with default (Skia) styling.
[TestClass]
[RunsOnUIThread]
public class Given_NativeButtons_UITest
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_Button_Tapped_Fires_Click_Tapped_And_Command_Respecting_IsEnabled()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var result = "No value";
		var resultTapped = "No value";
		var resultCommand = "No command";
		var clickCount = 0;
		var tappedCount = 0;
		var commandCount = 0;

		void OnClick(object sender, RoutedEventArgs e) =>
			result = $"Button {((Button)sender).Name} Clicked ({++clickCount})";

		void OnTapped(object sender, TappedRoutedEventArgs e) =>
			resultTapped = $"Button {((Button)sender).Name} Tapped ({++tappedCount})";

		var clickCommand = new DelegateCommand(o => resultCommand = $"Command {o} ({++commandCount})");

		var button01 = new Button { Name = "button01", Content = "Button 01", Command = clickCommand, CommandParameter = "Button 01" };
		button01.Click += OnClick;
		button01.Tapped += OnTapped;

		var button02 = new Button { Name = "button02", Content = "Button 02", IsEnabled = false, Command = clickCommand, CommandParameter = "Button 02" };
		button02.Click += OnClick;
		button02.Tapped += OnTapped;

		var enableButton02 = new Button { Content = "Enable Button 02" };
		enableButton02.Click += (_, _) => button02.IsEnabled = true;

		var panel = new StackPanel { Children = { button01, button02, enableButton02 } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(FrameworkElement target)
			{
				var center = target.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
					.TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
				mouse.Press(center);
				mouse.Release();
				await WaitForIdle();
			}

			await Tap(button01);
			Assert.AreEqual("Button button01 Clicked (1)", result);
			Assert.AreEqual("Button button01 Tapped (1)", resultTapped);
			Assert.AreEqual("Command Button 01 (1)", resultCommand);

			// button02 is disabled: tapping it must not raise Click/Tapped/Command.
			await Tap(button02);
			Assert.AreEqual("Button button01 Clicked (1)", result);
			Assert.AreEqual("Button button01 Tapped (1)", resultTapped);
			Assert.AreEqual("Command Button 01 (1)", resultCommand);

			await Tap(enableButton02);
			await Tap(button02);
			Assert.AreEqual("Button button02 Clicked (2)", result);
			Assert.AreEqual("Button button02 Tapped (2)", resultTapped);
			Assert.AreEqual("Command Button 02 (2)", resultCommand);
		}
		finally
		{
			WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_ToggleSwitch_Tapped_Fires_Toggled_Respecting_IsEnabled()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var result = "No value";
		var toggleCount = 0;

		void OnToggled(object sender, RoutedEventArgs e)
		{
			var toggleSwitch = (ToggleSwitch)sender;
			result = $"ToggleSwitch {toggleSwitch.Name} Toggled {toggleSwitch.IsOn} ({++toggleCount})";
		}

		var toggleSwitch01 = new ToggleSwitch { Name = "toggleSwitch01", OnContent = "On Content", OffContent = "Off Content" };
		toggleSwitch01.Toggled += OnToggled;

		var toggleSwitch02 = new ToggleSwitch { Name = "toggleSwitch02", IsEnabled = false, OnContent = "On Content", OffContent = "Off Content" };
		toggleSwitch02.Toggled += OnToggled;

		var enableToggleSwitch02 = new Button { Content = "Enable ToggleSwitch 02" };
		enableToggleSwitch02.Click += (_, _) => toggleSwitch02.IsEnabled = true;

		var panel = new StackPanel { Children = { toggleSwitch01, toggleSwitch02, enableToggleSwitch02 } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(FrameworkElement target)
			{
				var center = target.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
					.TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
				mouse.Press(center);
				mouse.Release();
				await WaitForIdle();
			}

			await Tap(toggleSwitch01);
			Assert.AreEqual("ToggleSwitch toggleSwitch01 Toggled True (1)", result);

			await Tap(toggleSwitch01);
			Assert.AreEqual("ToggleSwitch toggleSwitch01 Toggled False (2)", result);

			// toggleSwitch02 is disabled: tapping it must not raise Toggled.
			await Tap(toggleSwitch02);
			Assert.AreEqual("ToggleSwitch toggleSwitch01 Toggled False (2)", result);

			await Tap(enableToggleSwitch02);
			await Tap(toggleSwitch02);
			Assert.AreEqual("ToggleSwitch toggleSwitch02 Toggled True (3)", result);

			await Tap(toggleSwitch02);
			Assert.AreEqual("ToggleSwitch toggleSwitch02 Toggled False (4)", result);
		}
		finally
		{
			WindowContent = null;
		}
	}

	private sealed class DelegateCommand : ICommand
	{
		private readonly System.Action<object> _execute;

		public DelegateCommand(System.Action<object> execute) => _execute = execute;

		public event System.EventHandler CanExecuteChanged { add { } remove { } }

		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter) => _execute(parameter);
	}
}

#endif
