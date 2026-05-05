using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// Tests in this file were migrated from the legacy Uno.UI.Tests unit-test project.
public partial class Given_VisualStateManager
{
	[TestMethod]
	public async Task When_UsingStateTrigger()
	{
		var control = new Control { Name = "control" };

		var trigger1 = new CustomStateTrigger(true);
		var trigger2 = new CustomStateTrigger(false);

		var state1 = new VisualState
		{
			Name = "state1",
			Setters = { new Setter(new TargetPropertyPath("control", "Tag"), 1) },
			StateTriggers = new List<StateTriggerBase> { trigger1 },
		};
		var state2 = new VisualState
		{
			Name = "state2",
			Setters = { new Setter(new TargetPropertyPath("control", "Tag"), 2) },
			StateTriggers = new List<StateTriggerBase> { trigger2 },
		};
		var group = new VisualStateGroup();
		group.States.Add(state1);
		group.States.Add(state2);

		VisualStateManager.SetVisualStateGroups(control, new List<VisualStateGroup> { group });

		trigger1.Set();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, control.Tag);

		trigger2.Set();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, control.Tag);

		trigger1.Unset();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(2, control.Tag);

		trigger2.Unset();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsNull(control.Tag);
	}

	[TestMethod]
	public async Task When_UsingStateTriggerAndStoryboard()
	{
		var control = new Control { Name = "control" };

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
			StateTriggers = new List<StateTriggerBase> { trigger1 },
		};
		var state2 = new VisualState
		{
			Name = "state2",
			Storyboard = storyboard2,
			StateTriggers = new List<StateTriggerBase> { trigger2 },
		};
		var group = new VisualStateGroup();
		group.States.Add(state1);
		group.States.Add(state2);

		VisualStateManager.SetVisualStateGroups(control, new List<VisualStateGroup> { group });

		Assert.AreEqual(0, completed1Count);
		Assert.AreEqual(0, completed2Count);

		await TestServices.WindowHelper.WaitForIdle();

		trigger1.Set();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, completed1Count);
		Assert.AreEqual(0, completed2Count);

		trigger2.Set();
		await TestServices.WindowHelper.WaitForIdle();

		// Documented precedence: state2 will be set only after trigger1 is unset.
		Assert.AreEqual(1, completed1Count);
		Assert.AreEqual(0, completed2Count);

		trigger1.Unset();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, completed1Count);
		Assert.AreEqual(1, completed2Count);

		trigger2.Unset();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, completed1Count);
		Assert.AreEqual(1, completed2Count);
	}

	[TestMethod]
	public void When_SetterAffectsSameProperty_Then_OriginalValueNotRestored()
	{
		var (control, _) = SetupVsmTest();

		var tags = new List<string> { (string)control.Tag };
		control.RegisterPropertyChangedCallback(Control.TagProperty, (_, __) => tags.Add((string)control.Tag));

		VisualStateManager.GoToState(control, "state1", true);
		VisualStateManager.GoToState(control, "state2", true);

		Assert.IsTrue(new[] { "initial", "state1", "state2" }.SequenceEqual(tags));
	}

	[TestMethod]
	public void When_NoSetter_Then_OriginalValueRestored()
	{
		var (control, group) = SetupVsmTest();
		group.States[1].Setters.Clear();

		var tags = new List<string> { (string)control.Tag };
		control.RegisterPropertyChangedCallback(Control.TagProperty, (_, __) => tags.Add((string)control.Tag));

		VisualStateManager.GoToState(control, "state1", true);
		VisualStateManager.GoToState(control, "state2", true);

		Assert.IsTrue(new[] { "initial", "state1", "initial" }.SequenceEqual(tags));
	}

	[TestMethod]
	public void When_GoToStateWhileTransitionning_Then_StopTransition()
	{
		var (control, group) = SetupVsmTest();
		var transition = MakeTransition(control, to: 1, frames: 5);
		group.Transitions.Add(transition);

		VisualStateManager.GoToState(control, "state1", true);
		Assert.AreEqual(Timeline.TimelineState.Active, transition.Storyboard.State);

		VisualStateManager.GoToState(control, "state2", true);
		Assert.AreEqual(Timeline.TimelineState.Stopped, transition.Storyboard.State);
	}

	[TestMethod]
	public void When_GoToStateWhileAnimating_Then_StopAnimation()
	{
		var (control, group) = SetupVsmTest();
		var animation = group.States[0].Storyboard = AnimateTag(control, "state1", 5);

		VisualStateManager.GoToState(control, "state1", true);
		Assert.AreEqual(Timeline.TimelineState.Active, animation.State);

		VisualStateManager.GoToState(control, "state2", true);
		Assert.AreEqual(Timeline.TimelineState.Stopped, animation.State);
	}

	[TestMethod]
	public void When_TransitionAndSetter_Then_SetterAppliedAfter()
	{
		var (control, group) = SetupVsmTest();
		group.Transitions.Add(MakeTransition(control, from: 1, to: 2, frames: 0));

		var tags = new List<string> { (string)control.Tag };
		control.RegisterPropertyChangedCallback(Control.TagProperty, (_, __) => tags.Add((string)control.Tag));

		VisualStateManager.GoToState(control, "state1", true);
		VisualStateManager.GoToState(control, "state2", true);

		Assert.IsTrue(new[] { "initial", "state1", "transition_from_state1_to_state2_frame_0", "state2" }.SequenceEqual(tags));
	}

	[TestMethod]
	public void When_TransitionAndSetter_InCompatibilityMode_Then_SetterAppliedBefore()
	{
		try
		{
			FeatureConfiguration.VisualState.ApplySettersBeforeTransition = true;

			var (control, group) = SetupVsmTest();
			group.Transitions.Add(MakeTransition(control, from: 1, to: 2, frames: 0));

			var tags = new List<string> { (string)control.Tag };
			control.RegisterPropertyChangedCallback(Control.TagProperty, (_, __) => tags.Add((string)control.Tag));

			VisualStateManager.GoToState(control, "state1", true);
			VisualStateManager.GoToState(control, "state2", true);

			Assert.IsTrue(new[] { "initial", "state1", "state2", "transition_from_state1_to_state2_frame_0" }.SequenceEqual(tags));
		}
		finally
		{
			FeatureConfiguration.VisualState.ApplySettersBeforeTransition = false;
		}
	}

	[TestMethod]
	public void When_CustomManager_Then_UseIt()
	{
		var (control, _) = SetupVsmTest();
		var vsm = new CustomManager();
		VisualStateManager.SetCustomVisualStateManager((FrameworkElement)control.TemplatedRoot, vsm);

		VisualStateManager.GoToState(control, "state1", true);
		VisualStateManager.GoToState(control, "state2", true);

		Assert.IsTrue(new[] { "state1", "state2" }.SequenceEqual(vsm.States));
	}

	private static (Control control, VisualStateGroup states) SetupVsmTest()
	{
		var control = new Control
		{
			Name = "control",
			Tag = "initial",
			Template = new ControlTemplate(() => new Grid()),
		};
		var group = new VisualStateGroup { States = { State(1), State(2) } };

		control.ApplyTemplate();

		VisualStateManager.SetVisualStateGroups((FrameworkElement)control.TemplatedRoot, new List<VisualStateGroup> { group });

		return (control, group);
	}

	private static VisualState State(int id)
		=> new()
		{
			Name = "state" + id,
			Setters = { new Setter(new TargetPropertyPath("control", "Tag"), "state" + id) },
		};

	private static VisualTransition MakeTransition(Control control, int? @from = null, int? to = null, params int[] frames)
	{
		var transition = new VisualTransition();
		var animationName = "transition";

		if (@from is int f)
		{
			transition.From = "state" + f;
			animationName += "_from_state" + f;
		}

		if (to is int t)
		{
			transition.To = "state" + t;
			animationName += "_to_state" + t;
		}

		transition.Storyboard = AnimateTag(control, animationName, frames);

		return transition;
	}

	private static Storyboard AnimateTag(Control target, string name, params int[] frames)
	{
		var anim = new ObjectAnimationUsingKeyFrames();
		Storyboard.SetTarget(anim, target);
		Storyboard.SetTargetProperty(anim, "Tag");
		foreach (var frame in frames)
		{
			anim.KeyFrames.Add(new DiscreteObjectKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(frame)),
				Value = $"{name}_frame_{frame}",
			});
		}

		return new Storyboard { Children = { anim } };
	}

	private class CustomStateTrigger : StateTrigger
	{
		internal CustomStateTrigger(bool initialState = true)
		{
			IsActive = initialState;
		}

		internal void Set() => IsActive = true;

		internal void Unset() => IsActive = false;
	}

	private class CustomManager : VisualStateManager
	{
		public List<string> States { get; } = new();

		protected override bool GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup @group, VisualState state, bool useTransitions)
		{
			States.Add(stateName);
			return base.GoToStateCore(control, templateRoot, stateName, @group, state, useTransitions);
		}
	}
}
