using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_EventTrigger
{
	[TestMethod]
	public async Task When_EventTrigger_With_BeginStoryboard_On_Loaded()
	{
		var grid = new Grid
		{
			Width = 100,
			Height = 100
		};

		var border = new Border
		{
			Width = 50,
			Height = 50,
			Opacity = 0
		};

		// Create storyboard to animate opacity
		var storyboard = new Storyboard();
		var animation = new DoubleAnimation
		{
			From = 0,
			To = 1,
			Duration = new Duration(TimeSpan.FromMilliseconds(100))
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		storyboard.Children.Add(animation);

		// Create BeginStoryboard action
		var beginStoryboard = new BeginStoryboard
		{
			Storyboard = storyboard
		};

		// Create EventTrigger
		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(beginStoryboard);

		// Add trigger to border
		border.Triggers.Add(eventTrigger);

		grid.Children.Add(border);

		// Add to visual tree
		TestServices.WindowHelper.WindowContent = grid;
		await TestServices.WindowHelper.WaitForLoaded(grid);
		await TestServices.WindowHelper.WaitForIdle();

		// Give the animation time to start and complete
		await Task.Delay(200);

		// Verify that the storyboard ran and opacity changed
		Assert.AreEqual(1.0, border.Opacity, 0.01, "Border opacity should be 1 after storyboard completes");
	}

	[TestMethod]
	public async Task When_EventTrigger_Without_RoutedEvent_Defaults_To_Loaded()
	{
		bool storyboardStarted = false;

		var border = new Border
		{
			Width = 50,
			Height = 50
		};

		// Create storyboard
		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		// Create BeginStoryboard action
		var beginStoryboard = new BeginStoryboard
		{
			Storyboard = storyboard
		};

		// Create EventTrigger without setting RoutedEvent
		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(beginStoryboard);

		// Add trigger to border
		border.Triggers.Add(eventTrigger);

		// Add to visual tree
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Wait for storyboard to complete
		await TestServices.WindowHelper.WaitFor(() => storyboardStarted);

		Assert.IsTrue(storyboardStarted, "Storyboard should have started when element loaded");
	}

	[TestMethod]
	public async Task When_Multiple_Actions_In_EventTrigger()
	{
		int action1Executed = 0;
		int action2Executed = 0;

		var border = new Border
		{
			Width = 50,
			Height = 50
		};

		// Create first storyboard
		var storyboard1 = new Storyboard();
		storyboard1.Completed += (s, e) => action1Executed++;

		// Create second storyboard
		var storyboard2 = new Storyboard();
		storyboard2.Completed += (s, e) => action2Executed++;

		// Create BeginStoryboard actions
		var beginStoryboard1 = new BeginStoryboard { Storyboard = storyboard1 };
		var beginStoryboard2 = new BeginStoryboard { Storyboard = storyboard2 };

		// Create EventTrigger with multiple actions
		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(beginStoryboard1);
		eventTrigger.Actions.Add(beginStoryboard2);

		// Add trigger to border
		border.Triggers.Add(eventTrigger);

		// Add to visual tree
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Wait for storyboards to complete
		await TestServices.WindowHelper.WaitFor(() => action1Executed > 0 && action2Executed > 0);

		Assert.AreEqual(1, action1Executed, "First storyboard should have executed");
		Assert.AreEqual(1, action2Executed, "Second storyboard should have executed");
	}

	[TestMethod]
	public void When_EventTrigger_With_Non_Loaded_Event_Throws()
	{
		var eventTrigger = new EventTrigger();

		// Create a custom routed event (not Loaded)
		var customEvent = FrameworkElement.PointerCanceledEvent;

		// Attempting to set a non-Loaded RoutedEvent should throw
		Assert.ThrowsExactly<ArgumentException>(() =>
		{
			eventTrigger.RoutedEvent = customEvent;
		}, "Setting a non-Loaded RoutedEvent should throw ArgumentException");
	}

	[TestMethod]
	public void When_EventTrigger_With_Loaded_Event_XamlReader()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
	<Border.Triggers>
		<EventTrigger RoutedEvent='FrameworkElement.Loaded'>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		var xamlReader = XamlReader.Load(xaml);
		var border = (Border)xamlReader;
		var eventTrigger = (EventTrigger)border.Triggers[0];

		Assert.IsNotNull(eventTrigger.RoutedEvent);
	}

	[TestMethod]
	public void When_EventTrigger_With_Invalid_Event_XamlReader()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
	<Border.Triggers>
		<EventTrigger RoutedEvent='FrameworkElement.Invalid'>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		Assert.Throws<Exception>(() =>
		{
			Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
		});
	}

	[TestMethod]
	public async Task When_EventTrigger_In_Xaml()
	{
		var page = new EventTrigger_Xaml_Valid();
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);

		Assert.AreEqual(Colors.Red, page.BorderBackgroundColor);
	}

	[TestMethod]
	public async Task When_EventTrigger_Set_After_Element_Is_Already_Loaded()
	{
		bool storyboardStarted = false;

		var border = new Border
		{
			Width = 50,
			Height = 50
		};

		// Add to visual tree first (element gets loaded)
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Verify element is loaded
		Assert.IsTrue(border.IsLoaded, "Border should be loaded before adding triggers");

		// Create storyboard
		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		// Create BeginStoryboard action
		var beginStoryboard = new BeginStoryboard
		{
			Storyboard = storyboard
		};

		// Create EventTrigger and add AFTER element is already loaded
		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(beginStoryboard);

		// Add trigger to border (element is already loaded)
		border.Triggers.Add(eventTrigger);

		// Wait for storyboard to complete - should fire immediately since element is already loaded
		await TestServices.WindowHelper.WaitFor(() => storyboardStarted, timeoutMS: 2000);

		Assert.IsTrue(storyboardStarted, "Storyboard should have started even when trigger is set after element is loaded");
	}
}
