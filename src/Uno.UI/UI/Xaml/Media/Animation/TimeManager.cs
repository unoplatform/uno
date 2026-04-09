// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/timemgr.cpp, dxaml/xcp/core/inc/timemgr.h

#if __SKIA__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Central animation timing coordinator that ticks all active timelines before layout,
	/// matching WinUI's frame cycle: TimeManager.Tick() → Layout → Render.
	///
	/// Ported from WinUI CTimeManager (dxaml/xcp/core/animation/timemgr.cpp).
	/// Skipped: DComp/WUC interop, independent animation tracking (IACounters/IATargets),
	/// clock locking (splash screen), SlowDownAnimations, DispatcherTimer handling.
	/// </summary>
	internal sealed class TimeManager
	{
		/// <summary>
		/// Singleton instance. In WinUI, CTimeManager is owned by CCoreServices.
		/// </summary>
		internal static TimeManager Instance { get; } = new();

		// --- Clock ---
		// MUX: m_pIClock (RefreshAlignedClock*), m_rTimeStarted, m_rLastTickTime
		private readonly Stopwatch _clock = new();
		private double _timeStarted;
		private double _lastTickTime;
		private bool _isStarted;

		// --- Timeline linked list ---
		// MUX: m_pTimelineListHead, m_pSnappedTimelineHeadNoRef, m_pTimelinePreviousHead
		// Doubly-linked list with head pointer. New timelines are inserted at head so
		// the two-pass tick (newTimelinesOnly) can process only newly-added entries.
		private TimelineListNode _head;
		private TimelineListNode _snappedHead;
		private TimelineListNode _previousHead;

		// --- Animation-to-property registry ---
		// MUX: m_hashTable (HashNodeList) with HashKey(weakref, KnownPropertyIndex)
		// Tracks which animation currently controls each (target, property) pair.
		// Used for animation handoff in CAnimation::ReadBaseValuesFromTargetOrHandoff.
		private readonly Dictionary<AnimationPropertyKey, WeakReference<Timeline>> _animationOnProperty = new();

		/// <summary>
		/// The global clock time of the last tick, in seconds.
		/// MUX: m_rLastTickTime
		/// </summary>
		internal double LastTickTime => _lastTickTime;

		/// <summary>
		/// Whether there are any active timelines registered.
		/// MUX: CTimeManager::HasActiveTimelines()
		/// </summary>
		internal bool HasActiveTimelines => _head is not null;

		/// <summary>
		/// Ticks all active timelines. Called from CoreServices.OnTick() before layout.
		///
		/// MUX: CTimeManager::Tick() (timemgr.cpp line 221)
		///
		/// The linked list is iterated from head to the previous-head marker. When
		/// <paramref name="newTimelinesOnly"/> is true, only timelines added since the
		/// last full tick are processed (those between head and the snapped previous-head).
		/// This enables animations started during layout to be picked up in a second pass.
		/// </summary>
		/// <param name="newTimelinesOnly">
		/// When true, only tick timelines added since the last full tick. Used for the
		/// second pass after layout. MUX: newTimelinesOnly parameter.
		/// </param>
		internal void Tick(bool newTimelinesOnly = false)
		{
			EnsureTimeStarted();

			if (_head is null)
			{
				return;
			}

			// Compute global clock time.
			// MUX: m_rLastTickTime = m_pIClock->GetLastTickTimeInSeconds() - m_rTimeStarted
			if (!newTimelinesOnly)
			{
				_lastTickTime = _clock.Elapsed.TotalSeconds - _timeStarted;
			}

			// Save the current head for two-pass tick boundary tracking.
			// MUX: m_pSnappedTimelineHeadNoRef = m_pTimelineListHead
			_snappedHead = _head;

			if (!newTimelinesOnly)
			{
				_previousHead = null;
			}

			// Build ComputeState params.
			// MUX: parentParams setup in CTimeManager::Tick()
			var parentParams = new ComputeStateParams()
			{
				HasTime = true,
				Time = _lastTickTime,
			};

			// Iterate the linked list from head to the previous-head marker.
			// MUX: while (pCurrNode != m_pTimelinePreviousHead && pCurrNode != nullptr)
			var current = _head;
			while (current is not null && current != _previousHead)
			{
				var next = current.Next; // Cache before potential removal.
				var timeline = current.Timeline;

				if (timeline is not null)
				{
					// MUX: IFC(pTimeline->ComputeState(parentParams, &hasNoExternalReferences))
					timeline.ComputeState(parentParams);

					// Remove timelines that are no longer in an active state.
					// MUX: if (hasNoExternalReferences || (!pTimeline->IsInActiveState() && ...))
					if (!timeline.IsInActiveState)
					{
						RemoveNode(current);
						timeline.OnRemoveFromTimeManager();
					}
				}

				current = next;
			}

			// Mark the boundary for the next two-pass tick.
			// MUX: m_pTimelinePreviousHead = m_pSnappedTimelineHeadNoRef
			_previousHead = _snappedHead;
			_snappedHead = null;

			// Clean up expired weak references in the animation-to-property registry.
			// MUX: hash table cleanup loop at end of Tick()
			CleanupExpiredAnimationRegistrations();
		}

		/// <summary>
		/// Registers a top-level timeline (Storyboard) with the TimeManager.
		/// Called from Storyboard.Begin().
		///
		/// MUX: CTimeManager::AddTimeline() (timemgr.cpp)
		/// New timelines are inserted at the head of the linked list so the two-pass
		/// tick can distinguish them from already-ticked timelines.
		/// </summary>
		internal void AddTimeline(Timeline timeline)
		{
			if (timeline is null)
			{
				return;
			}

			EnsureTimeStarted();

			// MUX: pTimeline->OnAddToTimeManager()
			timeline.OnAddToTimeManager();

			// MUX: InsertNodeAtHead(pNewNode, m_pTimelineListHead)
			var node = new TimelineListNode { Timeline = timeline };
			InsertNodeAtHead(node, _head);
			_head = node;
		}

		/// <summary>
		/// Removes a top-level timeline from the TimeManager.
		/// Called from Storyboard.Stop() or when an animation completes.
		///
		/// MUX: CTimeManager::RemoveTimeline() (timemgr.cpp)
		/// </summary>
		internal void RemoveTimeline(Timeline timeline)
		{
			if (timeline is null)
			{
				return;
			}

			var current = _head;
			while (current is not null)
			{
				if (current.Timeline == timeline)
				{
					RemoveNode(current);
					// MUX: pTimeline->SetTimingParent(NULL)
					// MUX: pTimeline->OnRemoveFromTimeManager()
					timeline.OnRemoveFromTimeManager();
					break;
				}

				current = current.Next;
			}
		}

		#region Animation-to-property registry

		/// <summary>
		/// Associates an animation with a specific (target, property) pair.
		/// MUX: CTimeManager::SetAnimationOnProperty()
		/// </summary>
		internal void SetAnimationOnProperty(
			DependencyObject target,
			string propertyPath,
			Timeline animation)
		{
			var key = new AnimationPropertyKey(target, propertyPath);
			_animationOnProperty[key] = new WeakReference<Timeline>(animation);
		}

		/// <summary>
		/// Clears the animation registration for a (target, property) pair.
		/// MUX: CTimeManager::ClearAnimationOnProperty()
		/// </summary>
		internal void ClearAnimationOnProperty(
			DependencyObject target,
			string propertyPath)
		{
			var key = new AnimationPropertyKey(target, propertyPath);
			_animationOnProperty.Remove(key);
		}

		/// <summary>
		/// Gets the animation currently registered for a (target, property) pair.
		/// Returns null if no animation is registered or the reference has expired.
		/// MUX: CTimeManager::GetAnimationOnProperty()
		/// </summary>
		internal Timeline GetAnimationOnProperty(
			DependencyObject target,
			string propertyPath)
		{
			var key = new AnimationPropertyKey(target, propertyPath);
			if (_animationOnProperty.TryGetValue(key, out var weakRef) &&
				weakRef.TryGetTarget(out var animation))
			{
				return animation;
			}

			return null;
		}

		private void CleanupExpiredAnimationRegistrations()
		{
			// MUX: cleanup loop at end of Tick() that removes expired weak refs
			List<AnimationPropertyKey> toRemove = null;
			foreach (var kvp in _animationOnProperty)
			{
				if (!kvp.Value.TryGetTarget(out _))
				{
					toRemove ??= new List<AnimationPropertyKey>();
					toRemove.Add(kvp.Key);
				}
			}

			if (toRemove is not null)
			{
				foreach (var key in toRemove)
				{
					_animationOnProperty.Remove(key);
				}
			}
		}

		#endregion

		#region Continuous ticking

		private bool _isTicking;

		/// <summary>
		/// Ensures the TimeManager is continuously ticking via CompositionTarget.Rendering.
		/// Called when there are active animations.
		///
		/// In WinUI, the TimeManager ticks from the frame scheduler (VSync-driven, separate
		/// from the dispatcher). In Uno, we use CompositionTarget.Rendering as the closest
		/// equivalent. This fires at VSync rate from the render pipeline, NOT from the
		/// dispatcher Normal queue, so it doesn't prevent WaitForIdle/RunIdleAsync.
		///
		/// After ticking animations, we call RequestAdditionalFrame() once to ensure layout
		/// picks up the new animated property values.
		/// </summary>
		internal void EnsureTicking()
		{
			if (!_isTicking)
			{
				_isTicking = true;
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
			}
		}

		private void StopTicking()
		{
			if (_isTicking)
			{
				_isTicking = false;
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
			}
		}

		/// <summary>
		/// VSync-driven tick callback. Ticks all animations and requests the next render
		/// frame to keep the loop alive. In WinUI, the frame scheduler fires continuously
		/// at VSync rate while there are active timelines. In Uno, we must explicitly request
		/// the next render frame because not all animated property changes trigger visual
		/// invalidation (e.g., Opacity changes on Skia don't call InvalidateRender).
		/// </summary>
		private void OnRendering(object sender, object e)
		{
			if (!HasActiveTimelines)
			{
				StopTicking();
				return;
			}

			// Tick all animations — compute values and apply to properties.
			Tick();

			// Request the next render frame to keep the VSync loop alive.
			// This is the render pipeline (not dispatcher Normal queue), so it
			// doesn't prevent WaitForIdle/RunIdleAsync from completing.
			RequestRenderFrame();
		}

		/// <summary>
		/// Requests the next render frame via CompositionTarget.RequestNewFrame.
		/// This ensures the VSync callback continues firing while animations are active.
		/// </summary>
		private static void RequestRenderFrame()
		{
			// Find any active CompositionTarget and request a frame.
			foreach (var window in Uno.UI.ApplicationHelper.WindowsInternal)
			{
				if (window.RootElement?.XamlRoot?.Content?.Visual.CompositionTarget
					is Microsoft.UI.Xaml.Media.CompositionTarget ct)
				{
					((Uno.UI.Composition.ICompositionTarget)ct).RequestNewFrame();
					break;
				}
			}
		}

		#endregion

		#region Private helpers

		/// <summary>
		/// Ensures the clock has been started.
		/// MUX: CTimeManager::EnsureTimeStarted()
		/// </summary>
		private void EnsureTimeStarted()
		{
			if (!_isStarted)
			{
				_isStarted = true;
				_clock.Start();
				_timeStarted = _clock.Elapsed.TotalSeconds;
			}
		}

		/// <summary>
		/// Inserts a new node at the head of the linked list.
		/// MUX: CTimeManager::InsertNodeAtHead()
		/// </summary>
		private static void InsertNodeAtHead(TimelineListNode newHead, TimelineListNode previousHead)
		{
			newHead.Next = previousHead;
			newHead.Previous = null;

			if (previousHead is not null)
			{
				previousHead.Previous = newHead;
			}
		}

		/// <summary>
		/// Removes a node from the doubly-linked list, adjusting all head markers.
		/// MUX: inline removal in CTimeManager::RemoveTimeline()
		/// </summary>
		private void RemoveNode(TimelineListNode node)
		{
			if (node.Previous is not null)
			{
				node.Previous.Next = node.Next;
			}

			if (node.Next is not null)
			{
				node.Next.Previous = node.Previous;
			}

			if (_head == node)
			{
				_head = node.Next;
			}

			if (_snappedHead == node)
			{
				_snappedHead = node.Next;
			}

			if (_previousHead == node)
			{
				_previousHead = node.Next;
			}

			node.Timeline = null;
			node.Next = null;
			node.Previous = null;
		}

		#endregion

		/// <summary>
		/// Doubly-linked list node for active timelines.
		/// MUX: CTimeManager::TimelineListNode (timemgr.h)
		/// </summary>
		private sealed class TimelineListNode
		{
			public Timeline Timeline;
			public TimelineListNode Next;
			public TimelineListNode Previous;
		}

		/// <summary>
		/// Hash key for the animation-to-property registry.
		/// MUX: CTimeManager::HashKey (timemgr.h)
		///
		/// Uses a weak reference to the target DependencyObject combined with the
		/// property path string to uniquely identify an animated property.
		/// </summary>
		private readonly struct AnimationPropertyKey : IEquatable<AnimationPropertyKey>
		{
			private readonly int _targetHashCode;
			private readonly WeakReference<DependencyObject> _targetRef;
			private readonly string _propertyPath;

			public AnimationPropertyKey(DependencyObject target, string propertyPath)
			{
				_targetHashCode = target?.GetHashCode() ?? 0;
				_targetRef = target is not null ? new WeakReference<DependencyObject>(target) : null;
				_propertyPath = propertyPath;
			}

			public bool Equals(AnimationPropertyKey other)
			{
				if (_targetHashCode != other._targetHashCode || _propertyPath != other._propertyPath)
				{
					return false;
				}

				// Resolve weak references to compare identity.
				DependencyObject thisTarget = null;
				DependencyObject otherTarget = null;
				var hasThis = _targetRef?.TryGetTarget(out thisTarget) ?? false;
				var hasOther = other._targetRef?.TryGetTarget(out otherTarget) ?? false;

				if (!hasThis && !hasOther)
				{
					return true;
				}

				if (!hasThis || !hasOther)
				{
					return false;
				}

				return ReferenceEquals(thisTarget, otherTarget);
			}

			public override bool Equals(object obj) => obj is AnimationPropertyKey other && Equals(other);

			public override int GetHashCode() => HashCode.Combine(_targetHashCode, _propertyPath);
		}
	}

	/// <summary>
	/// Internal clock state matching WinUI's DirectUI::ClockState.
	/// Extends the public ClockState (Active, Filling, Stopped) with NotStarted,
	/// which WinUI uses internally but doesn't expose publicly.
	/// MUX: DirectUI::ClockState
	/// </summary>
	internal enum InternalClockState
	{
		NotStarted,
		Active,
		Filling,
		Stopped,
	}

	/// <summary>
	/// Parameters passed through the timing hierarchy during ComputeState.
	/// MUX: ComputeStateParams (ComputeStateParams.h)
	/// </summary>
	internal struct ComputeStateParams
	{
		/// <summary>Whether the time value is valid.</summary>
		public bool HasTime;

		/// <summary>Global clock time in seconds.</summary>
		public double Time;

		/// <summary>Whether the parent timeline is paused.</summary>
		public bool IsPaused;

		/// <summary>Whether the parent timeline is reversed (for AutoReverse).</summary>
		public bool IsReversed;

		/// <summary>Combined speed ratio from all ancestors. MUX: speedRatio.</summary>
		public float SpeedRatio;

		public ComputeStateParams()
		{
			SpeedRatio = 1.0f;
		}
	}
}
#endif
