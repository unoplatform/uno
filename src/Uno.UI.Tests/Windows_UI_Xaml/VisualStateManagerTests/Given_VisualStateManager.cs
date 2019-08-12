using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml.VisualStateManagerTests
{
	[TestClass]
	public class Given_VisualStateManager
	{
		[TestMethod]
		public void When_UsingStateTrigger()
		{
			var control = new Control {Name = "control"};
			var dispatcher = control.Dispatcher;

			var trigger1 = new CustomStateTrigger(true);
			var trigger2 = new CustomStateTrigger(false);

			var state1 = new VisualState
			{
				Name = "state1",
				Setters =
				{
					new Setter(new TargetPropertyPath("control", "Tag"), 1)
				},
				StateTriggers = new List<StateTriggerBase> { trigger1 }
			};
			var state2 = new VisualState
			{
				Name = "state2",
				Setters =
				{
					new Setter(new TargetPropertyPath("control", "Tag"), 2)
				},
				StateTriggers = new List<StateTriggerBase> { trigger2 }
			};
			var group = new VisualStateGroup();
			group.States.Add(state1);
			group.States.Add(state2);

			VisualStateManager.SetVisualStateGroups(control, new List<VisualStateGroup> {group});

			trigger1.Set();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
			control.Tag.Should().Be(1);

			trigger2.Set();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
			control.Tag.Should().Be(1);

			trigger1.Unset();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
			control.Tag.Should().Be(2);

			trigger2.Unset();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
			control.Tag.Should().Be(null);
		}

		[TestMethod]
		public void When_UsingStateTriggerAndStoryboard()
		{
			var control = new Control { Name = "control" };
			var dispatcher = control.Dispatcher;

			var trigger1 = new CustomStateTrigger(true);
			var trigger2 = new CustomStateTrigger(false);

			var completed1Count = 0;
			var completed2Count = 0;

			var storyboard1 = new Storyboard();
			storyboard1.Completed += (sender, eventArgs) => completed1Count++;
			var storyboard2 = new Storyboard();
			storyboard2.Completed += (sender, eventArgs) => completed2Count++;

			var state1 = new VisualState
			{
				Name = "state1",
				Storyboard = storyboard1,
				StateTriggers = new List<StateTriggerBase> { trigger1 }
			};
			var state2 = new VisualState
			{
				Name = "state2",
				Storyboard = storyboard2,
				StateTriggers = new List<StateTriggerBase> { trigger2 }
			};
			var group = new VisualStateGroup();
			group.States.Add(state1);
			group.States.Add(state2);

			VisualStateManager.SetVisualStateGroups(control, new List<VisualStateGroup> { group });

			completed1Count.Should().Be(0);
			completed2Count.Should().Be(0);

			// This is to workaround to the fact that the state is applied as soon as it's injected
			// in the states list, so it will be complete twice (once when added in the collection,
			// second when element is set ... which is not the behavior on Windows but is required for some other tests)
			// This should be removed once the VisualStateGroup (and other tests) have been fixed.
			completed1Count = -1;

			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			trigger1.Set();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			completed1Count.Should().Be(1);
			completed2Count.Should().Be(0);

			trigger2.Set();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			// Seems weird, but that's normal, it's following documented
			// precedence. "state2" will be set only after "trigger1" will
			// be unset.
			completed1Count.Should().Be(1);
			completed2Count.Should().Be(0);

			trigger1.Unset();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			completed1Count.Should().Be(1);
			completed2Count.Should().Be(1);

			trigger2.Unset();
			dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			completed1Count.Should().Be(1);
			completed2Count.Should().Be(1);
		}

		private class CustomStateTrigger : StateTrigger
		{
			internal CustomStateTrigger(bool initialState = true)
			{
				IsActive = initialState;
			}

			internal void Set()
			{
				IsActive = true;
			}

			internal void Unset()
			{
				IsActive = false;
			}
		}
	}
}
