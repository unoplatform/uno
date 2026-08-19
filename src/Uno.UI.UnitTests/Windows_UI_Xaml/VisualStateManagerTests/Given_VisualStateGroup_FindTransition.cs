using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;

namespace Uno.UI.Tests.Windows_UI_Xaml.VisualStateManagerTests;

[TestClass]
public class Given_VisualStateGroup_FindTransition
{
	// Regression tests for https://github.com/unoplatform/uno/pull/17436:
	// VisualStateGroup.FindTransition used to ignore VisualTransitions that
	// didn't have both From and To set, even though only one of them is
	// required to match.

	private static VisualTransition GetAppliedTransition(VisualStateGroup group)
	{
		var currentField = typeof(VisualStateGroup).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsNotNull(currentField, "Unable to find VisualStateGroup._current field (implementation changed).");
		var current = currentField.GetValue(group);
		Assert.IsNotNull(current, "VisualStateGroup._current returned null unexpectedly.");
		// The tuple's element names (state, transition) are compiler sugar and are not
		// preserved as actual reflection field names; the underlying ValueTuple<T1, T2>
		// fields are Item1/Item2.
		var transitionField = current.GetType().GetField("Item2");
		Assert.IsNotNull(transitionField, "Unable to find ValueTuple.Item2 field (implementation changed).");
		return (VisualTransition)transitionField.GetValue(current);
	}

	private static Control CreateControlWithGroup(VisualStateGroup group)
	{
		var control = new TransitionsTestControl
		{
			Template = new ControlTemplate(() => new Border { Name = "root" }
				.Apply(root => VisualStateManager.SetVisualStateGroups(root, new List<VisualStateGroup> { group })))
		};

		control.ApplyTemplate();

		return control;
	}

	[TestMethod]
	public void When_Perfect_Match_Exists_It_Takes_Priority_Over_Partial_Matches()
	{
		var group = new VisualStateGroup();
		group.States.Add(new VisualState { Name = "A" });
		group.States.Add(new VisualState { Name = "B" });

		var fromOnly = new VisualTransition { From = "A" };
		var toOnly = new VisualTransition { To = "B" };
		var perfectMatch = new VisualTransition { From = "A", To = "B" };
		group.Transitions.Add(fromOnly);
		group.Transitions.Add(toOnly);
		group.Transitions.Add(perfectMatch);

		var control = CreateControlWithGroup(group);

		Assert.IsTrue(VisualStateManager.GoToState(control, "A", false));
		Assert.IsTrue(VisualStateManager.GoToState(control, "B", true));

		Assert.AreSame(perfectMatch, GetAppliedTransition(group));
	}

	[TestMethod]
	public void When_No_Perfect_Match_A_From_Only_Transition_Is_Used()
	{
		var group = new VisualStateGroup();
		group.States.Add(new VisualState { Name = "A" });
		group.States.Add(new VisualState { Name = "B" });

		var fromOnly = new VisualTransition { From = "A" };
		group.Transitions.Add(fromOnly);

		var control = CreateControlWithGroup(group);

		Assert.IsTrue(VisualStateManager.GoToState(control, "A", false));
		Assert.IsTrue(VisualStateManager.GoToState(control, "B", true));

		Assert.AreSame(fromOnly, GetAppliedTransition(group));
	}

	[TestMethod]
	public void When_No_Perfect_Match_A_From_Only_Transition_Takes_Priority_Over_A_To_Only_Transition()
	{
		var group = new VisualStateGroup();
		group.States.Add(new VisualState { Name = "A" });
		group.States.Add(new VisualState { Name = "B" });

		var fromOnly = new VisualTransition { From = "A" };
		var toOnly = new VisualTransition { To = "B" };
		group.Transitions.Add(toOnly);
		group.Transitions.Add(fromOnly);

		var control = CreateControlWithGroup(group);

		Assert.IsTrue(VisualStateManager.GoToState(control, "A", false));
		Assert.IsTrue(VisualStateManager.GoToState(control, "B", true));

		Assert.AreSame(fromOnly, GetAppliedTransition(group));
	}

	[TestMethod]
	public void When_No_Perfect_Or_From_Only_Match_A_To_Only_Transition_Is_Used()
	{
		var group = new VisualStateGroup();
		group.States.Add(new VisualState { Name = "A" });
		group.States.Add(new VisualState { Name = "B" });

		var toOnly = new VisualTransition { To = "B" };
		group.Transitions.Add(toOnly);

		var control = CreateControlWithGroup(group);

		Assert.IsTrue(VisualStateManager.GoToState(control, "A", false));
		Assert.IsTrue(VisualStateManager.GoToState(control, "B", true));

		Assert.AreSame(toOnly, GetAppliedTransition(group));
	}

	[TestMethod]
	public void When_No_Transition_Matches_None_Is_Applied()
	{
		var group = new VisualStateGroup();
		group.States.Add(new VisualState { Name = "A" });
		group.States.Add(new VisualState { Name = "B" });

		group.Transitions.Add(new VisualTransition { From = "X", To = "Y" });

		var control = CreateControlWithGroup(group);

		Assert.IsTrue(VisualStateManager.GoToState(control, "A", false));
		Assert.IsTrue(VisualStateManager.GoToState(control, "B", true));

		Assert.IsNull(GetAppliedTransition(group));
	}
}

public class TransitionsTestControl : ContentControl
{
}
