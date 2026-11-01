// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference timemgr.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TimeManager
{
	internal bool IsRunning() => m_isLoaded;

	internal Timeline? GetRootTimeline() => m_pRootTimeline;

	internal bool HasActiveTimelines() => m_pTimelineListHead != null;

#if !HAS_UNO
	// TODO Uno: Independent (composition thread) animations are not supported, every animation ticks on the UI thread.
	// bool HaveIndependentTimelinesChanged() const { return m_independentTimelinesChanged; }
#endif

	internal void UnlockTime()
	{
		global::System.Diagnostics.Debug.Assert(m_lockTimeToZero);
		m_unlockRequested = true;
		m_lockTimeToZero = false;
#if !HAS_UNO
		m_independentTimelinesChanged = TRUE; // Make sure we commit DComp animations
#endif
	}

	internal bool IsLocked() => m_lockTimeToZero;

	private sealed class TimelineListNode
	{
		public Timeline? m_pTimeline;
		public TimelineListNode? m_pNextNoRef;
		public TimelineListNode? m_pPreviousNoRef;
	}

	private bool m_isLoaded;
	private bool m_lockTimeToZero;
	private bool m_unlockRequested;
#if !HAS_UNO
	private bool m_processIATargets;
	private bool m_independentTimelinesChanged;
	private bool m_hadActiveFiniteAnimations;
#endif

	private double m_rTimeStarted;
	private double m_rLastTickTime;

	private TimelineListNode? m_pTimelineListHead;
	private TimelineListNode? m_pSnappedTimelineHeadNoRef;
	private TimelineListNode? m_pTimelinePreviousHead;
	private Timeline? m_pRootTimeline;

#if HAS_UNO
	// TODO Uno specific: WinUI keys a hash table by (weakref, property index) and prunes expired entries on every tick.
	// A ConditionalWeakTable drops an entry with its target, which gives the same lifetime.
	private readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, Timeline>> m_hashTable = new();
#endif

#if !HAS_UNO
	// TODO Uno: IA counters, the WUC animation tracking and the target DO list only serve composition animations.
	// std::array<IATargetCountMap, 2> m_IACounters;
	// bool m_IACountersSwapState;
	// std::unordered_set<xref_ptr<CTranslateTransform>> m_translatesWithEndingAnimations;
	// std::unordered_map<xref_ptr<WUComp::ICompositionScopedBatch>, xref_ptr<CTimeline>> m_activeWUCAnimations;
	// std::vector<xref_ptr<CDependencyObject>> m_targetDOs;
#endif

	private double m_clockOverride;

#if !HAS_UNO
	// TODO Uno: The SlowDownAnimations and AnimationFailFast runtime features are not supported.
	// static bool s_slowDownAnimations;
	// static bool s_slowDownAnimationsLoaded;
	// static bool s_animationFailFast;
	// static bool s_animationFailFastLoaded;
#endif
}
