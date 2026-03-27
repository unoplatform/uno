// MUX Reference ItemCollectionTransitionProvider.h + ItemCollectionTransitionProvider.cpp
//             + ItemCollectionTransitionProvider.properties.h + ItemCollectionTransitionProvider.properties.cpp,
//             tag winui3/release/1.6-stable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemCollectionTransitionProvider
{
	// Current batch identifier; incremented each time CompositionTarget.Rendering starts a new batch.
	private uint _transitionsBatch;

	// Maps each keep-alive DispatcherTimer to the batch it is guarding.
	private readonly Dictionary<DispatcherTimer, uint> _keepAliveTimersMap = new();

	// All transitions queued in a given batch (animated + non-animated).
	private readonly Dictionary<uint, List<ItemCollectionTransition>> _transitionsMap = new();

	// Only the transitions in a given batch that will be animated.
	private readonly Dictionary<uint, List<ItemCollectionTransition>> _transitionsWithAnimationsMap = new();

	// Revoker for CompositionTarget.Rendering subscription.
	private readonly SerialDisposable _renderingRevoker = new();

	/// <summary>
	/// Occurs when the ItemCollectionTransitionProgress.Complete method is called to indicate that a transition on a specific UIElement has completed.
	/// </summary>
	public event TypedEventHandler<ItemCollectionTransitionProvider, ItemCollectionTransitionCompletedEventArgs> TransitionCompleted;

	/// <summary>
	/// Initializes a new instance of the ItemCollectionTransitionProvider class.
	/// </summary>
	public ItemCollectionTransitionProvider()
	{
	}

	// ─── IItemCollectionTransitionProvider ───────────────────────────────────

	/// <summary>
	/// Prepares a transition animation to be started by a call to StartTransitions.
	/// </summary>
	/// <param name="transition">The transition to be animated.</param>
	public void QueueTransition(ItemCollectionTransition transition)
	{
		// When usesNewTransitionsBatch remains false, the transition is added to the current batch.
		bool usesNewTransitionsBatch = false;

		if (_renderingRevoker.Disposable is null)
		{
			// This marks the beginning of a new batch.
			usesNewTransitionsBatch = true;
			_transitionsBatch++;
			CompositionTarget.Rendering += OnRendering;
			_renderingRevoker.Disposable = new DisposableAction(() => CompositionTarget.Rendering -= OnRendering);
		}

		// Animate only when animations are globally enabled and the provider opts-in for this transition.
		if (SharedHelpers.IsAnimationsEnabled() && ShouldAnimate(transition))
		{
			if (usesNewTransitionsBatch)
			{
				_transitionsWithAnimationsMap[_transitionsBatch] = new List<ItemCollectionTransition>();
			}

			_transitionsWithAnimationsMap[_transitionsBatch].Add(transition);
		}

		// Even non-animated transitions must raise TransitionCompleted from the Rendering handler
		// to preserve VirtualizationInfo ordering.
		if (usesNewTransitionsBatch)
		{
			_transitionsMap[_transitionsBatch] = new List<ItemCollectionTransition>();
		}

		_transitionsMap[_transitionsBatch].Add(transition);
	}

	/// <summary>
	/// When overridden in a derived class, determines whether a specific transition should be animated.
	/// </summary>
	/// <param name="transition">The transition to evaluate.</param>
	/// <returns><c>true</c> if the transition should be animated; otherwise, <c>false</c>.</returns>
	public bool ShouldAnimate(ItemCollectionTransition transition)
	{
		return ShouldAnimateCore(transition);
	}

	// ─── IItemCollectionTransitionProviderOverrides ───────────────────────────

	/// <summary>
	/// Starts the queued transition animations.
	/// </summary>
	/// <param name="transitions">The collection of transitions for which to start animations.</param>
	protected virtual void StartTransitions(IList<ItemCollectionTransition> transitions)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// When overridden in a derived class, determines whether a specific transition should be animated.
	/// </summary>
	/// <param name="transition">The transition to evaluate.</param>
	/// <returns><c>true</c> if the transition should be animated; otherwise, <c>false</c>.</returns>
	protected virtual bool ShouldAnimateCore(ItemCollectionTransition transition)
	{
		throw new NotImplementedException();
	}

	// ─── Internal ────────────────────────────────────────────────────────────

	internal void NotifyTransitionCompleted(ItemCollectionTransition transition)
	{
		TransitionCompleted?.Invoke(this, new ItemCollectionTransitionCompletedEventArgs(transition));
	}

	private void CleanTransitionsBatch()
	{
		// Called when none of a batch's transitions required a timer to keep them alive.
		_transitionsWithAnimationsMap.Remove(_transitionsBatch);
		_transitionsMap.Remove(_transitionsBatch);
	}

	private void OnKeepAliveTimerTick(object sender, object args)
	{
		// Once the timer expires, all associated transitions are considered completed and can be released.
		if (sender is not DispatcherTimer keepAliveTimer)
		{
			return;
		}

		keepAliveTimer.Stop();

		if (!_keepAliveTimersMap.TryGetValue(keepAliveTimer, out var transitionsBatch))
		{
			return;
		}

		_transitionsWithAnimationsMap.Remove(transitionsBatch);
		_transitionsMap.Remove(transitionsBatch);
		_keepAliveTimersMap.Remove(keepAliveTimer);
	}

	private void OnRendering(object sender, object args)
	{
		_renderingRevoker.Disposable = null;

		bool keepAliveTimerRequired = false;

		try
		{
			if (_transitionsWithAnimationsMap.TryGetValue(_transitionsBatch, out var transitionsWithAnimations))
			{
				StartTransitions(transitionsWithAnimations);
			}

			// Automatically raise TransitionCompleted for transitions that were not animated,
			// so every queued transition always receives a corresponding TransitionCompleted event.
			if (_transitionsMap.TryGetValue(_transitionsBatch, out var transitions))
			{
				foreach (var transition in transitions)
				{
					if (transition.HasStarted)
					{
						keepAliveTimerRequired = true;
					}
					else
					{
						NotifyTransitionCompleted(transition);
					}
				}
			}
		}
		finally
		{
			if (keepAliveTimerRequired)
			{
				// At least one transition requires the keep-alive timer so its progress object
				// remains valid long enough for ItemCollectionTransitionProgress.Complete() to fire.
				StartNewKeepAliveTimer();
			}
			else
			{
				// No keep-alive needed; discard the whole batch now.
				CleanTransitionsBatch();
			}
		}
	}

	private void StartNewKeepAliveTimer()
	{
		// This timer keeps the batch's transition objects alive so that
		// ItemCollectionTransitionProgress.Complete() can still trigger NotifyTransitionCompleted,
		// which is required for TransitionManager.OnTransitionProviderTransitionCompleted to update
		// the ItemsRepeater's item ownership.
		var keepAliveTimer = new DispatcherTimer();
		keepAliveTimer.Interval = TimeSpan.FromSeconds(5);
		keepAliveTimer.Tick += OnKeepAliveTimerTick;
		keepAliveTimer.Start();

		_keepAliveTimersMap[keepAliveTimer] = _transitionsBatch;
	}

#if HAS_UNO
	// TODO Uno: Original C++ destructor stops all keep-alive timers.
	// Uno does not support cleanup via finalizers; consumers should call Dispose() or stop
	// using the provider when done.  The timers will be collected when the provider is GC'd.
	//
	// Original destructor logic (not executed):
	// foreach (var keepAliveTimer in _keepAliveTimersMap.Keys) { keepAliveTimer.Stop(); }
#endif
}
