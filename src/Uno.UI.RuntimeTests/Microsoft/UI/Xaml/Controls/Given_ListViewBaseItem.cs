using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;
using Uno.UI.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>Records every state name the VSM routes to the item template root, in order.</summary>
public partial class ListViewBaseItem_StateRecorder : Grid
{
	public List<string> States { get; } = new();

	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		States.Add(stateName);
		return false;
	}
}

[TestClass]
[RunsOnUIThread]
public class Given_ListViewBaseItem
{
	private static readonly string[] CommonStates = { "Normal", "PointerOver", "Pressed", "Selected", "PointerOverSelected", "PressedSelected" };

	private const string RecordingStyle =
		"""
		<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			   xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls"
			   TargetType="ListViewItem">
			<Setter Property="IsTabStop" Value="True" />
			<Setter Property="Height" Value="60" />
			<Setter Property="Template">
				<Setter.Value>
					<ControlTemplate TargetType="ListViewItem">
						<local:ListViewBaseItem_StateRecorder Background="Transparent">
							<ContentPresenter />
						</local:ListViewBaseItem_StateRecorder>
					</ControlTemplate>
				</Setter.Value>
			</Setter>
		</Style>
		""";

	private static async Task<(ListView List, ListViewItem Item, ListViewBaseItem_StateRecorder Recorder)> Setup(
		ListViewSelectionMode selectionMode = ListViewSelectionMode.Single,
		bool isItemClickEnabled = false)
	{
		var list = new ListView
		{
			Width = 200,
			Height = 300,
			SelectionMode = selectionMode,
			IsItemClickEnabled = isItemClickEnabled,
			ItemContainerStyle = (Style)XamlReader.Load(RecordingStyle),
			ItemsSource = new[] { "A", "B", "C" },
		};

		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await UITestHelper.WaitFor(() => list.ContainerFromIndex(0) is ListViewItem { IsLoaded: true }, timeoutMS: 3000);
		await WindowHelper.WaitForIdle();

		var item = (ListViewItem)list.ContainerFromIndex(0);
		var recorder = (ListViewBaseItem_StateRecorder)VisualTreeHelper.GetChild(item, 0);

		return (list, item, recorder);
	}

	private static string LastCommonState(ListViewBaseItem_StateRecorder recorder)
		=> recorder.States.LastOrDefault(s => CommonStates.Contains(s));

	private static Point Center(FrameworkElement element)
	{
		var bounds = element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
		return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
	}

	[TestMethod]
	public async Task When_ChangeVisualState_Emits_WinUI_Sequence()
	{
#if HAS_UNO
		using var _ = ListViewChromeHelper.UseNonRoundedChrome();
		const string indicatorState = "SelectionIndicatorDisabled";
#else
		const string indicatorState = "SelectionIndicatorEnabled";
#endif
		var (_, item, recorder) = await Setup();

		recorder.States.Clear();
		item.IsSelected = true;

		CollectionAssert.AreEqual(
			new[] { "DataAvailable", "Unfocused", "MultiSelectDisabled", indicatorState, "Enabled", "Selected", "NoReorderHint", "NotDragging" },
			recorder.States,
			string.Join(", ", recorder.States));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Rounded_Chrome_Single_Mode_Indicator_Enabled()
	{
#if HAS_UNO
		using var _ = ListViewChromeHelper.UseRoundedChromeResource(true);
#endif
		var (list, item, recorder) = await Setup();

		recorder.States.Clear();
		item.IsSelected = true;
		Assert.IsTrue(recorder.States.Contains("SelectionIndicatorEnabled"), string.Join(", ", recorder.States));

		list.SelectionMode = ListViewSelectionMode.Multiple;
		await WindowHelper.WaitForIdle();
		recorder.States.Clear();
		item.IsSelected = true;
		item.IsSelected = false;

		Assert.IsTrue(recorder.States.Contains("SelectionIndicatorDisabled"), string.Join(", ", recorder.States));
		Assert.IsTrue(recorder.States.Contains("MultiSelectEnabled"), string.Join(", ", recorder.States));
	}

	[TestMethod]
	public async Task When_Mouse_Hover_PointerOver()
	{
		var (_, item, recorder) = await Setup();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var mouse = injector.GetMouse();

		mouse.MoveTo(Center(item));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("PointerOver", LastCommonState(recorder));

		mouse.MoveTo(new Point(Center(item).X, Center(item).Y + 1000));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Normal", LastCommonState(recorder));
	}

	[TestMethod]
	public async Task When_Touch_Press_Pressed_After_Delay()
	{
		var (_, item, recorder) = await Setup(ListViewSelectionMode.None, isItemClickEnabled: true);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var finger = injector.GetFinger();

		finger.Press(Center(item));
		await Task.Delay(20);
		Assert.AreEqual("Normal", LastCommonState(recorder), "Touch must not go to Pressed before the 100 ms delay.");
		Assert.IsFalse(recorder.States.Contains("PointerOver"), "Touch never shows PointerOver.");

		await Task.Delay(300);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Pressed", LastCommonState(recorder));

		finger.Release();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Normal", LastCommonState(recorder));
	}

	[TestMethod]
	public async Task When_Touch_Tap_Holds_Pressed()
	{
		var (_, item, recorder) = await Setup(ListViewSelectionMode.None, isItemClickEnabled: true);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var finger = injector.GetFinger();

		finger.Press(Center(item));
		finger.Release();
		Assert.AreEqual("Pressed", LastCommonState(recorder), "A quick tap shows Pressed for 125 ms.");

		await Task.Delay(400);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Normal", LastCommonState(recorder));
	}

	[TestMethod]
	public async Task When_Right_Button_Pressed()
	{
		var (_, item, recorder) = await Setup();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var mouse = injector.GetMouse();

		mouse.PressRight(Center(item));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Pressed", LastCommonState(recorder));

		mouse.ReleaseRight();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("PointerOver", LastCommonState(recorder));

		mouse.MoveTo(new Point(Center(item).X, Center(item).Y + 1000));
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task When_GamepadA_Pressed_But_Not_Enter()
	{
		var (_, item, recorder) = await Setup(ListViewSelectionMode.None, isItemClickEnabled: true);
		item.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.PressKeySequence("$d$_GamepadA", item);
		Assert.AreEqual("Pressed", LastCommonState(recorder));

		await KeyboardHelper.PressKeySequence("$u$_GamepadA", item);
		Assert.AreEqual("Normal", LastCommonState(recorder));

		await KeyboardHelper.PressKeySequence("$d$_enter", item);
		Assert.AreEqual("Normal", LastCommonState(recorder), "Enter does not drive the Pressed state.");
		await KeyboardHelper.PressKeySequence("$u$_enter", item);
	}

	[TestMethod]
	public async Task When_SelectionMode_None_Masks_Selected()
	{
		var (list, item, recorder) = await Setup();

		item.IsSelected = true;
		Assert.AreEqual("Selected", LastCommonState(recorder));

		list.SelectionMode = ListViewSelectionMode.None;
		item.IsSelected = true;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("Normal", LastCommonState(recorder));
	}

	[TestMethod]
	public async Task When_ReadOnly_Masks_PointerOver()
	{
		var (list, item, recorder) = await Setup(ListViewSelectionMode.None, isItemClickEnabled: false);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var mouse = injector.GetMouse();

		mouse.MoveTo(Center(item));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Normal", LastCommonState(recorder), "Read-only ListView must not show PointerOver.");

		list.IsItemClickEnabled = true;
		mouse.MoveTo(new Point(Center(item).X, Center(item).Y + 1000));
		mouse.MoveTo(Center(item));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("PointerOver", LastCommonState(recorder));

		mouse.MoveTo(new Point(Center(item).X, Center(item).Y + 1000));
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task When_Disabled_Clears_Touch_Flags()
	{
		var (_, item, recorder) = await Setup(ListViewSelectionMode.None, isItemClickEnabled: true);
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available");
		using var finger = injector.GetFinger();

		finger.Press(Center(item));
		item.IsEnabled = false;
		Assert.IsTrue(recorder.States.Contains("Disabled"), string.Join(", ", recorder.States));

		item.IsEnabled = true;
		await Task.Delay(300);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("Normal", LastCommonState(recorder), "Disabling stops the touch timer, so Pressed never comes.");
		finger.Release();
	}
}
