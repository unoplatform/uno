// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicator.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Private.Controls;

partial class SortIndicator
{
	private const string s_NoSortStateName = "NoSort";
	private const string s_AscendingStateName = "Ascending";
	private const string s_DescendingStateName = "Descending";

	public SortIndicator()
	{
		// __RP_Marker_ClassById(RuntimeProfiler.ProfId_SortIndicator);

		this.SetTabularDefaultStyleKey();
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		UpdateVisualState(false /* useTransitions */);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new SortIndicatorAutomationPeer(this);

	private void OnDirectionPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualState(true /* useTransitions */);
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		var direction = Direction;

		string directionStateName = string.Empty;
		switch (direction)
		{
			case SortIndicatorDirection.Ascending:
				directionStateName = s_AscendingStateName;
				break;
			case SortIndicatorDirection.Descending:
				directionStateName = s_DescendingStateName;
				break;
			case SortIndicatorDirection.None:
			default:
				directionStateName = s_NoSortStateName;
				break;
		}

		VisualStateManager.GoToState(this, directionStateName, useTransitions);
	}
}
