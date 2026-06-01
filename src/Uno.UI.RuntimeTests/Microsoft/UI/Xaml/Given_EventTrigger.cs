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

		// Wait for the storyboard to complete and opacity to reach target
		await TestServices.WindowHelper.WaitFor(() => border.Opacity >= 0.99, timeoutMS: 2000);

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

	[TestMethod]
	public async Task When_Multiple_EventTriggers_On_Same_Element()
	{
		int trigger1Fired = 0;
		int trigger2Fired = 0;

		var border = new Border { Width = 50, Height = 50 };

		var storyboard1 = new Storyboard();
		storyboard1.Completed += (s, e) => trigger1Fired++;
		var storyboard2 = new Storyboard();
		storyboard2.Completed += (s, e) => trigger2Fired++;

		var eventTrigger1 = new EventTrigger();
		eventTrigger1.Actions.Add(new BeginStoryboard { Storyboard = storyboard1 });

		var eventTrigger2 = new EventTrigger();
		eventTrigger2.Actions.Add(new BeginStoryboard { Storyboard = storyboard2 });

		border.Triggers.Add(eventTrigger1);
		border.Triggers.Add(eventTrigger2);

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.WaitFor(() => trigger1Fired > 0 && trigger2Fired > 0);

		Assert.AreEqual(1, trigger1Fired, "First trigger should have fired exactly once");
		Assert.AreEqual(1, trigger2Fired, "Second trigger should have fired exactly once");
	}

	[TestMethod]
	public async Task When_EventTrigger_With_Null_Storyboard_Does_Not_Throw()
	{
		var border = new Border { Width = 50, Height = 50 };

		// BeginStoryboard without setting Storyboard property (defaults to null)
		var beginStoryboard = new BeginStoryboard();

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(beginStoryboard);
		border.Triggers.Add(eventTrigger);

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Should not throw — null Storyboard is silently ignored
		Assert.IsNull(beginStoryboard.Storyboard);
	}

	[TestMethod]
	public async Task When_Empty_EventTrigger_Does_Not_Throw()
	{
		var border = new Border { Width = 50, Height = 50 };

		// EventTrigger with no actions
		var eventTrigger = new EventTrigger();
		border.Triggers.Add(eventTrigger);

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Should not throw — empty Actions collection is harmless
		Assert.AreEqual(0, eventTrigger.Actions.Count);
	}

	[TestMethod]
	public async Task When_Element_Re_Added_To_Tree_Triggers_Fire_Again()
	{
		int fireCount = 0;

		var parent = new Grid { Width = 100, Height = 100 };
		var border = new Border { Width = 50, Height = 50 };

		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => fireCount++;

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });
		border.Triggers.Add(eventTrigger);

		parent.Children.Add(border);

		// First load
		TestServices.WindowHelper.WindowContent = parent;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitFor(() => fireCount >= 1, timeoutMS: 2000);
		Assert.AreEqual(1, fireCount, "Trigger should have fired on first load");

		// Remove from tree
		parent.Children.Remove(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Re-add to tree
		parent.Children.Add(border);
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitFor(() => fireCount >= 2, timeoutMS: 2000);

		Assert.AreEqual(2, fireCount, "Trigger should have fired again when re-added to tree");
	}

	[TestMethod]
	public async Task When_Trigger_Added_After_Loaded_Via_Insert()
	{
		bool storyboardStarted = false;

		var border = new Border { Width = 50, Height = 50 };

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });

		// Use Insert instead of Add
		border.Triggers.Insert(0, eventTrigger);

		await TestServices.WindowHelper.WaitFor(() => storyboardStarted, timeoutMS: 2000);
		Assert.IsTrue(storyboardStarted, "Trigger added via Insert should fire immediately on already-loaded element");
	}

	[TestMethod]
	public async Task When_Trigger_Added_After_Loaded_Via_Indexer()
	{
		bool storyboardStarted = false;

		var border = new Border { Width = 50, Height = 50 };

		// Add a placeholder trigger first
		var placeholder = new EventTrigger();
		border.Triggers.Add(placeholder);

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();

		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });

		// Replace via indexer
		border.Triggers[0] = eventTrigger;

		await TestServices.WindowHelper.WaitFor(() => storyboardStarted, timeoutMS: 2000);
		Assert.IsTrue(storyboardStarted, "Trigger set via indexer should fire immediately on already-loaded element");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI crashes with AccessViolation when setting RoutedEvent to null
	public void When_EventTrigger_RoutedEvent_Set_To_Null_Does_Not_Throw()
	{
		var eventTrigger = new EventTrigger();
		eventTrigger.RoutedEvent = null;
		Assert.IsNull(eventTrigger.RoutedEvent);
	}

	[TestMethod]
	public async Task When_Parent_And_Child_Both_Have_Triggers()
	{
		int parentFired = 0;
		int childFired = 0;

		var parent = new Grid { Width = 100, Height = 100 };
		var child = new Border { Width = 50, Height = 50 };

		var parentStoryboard = new Storyboard();
		parentStoryboard.Completed += (s, e) => parentFired++;
		var parentTrigger = new EventTrigger();
		parentTrigger.Actions.Add(new BeginStoryboard { Storyboard = parentStoryboard });
		parent.Triggers.Add(parentTrigger);

		var childStoryboard = new Storyboard();
		childStoryboard.Completed += (s, e) => childFired++;
		var childTrigger = new EventTrigger();
		childTrigger.Actions.Add(new BeginStoryboard { Storyboard = childStoryboard });
		child.Triggers.Add(childTrigger);

		parent.Children.Add(child);

		TestServices.WindowHelper.WindowContent = parent;
		await TestServices.WindowHelper.WaitForLoaded(child);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.WaitFor(() => parentFired > 0 && childFired > 0);

		Assert.AreEqual(1, parentFired, "Parent trigger should have fired");
		Assert.AreEqual(1, childFired, "Child trigger should have fired");
	}

	[TestMethod]
	public void When_XamlReader_Parses_Subtype_Loaded_Event()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
	<Border.Triggers>
		<EventTrigger RoutedEvent='Button.Loaded'>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		var border = (Border)XamlReader.Load(xaml);
		var eventTrigger = (EventTrigger)border.Triggers[0];

		Assert.IsNotNull(eventTrigger.RoutedEvent, "RoutedEvent should be set when using a FrameworkElement subtype");
	}

	[TestMethod]
	public void When_XamlReader_Rejects_Non_FrameworkElement_Type()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
	<Border.Triggers>
		<EventTrigger RoutedEvent='SolidColorBrush.Loaded'>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		Assert.Throws<Exception>(() =>
		{
			XamlReader.Load(xaml);
		});
	}

	[TestMethod]
	public async Task When_Triggers_Collection_Cleared_Before_Loaded()
	{
		bool storyboardStarted = false;

		var border = new Border { Width = 50, Height = 50 };

		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });
		border.Triggers.Add(eventTrigger);

		// Clear before loading
		border.Triggers.Clear();

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();
		// Short delay for negative assertion — verifying the storyboard does NOT start
		await Task.Delay(100);

		Assert.IsFalse(storyboardStarted, "Storyboard should not have started after triggers were cleared");
	}

	[TestMethod]
	public async Task When_EventTrigger_Removed_Before_Loaded()
	{
		bool storyboardStarted = false;

		var border = new Border { Width = 50, Height = 50 };

		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => storyboardStarted = true;

		var eventTrigger = new EventTrigger();
		eventTrigger.Actions.Add(new BeginStoryboard { Storyboard = storyboard });
		border.Triggers.Add(eventTrigger);

		// Remove before loading
		border.Triggers.Remove(eventTrigger);

		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		await TestServices.WindowHelper.WaitForIdle();
		// Short delay for negative assertion — verifying the storyboard does NOT start
		await Task.Delay(100);

		Assert.IsFalse(storyboardStarted, "Storyboard should not have started after trigger was removed");
	}

	[TestMethod]
	public void When_XamlReader_Rejects_Missing_Dot_In_RoutedEvent()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
	<Border.Triggers>
		<EventTrigger RoutedEvent='Loaded'>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		Assert.Throws<Exception>(() =>
		{
			XamlReader.Load(xaml);
		});
	}

	[TestMethod]
	public void When_EventTrigger_Default_No_RoutedEvent_In_Xaml()
	{
		var xaml = @"
<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        Width='50' Height='50'>
	<Border.Triggers>
		<EventTrigger>
			<BeginStoryboard>
				<Storyboard />
			</BeginStoryboard>
		</EventTrigger>
	</Border.Triggers>
</Border>";

		var border = (Border)XamlReader.Load(xaml);

		Assert.AreEqual(1, border.Triggers.Count, "Should have one trigger");
		var eventTrigger = (EventTrigger)border.Triggers[0];
		Assert.AreEqual(1, eventTrigger.Actions.Count, "Should have one action");
		Assert.IsInstanceOfType<BeginStoryboard>(eventTrigger.Actions[0]);
	}
}
