// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/1.4.2

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Visitor interface for processing animation commands.
/// </summary>
internal interface IListViewBaseItemAnimationCommandVisitor
{
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_Pressed command);
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_ReorderHint command);
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_DragDrop command);
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_MultiSelect command);
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_IndicatorSelect command);
	void VisitAnimationCommand(ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility command);
}

/// <summary>
/// Base class for animation commands that the chrome enqueues and the presenter processes.
/// </summary>
internal abstract class ListViewBaseItemAnimationCommand
{
	/// <summary>
	/// If true, this command starts an animation. If false, it stops an animation.
	/// </summary>
	public bool IsStarting { get; set; }

	/// <summary>
	/// If true, skip the animation and go directly to the steady state.
	/// </summary>
	public bool SteadyStateOnly { get; set; }

	protected ListViewBaseItemAnimationCommand(bool isStarting, bool steadyStateOnly)
	{
		IsStarting = isStarting;
		SteadyStateOnly = steadyStateOnly;
	}

	/// <summary>
	/// Gets the priority of this command. Higher priorities take precedence.
	/// </summary>
	public abstract int GetPriority();

	/// <summary>
	/// Accepts a visitor for double-dispatch.
	/// </summary>
	public abstract void Accept(IListViewBaseItemAnimationCommandVisitor visitor);

	/// <summary>
	/// Creates a clone of this command.
	/// </summary>
	public abstract ListViewBaseItemAnimationCommand Clone();
}

/// <summary>
/// Animation command for pointer pressed/released animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_Pressed : ListViewBaseItemAnimationCommand
{
	private const int Priority = 0;

	public bool IsPressed { get; }
	public WeakReference<UIElement>? Target { get; }

	public ListViewBaseItemAnimationCommand_Pressed(
		bool isPressed,
		WeakReference<UIElement>? target,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		IsPressed = isPressed;
		Target = target;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_Pressed(IsPressed, Target, IsStarting, SteadyStateOnly);
}

/// <summary>
/// Animation command for reorder hint animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_ReorderHint : ListViewBaseItemAnimationCommand
{
	private const int Priority = 1;

	public float OffsetX { get; }
	public float OffsetY { get; }
	public WeakReference<UIElement>? Target { get; }

	public ListViewBaseItemAnimationCommand_ReorderHint(
		float offsetX,
		float offsetY,
		WeakReference<UIElement>? target,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
		Target = target;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_ReorderHint(OffsetX, OffsetY, Target, IsStarting, SteadyStateOnly);
}

/// <summary>
/// Animation command for drag/drop animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_DragDrop : ListViewBaseItemAnimationCommand
{
	private const int Priority = 2;

	public DragStates DragState { get; }
	public WeakReference<UIElement>? ContentTarget { get; }
	public WeakReference<UIElement>? ChromeTarget { get; }
	public WeakReference<UIElement>? SecondaryTarget { get; }
	public WeakReference<ContentControl>? ParentTarget { get; }

	public ListViewBaseItemAnimationCommand_DragDrop(
		DragStates dragState,
		WeakReference<UIElement>? contentTarget,
		WeakReference<UIElement>? chromeTarget,
		WeakReference<UIElement>? secondaryTarget,
		WeakReference<ContentControl>? parentTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		DragState = dragState;
		ContentTarget = contentTarget;
		ChromeTarget = chromeTarget;
		SecondaryTarget = secondaryTarget;
		ParentTarget = parentTarget;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_DragDrop(DragState, ContentTarget, ChromeTarget, SecondaryTarget, ParentTarget, IsStarting, SteadyStateOnly);
}

/// <summary>
/// Animation command for multi-select checkbox entrance/exit animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_MultiSelect : ListViewBaseItemAnimationCommand
{
	private const int Priority = 3;

	public bool IsEnabled { get; }
	public bool IsInline { get; }
	public WeakReference<UIElement>? CheckBoxTarget { get; }
	public WeakReference<UIElement>? ContentTarget { get; }

	public ListViewBaseItemAnimationCommand_MultiSelect(
		bool isEnabled,
		bool isInline,
		WeakReference<UIElement>? checkBoxTarget,
		WeakReference<UIElement>? contentTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		IsEnabled = isEnabled;
		IsInline = isInline;
		CheckBoxTarget = checkBoxTarget;
		ContentTarget = contentTarget;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_MultiSelect(IsEnabled, IsInline, CheckBoxTarget, ContentTarget, IsStarting, SteadyStateOnly);
}

/// <summary>
/// Animation command for selection indicator slide animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_IndicatorSelect : ListViewBaseItemAnimationCommand
{
	private const int Priority = 4;

	public bool IsSelected { get; }
	public WeakReference<UIElement>? IndicatorTarget { get; }
	public WeakReference<UIElement>? ContentTarget { get; }

	public ListViewBaseItemAnimationCommand_IndicatorSelect(
		bool isSelected,
		WeakReference<UIElement>? indicatorTarget,
		WeakReference<UIElement>? contentTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		IsSelected = isSelected;
		IndicatorTarget = indicatorTarget;
		ContentTarget = contentTarget;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_IndicatorSelect(IsSelected, IndicatorTarget, ContentTarget, IsStarting, SteadyStateOnly);
}

/// <summary>
/// Animation command for selection indicator visibility scale/fade animations.
/// </summary>
internal sealed class ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility : ListViewBaseItemAnimationCommand
{
	private const int Priority = 5;

	public bool IsSelected { get; }
	public float FromScale { get; }
	public WeakReference<UIElement>? IndicatorTarget { get; }

	public ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(
		bool isSelected,
		float fromScale,
		WeakReference<UIElement>? indicatorTarget,
		bool isStarting,
		bool steadyStateOnly)
		: base(isStarting, steadyStateOnly)
	{
		IsSelected = isSelected;
		FromScale = fromScale;
		IndicatorTarget = indicatorTarget;
	}

	public override int GetPriority() => Priority;

	public override void Accept(IListViewBaseItemAnimationCommandVisitor visitor)
		=> visitor.VisitAnimationCommand(this);

	public override ListViewBaseItemAnimationCommand Clone()
		=> new ListViewBaseItemAnimationCommand_SelectionIndicatorVisibility(IsSelected, FromScale, IndicatorTarget, IsStarting, SteadyStateOnly);
}
