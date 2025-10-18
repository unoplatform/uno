using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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
		var customEvent = new RoutedEvent(Uno.UI.Xaml.RoutedEventFlag.None, "CustomEvent");

		// Attempting to set a non-Loaded RoutedEvent should throw
		Assert.ThrowsException<NotSupportedException>(() =>
		{
			eventTrigger.RoutedEvent = customEvent;
		}, "Setting a non-Loaded RoutedEvent should throw NotSupportedException");
	}

	[TestMethod]
	public void When_EventTrigger_With_Loaded_Event_Succeeds()
	{
		var eventTrigger = new EventTrigger();

		// Setting the Loaded event should succeed
		eventTrigger.RoutedEvent = FrameworkElement.LoadedEvent;

		Assert.AreEqual(FrameworkElement.LoadedEvent, eventTrigger.RoutedEvent);
	}
}
