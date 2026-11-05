// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System;
using System.Collections.Generic;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class FlyoutBase
{
	private readonly record struct OnPlacementUpdatedSubscriber(WeakReference<DependencyObject> Target, Action<DependencyObject, MajorPlacementMode> Callback);

	private List<OnPlacementUpdatedSubscriber> m_placementUpdatedSubscribers;

	internal new virtual void Enter(DependencyObject pNamescopeOwner, EnterParams @params)
	{
	}

	internal new virtual void Leave(DependencyObject pNamescopeOwner, LeaveParams @params)
	{
	}

	internal void OnPlacementUpdated(MajorPlacementMode majorPlacementMode)
	{
		if (m_placementUpdatedSubscribers is null)
		{
			return;
		}

		for (var i = 0; i < m_placementUpdatedSubscribers.Count;)
		{
			var subscriber = m_placementUpdatedSubscribers[i];
			if (subscriber.Target.TryGetTarget(out var pObject))
			{
				subscriber.Callback(pObject, majorPlacementMode);
				++i;
			}
			else
			{
				m_placementUpdatedSubscribers.RemoveAt(i);
			}
		}
	}

	internal void AddPlacementUpdatedSubscriber(DependencyObject pTarget, Action<DependencyObject, MajorPlacementMode> callbackFunc)
		=> (m_placementUpdatedSubscribers ??= new()).Add(new(new(pTarget), callbackFunc));

	internal void RemovePlacementUpdatedSubscriber(DependencyObject pTarget)
	{
		var targetPos = m_placementUpdatedSubscribers?.FindIndex(s => s.Target.TryGetTarget(out var pObject) && ReferenceEquals(pObject, pTarget)) ?? -1;
		if (targetPos >= 0)
		{
			m_placementUpdatedSubscribers.RemoveAt(targetPos);
		}
	}
}
