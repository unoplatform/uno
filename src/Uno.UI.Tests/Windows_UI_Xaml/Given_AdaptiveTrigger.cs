using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_AdaptiveTrigger
	{
		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_SingleActiveState()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 10, MinWindowWidth = 10 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);

			Window.Current.SetWindowSize(new Size(1, 1));
			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleActiveState_ExactValue()
		{
			Window.Current.SetWindowSize(new Size(100d, 100d));
			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 100d, MinWindowWidth = 100d };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.States.Add(state);

			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleInactiveState()
		{
			Window.Current.SetWindowSize(new Size(5, 5));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowHeight = 10, MinWindowWidth = 10 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(null);

			Window.Current.SetWindowSize(new Size(15, 15));
			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleActiveState_DefaultValue()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowWidth = 0 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);
		}

		[TestMethod]
		public void When_SingleWithTwoConstraints_FailingWidth()
		{
			Window.Current.SetWindowSize(new Size(100, 100));

			var border = new Border();

			var sut = new AdaptiveTrigger { MinWindowWidth = 101, MinWindowHeight = 42 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });
			border.ForceLoaded();

			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleWithTwoConstraints_FailingHeight()
		{
			Window.Current.SetWindowSize(new Size(100, 100));
			var border = new Border();

			var sut = new AdaptiveTrigger { MinWindowWidth = 42, MinWindowHeight = 101 };

			border.ForceLoaded();

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(null);
		}

		[TestMethod]
		public void When_SingleNoConstraints()
		{
			Window.Current.SetWindowSize(new Size(100, 100));
			var border = new Border();
			border.ForceLoaded();

			var sut = new AdaptiveTrigger { MinWindowWidth = 0, MinWindowHeight = 0 };

			var state = new VisualState { Name = "activeState" };
			state.StateTriggers.Add(sut);

			var group = new VisualStateGroup();
			group.States.Add(state);

			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { group });

			group.CurrentState.Should().Be(state);
		}

		[DataTestMethod]
		// when the widths differ, the widest win
		[DataRow(1, 100, 100, "{10,10}|{20,0}")]
		[DataRow(1, 100, 100, "{10,10}|{20,20}")]
		// when the widths are the same and the heights differ, the tallest win
		[DataRow(0, 100, 100, "{10,10}|{10,0}")]
		[DataRow(1, 100, 100, "{10,10}|{10,20}")]
		// when the widths and the heights are all same, the first in declaration order win
		[DataRow(0, 100, 100, "{10,}|{10,}")]
		[DataRow(0, 100, 100, "{,10}|{,10}")]
		[DataRow(0, 100, 100, "{10,10}|{10,10}")]
		public void When_Multiple_AdaptiveTriggers(int expectedIndex, int windowWidth, int windowHeight, string context)
		{
			Window.Current.SetWindowSize(new Size(windowWidth, windowHeight));
			var border = new Border();
			border.ForceLoaded();

			var sut = new VisualStateGroup();
			var states = context.Split('|')
				.Select(x => BuildAdaptiveTrigger(x.Trim('{', '}').Split(',')))
				.Select(x => new VisualState
				{
					StateTriggers = { x }
				})
				.ForEach(sut.States.Add);
			VisualStateManager.SetVisualStateGroups(border, new List<VisualStateGroup>() { sut });

			sut.CurrentState.Should().Be(sut.States[expectedIndex]);

			AdaptiveTrigger BuildAdaptiveTrigger(string[] args)
			{
				var result = new AdaptiveTrigger();
				if (args[0] is { Length: > 0 } arg0)
				{
					result.MinWindowWidth = double.Parse(arg0);
				}
				if (args[1] is { Length: > 0 } arg1)
				{
					result.MinWindowHeight = double.Parse(arg1);
				}

				return result;
			}
		}
	}
}
