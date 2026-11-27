using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_SplitButton
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Injected mouse input is rejected on WinAppSDK (E_INVALIDARG)
	public async Task When_Primary_Clicked_Then_Click_Raised()
	{
		var splitButton = CreateSplitButton();
		var clicks = 0;
		splitButton.Click += (_, _) => clicks++;

		await UITestHelper.Load(splitButton);
		var primary = GetPart(splitButton, "PrimaryButton");

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		try
		{
			mouse.Press(GetCenter(primary));
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, clicks);
		}
		finally
		{
			mouse.Release();
			mouse.MoveTo(new Point(0, 0));
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow("PrimaryButton", "PrimaryPressed")]
	[DataRow("SecondaryButton", "SecondaryPressed")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23021")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Injected mouse input is rejected on WinAppSDK (E_INVALIDARG)
	public async Task When_Part_Pressed_Then_VisualState_Pressed(string partName, string expectedState)
	{
		var splitButton = CreateSplitButton();

		await UITestHelper.Load(splitButton);
		var part = GetPart(splitButton, partName);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init InputInjector");
		using var mouse = injector.GetMouse();

		try
		{
			mouse.Press(GetCenter(part));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(part.IsPressed);
			Assert.AreEqual(expectedState, GetCommonState(splitButton));
		}
		finally
		{
			mouse.Release();
			mouse.MoveTo(new Point(0, 0));
			await WindowHelper.WaitForIdle();
			splitButton.Flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23021")]
	public async Task When_Flyout_Replaced_Then_Only_New_Flyout_Drives_State()
	{
		var oldFlyout = new Flyout { Content = new TextBlock { Text = "Old" } };
		var newFlyout = new Flyout { Content = new TextBlock { Text = "New" } };
		var splitButton = new SplitButton { Content = "Split", Flyout = oldFlyout };

		await UITestHelper.Load(splitButton);

		splitButton.Flyout = newFlyout;

		try
		{
			await ShowAndWaitForOpened(oldFlyout, splitButton);
			Assert.AreEqual("Normal", GetCommonState(splitButton));

			await HideAndWaitForClosed(oldFlyout);

			await ShowAndWaitForOpened(newFlyout, splitButton);
			Assert.AreEqual("FlyoutOpen", GetCommonState(splitButton));
		}
		finally
		{
			oldFlyout.Hide();
			newFlyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	// Opened/Closed are raised asynchronously on WinUI, so WaitForIdle alone can assert too early.
	private static async Task ShowAndWaitForOpened(Flyout flyout, FrameworkElement target)
	{
		var opened = new TaskCompletionSource<bool>();
		void OnOpened(object sender, object args) => opened.TrySetResult(true);

		flyout.Opened += OnOpened;
		try
		{
			flyout.ShowAt(target);
			await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			flyout.Opened -= OnOpened;
		}
	}

	private static async Task HideAndWaitForClosed(Flyout flyout)
	{
		var closed = new TaskCompletionSource<bool>();
		void OnClosed(object sender, object args) => closed.TrySetResult(true);

		flyout.Closed += OnClosed;
		try
		{
			flyout.Hide();
			await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await WindowHelper.WaitForIdle();
		}
		finally
		{
			flyout.Closed -= OnClosed;
		}
	}

	private static SplitButton CreateSplitButton()
		=> new() { Content = "Split", Flyout = new Flyout { Content = new TextBlock { Text = "Flyout" } } };

	private static Button GetPart(SplitButton splitButton, string name)
	{
		var part = splitButton.FindVisualChildByName(name) as Button;
		Assert.IsNotNull(part, $"Template part '{name}' not found");
		return part;
	}

	private static Point GetCenter(FrameworkElement element)
	{
		var bounds = element.GetAbsoluteBounds();
		return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
	}

	private static string GetCommonState(SplitButton splitButton)
		=> VisualTreeHelper.GetChild(splitButton, 0) is FrameworkElement root
			? VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g => g.Name == "CommonStates")?.CurrentState?.Name
			: null;
}
