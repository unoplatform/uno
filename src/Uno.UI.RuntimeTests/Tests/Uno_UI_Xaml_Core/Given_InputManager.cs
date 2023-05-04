#if __SKIA__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Extensions;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

[TestClass]
[RunsOnUIThread]
public class Given_InputManager
{
	[TestMethod]
#if !__SKIA__
	[Ignore("Pointer injection supported only on skia for now.")]
#endif
	public async Task When_VisibilityChangesWhileDispatching_Then_RecomputeOriginalSource()
	{
		if (Private.Infrastructure.TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("Pointer injection is not supported yet on XamlIsland");
			return;
		}

		Border col1, col2;
		var ui = new Grid
		{
			Width = 200,
			Height = 200,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
			},
			Children =
			{
				(col1 = new Border { Background = new SolidColorBrush(Colors.DeepPink) }.GridColumn(0)),
				(col2 = new Border { Background = new SolidColorBrush(Colors.DeepSkyBlue) }.GridColumn(1)),
			}
		};

		var position = await UITestHelper.Load(ui);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var failed = false;
		col1.PointerExited += (snd, args) => col2.Visibility = Visibility.Collapsed;
		col2.PointerEntered += (snd, args) => failed = true;

		injector.GetFinger().Drag(position.Location.Offset(10), position.Location.Offset(180, 10));

		Assert.AreEqual(Visibility.Collapsed, col2.Visibility, "The visibility should have been changed when the pointer left the col1.");
		Assert.IsFalse(failed, "The pointer should not have been dispatched to the col2 as it has been set to visibility collapsed.");
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("Pointer injection supported only on skia for now.")]
#endif
	public async Task When_Hover_No_Delay_For_VisualState_Update()
	{
		if (Private.Infrastructure.TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("Pointer injection is not supported yet on XamlIsland");
			return;
		}

		var comboxBoxItem = new ComboBoxItem()
		{
			Content = "ComboBoxItem Content",
		};

		var position = await UITestHelper.Load(comboxBoxItem);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var mouse = injector.GetMouse();

		var oldState = VisualStateManager.GetCurrentState(comboxBoxItem, "CommonStates").Name;
		mouse.MoveTo(position.GetCenter().X, position.GetCenter().Y);
		var newState = VisualStateManager.GetCurrentState(comboxBoxItem, "CommonStates").Name;
		Assert.AreEqual("Normal", oldState);
		Assert.AreEqual("PointerOver", newState);
	}
}
#endif
