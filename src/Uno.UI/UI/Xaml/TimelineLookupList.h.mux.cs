// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference TimelineLookupList.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml;

//  Helper class used to represent a pseudo-keyed set of timelines. This class contains
//  a list of timelines which is created by flattening one or more storyboards. (Flatten because
//  a storyboard can contain other storyboards...)
//
//  The list nodes are "keyed" on the DO and DP which their timeline affects.
internal sealed class TimelineLookupList : IEnumerable<TimelineLookupList.Node>
{
	internal sealed class Node
	{
		internal Timeline? m_timeline;
		internal DependencyObject? m_do;
		internal DependencyProperty? m_property;
#if !HAS_UNO
		// TODO Uno: NOT PORTED - CDynamicTimeline (the theme animations aren't generated from the theme).
		CDynamicTimeline* m_dynamicTimeline = nullptr;
#endif
		internal ResolvedVisualStateSetter? m_setter;

		internal bool Initialize(Timeline timeline)
		{
			m_timeline = timeline;
			m_setter = null;

#if HAS_UNO
			// TODO Uno: WinUI fails the state change when a target can't be resolved; Uno still resolves some targets
			// lazily (x:Load, deferred elements), so such a timeline is left out.
			try
			{
				timeline.GetAnimationTargets(out m_do, out m_property);
			}
			catch (InvalidOperationException e)
			{
				if (timeline.Log().IsEnabled(LogLevel.Debug))
				{
					timeline.Log().Debug($"Ignoring a timeline of a visual transition without target: {e.Message}");
				}

				return false;
			}

			return m_do is not null && m_property is not null;
#endif
		}

		internal Node()
		{
		}

		internal Node(ResolvedVisualStateSetter setter)
		{
			m_setter = setter;
			m_timeline = null;
			m_do = setter.TargetObject;
			m_property = setter.TargetProperty;
		}

		internal bool Matches(Node other)
			=> ReferenceEquals(other.m_do, m_do) && other.m_property == m_property;
	}

	internal TimelineLookupList(IEnumerable<Storyboard?> storyboards) => FlattenStoryboardList(storyboards);

	internal TimelineLookupList(Storyboard? storyboard) => FlattenStoryboard(storyboard);

	internal TimelineLookupList(IEnumerable<ResolvedVisualStateSetter> setters)
	{
		foreach (var setter in setters)
		{
			m_list.Add(new Node(setter));
		}
	}

	private void FlattenTimeline(Timeline? timeline)
	{
		if (timeline is not null)
		{
			// If the timeline is actually a Storyboard we could have
			// nested Timelines.
			if (timeline is Storyboard storyboard)
			{
				FlattenStoryboard(storyboard);
			}
#if HAS_UNO
			// TODO Uno: The unported theme animations (CDynamicTimeline) have no children to flatten.
			else if (timeline.IsAnimation())
#else
			else
#endif
			{
				var node = new Node();
				if (node.Initialize(timeline))
				{
					m_list.Add(node);
				}
			}
		}
	}

	private void FlattenStoryboard(Storyboard? pStoryboard)
	{
		if (pStoryboard?.Children is { } pChildren)
		{
			foreach (var timeline in pChildren)
			{
				FlattenTimeline(timeline);
			}
		}
	}

	private void FlattenStoryboardList(IEnumerable<Storyboard?> storyboards)
	{
		foreach (var storyboard in storyboards)
		{
			FlattenStoryboard(storyboard);
		}
	}

	// Remove nodes in this list which affect the same DO/DP pairs as
	// the keyed value from another list
	internal void RemoveMatchingNodes(Node keyedValue) => m_list.RemoveAll(node => node.Matches(keyedValue));

	public IEnumerator<Node> GetEnumerator() => m_list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private readonly List<Node> m_list = new();
}
