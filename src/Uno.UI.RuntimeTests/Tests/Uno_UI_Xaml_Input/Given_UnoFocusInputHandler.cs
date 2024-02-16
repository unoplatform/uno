#if HAS_UNO // Testing internal UnoFocusInputHandler, not available on Windows
using System.Threading.Tasks;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Input;

[TestClass]
[RunsOnUIThread]
public class Given_UnoFocusInputHandler
{
	private const int CenterButtonIndex = 4;
	private const int LeftButtonIndex = 3;
	private const int RightButtonIndex = 5;
	private const int TopButtonIndex = 1;
	private const int BottomButtonIndex = 7;

	[DataRow(XYFocusKeyboardNavigationMode.Enabled, VirtualKey.Left, true, LeftButtonIndex)]
	[DataRow(XYFocusKeyboardNavigationMode.Enabled, VirtualKey.Right, true, RightButtonIndex)]
	[DataRow(XYFocusKeyboardNavigationMode.Enabled, VirtualKey.Up, true, TopButtonIndex)]
	[DataRow(XYFocusKeyboardNavigationMode.Enabled, VirtualKey.Down, true, BottomButtonIndex)]
	[DataRow(XYFocusKeyboardNavigationMode.Disabled, VirtualKey.Down, false, -1)]
	[RequiresFullWindow]
	[TestMethod]
	public async Task When_VerifyEnabledXYKeyboardNavigation(XYFocusKeyboardNavigationMode mode, VirtualKey key, bool shouldSucceed, int targetIndex)
	{
		await VerifyXYNavigationAsync(
			mode,
			key,
			shouldSucceed,
			targetIndex);
	}

	[TestMethod]
	public async Task When_Tab_BringIntoView()
	{
		var ts1 = new ToggleSwitch();
		var ts2 = new ToggleSwitch();
		var SUT = new ScrollViewer
		{
			new StackPanel
			{
				Spacing = 1200,
				Children =
				{
					ts1,
					ts2
				}
			}
		};

		await UITestHelper.Load(SUT);

		Assert.AreEqual(0, SUT.VerticalOffset);

		ts1.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, SUT.VerticalOffset);

		KeyboardHelper.Tab();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(SUT.ScrollableHeight, SUT.VerticalOffset);
	}

	private async Task VerifyXYNavigationAsync(XYFocusKeyboardNavigationMode mode, VirtualKey key, bool shouldSucceed, int targetIndex)
	{
		var grid = CreateButtonGrid(mode);

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForIdle();

		var centerButton = (Button)grid.Children[CenterButtonIndex];

		centerButton.Focus(FocusState.Programmatic);

		var inputHandler = new UnoFocusInputHandler(VisualTree.GetRootOrIslandForElement(centerButton));
		var result = inputHandler.TryHandleDirectionalFocus(key);

		Assert.AreEqual(shouldSucceed, result);
		if (shouldSucceed)
		{
			var button = grid.Children[targetIndex];
			Assert.AreEqual(button, FocusManager.GetFocusedElement(grid.XamlRoot));
		}
	}

	private Grid CreateButtonGrid(XYFocusKeyboardNavigationMode navigationMode)
	{
		var grid = new Grid() { XYFocusKeyboardNavigation = navigationMode };
		for (int i = 0; i < 3; i++)
		{
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(64) });
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(64) });
		}

		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				var button = new Button()
				{
					Content = grid.Children.Count.ToString(),
					XYFocusKeyboardNavigation = navigationMode,
				};

				grid.Children.Add(button);

				Grid.SetColumn(button, x);
				Grid.SetRow(button, y);
			}
		}

		return grid;
	}
}
#endif
