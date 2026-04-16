// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProvider.cpp, commit 5f9e851133b3

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProvider
{
#if HAS_UNO
	// Uno: The original C++ destructor stops every keep-alive DispatcherTimer to avoid
	// use-after-free — C++/WinRT's `{ this, &Method }` handler binding stores a raw
	// `this` pointer and does not extend the provider's lifetime. In C#, `timer.Tick
	// += OnKeepAliveTimerTick` holds a strong delegate reference back to the provider,
	// so the provider stays alive (via Dispatcher -> DispatcherTimer -> Tick delegate)
	// until each timer fires, at which point OnKeepAliveTimerTick stops the timer and
	// removes it from _keepAliveTimersMap. GC then collects the provider naturally.
	// No IDisposable, finalizer, or Unloaded hook is required.
#endif

	// #pragma region IItemCollectionTransitionProvider

	/// <summary>
	/// Prepares a transition animation to be started by a call to StartTransitions.
	/// </summary>
	/// <param name="transition">The transition to be animated.</param>
	public void QueueTransition(ItemCollectionTransition transition)
	{
		// When usesNewTransitionsBatch remains 'false', the provided transition is added to the current batch
		// identified by the current m_transitionsBatch value.
		bool usesNewTransitionsBatch = false;

		if (_renderingRevoker.Disposable is null)
		{
			// This marks the beginning of a new batch of transitions.
			usesNewTransitionsBatch = true;
			// The batch gets a new id.
			_transitionsBatch++;
			CompositionTarget.Rendering += OnRendering;
			_renderingRevoker.Disposable = Disposable.Create(() => CompositionTarget.Rendering -= OnRendering);
		}

		// We'll animate if animations are enabled and the transition provider has indicated we should animate this transition.
		if (SharedHelpers.IsAnimationsEnabled() && ShouldAnimate(transition))
		{
			if (usesNewTransitionsBatch)
			{
				// Allocating a new array of ItemCollectionTransition with animations for this new batch.
				_transitionsWithAnimationsMap[_transitionsBatch] = new List<ItemCollectionTransition>();
			}

			_transitionsWithAnimationsMap[_transitionsBatch].Add(transition);
		}

		// To ensure proper VirtualizationInfo ordering, we still need to raise TransitionCompleted in a
		// CompositionTarget.Rendering handler, even if we aren't going to animate anything for this transition.
		if (usesNewTransitionsBatch)
		{
			// Allocating a new array of ItemCollectionTransition for this new batch.
			_transitionsMap[_transitionsBatch] = new List<ItemCollectionTransition>();
		}

		_transitionsMap[_transitionsBatch].Add(transition);
	}

	/// <summary>
	/// Determines whether a specific transition should be animated.
	/// </summary>
	/// <param name="transition">The transition to evaluate.</param>
	/// <returns><c>true</c> if the transition should be animated; otherwise, <c>false</c>.</returns>
	public bool ShouldAnimate(ItemCollectionTransition transition) => ShouldAnimateCore(transition);

	// #pragma endregion

	// #pragma region IItemCollectionTransitionProviderOverrides

	/// <summary>
	/// When overridden in a derived class, starts the queued transition animations.
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

	// #pragma endregion

	internal void NotifyTransitionCompleted(ItemCollectionTransition transition)
	{
		TransitionCompleted?.Invoke(this, new ItemCollectionTransitionCompletedEventArgs(transition));
	}

	private void CleanTransitionsBatch()
	{
		// Called when none of a batch's transitions required a timer to keep them alive.
		// No completion notification is expected and all transitions can be released.
		_transitionsWithAnimationsMap.Remove(_transitionsBatch);
		_transitionsMap.Remove(_transitionsBatch);
	}

	private void OnKeepAliveTimerTick(object sender, object args)
	{
		// By the time this timer expires, all transitions associated with it are considered completed and they no longer need to be kept alive for
		// ItemCollectionTransitionProgress.Complete() to successfully trigger a ItemCollectionTransitionProvider.NotifyTransitionCompleted call.
		if (sender is not DispatcherTimer keepAliveTimer)
		{
			return;
		}

		// The timer is stopped, all its associated transitions are released and so is the timer itself.
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

			// We'll automatically raise TransitionCompleted on all of the transitions that were not actually animated
			// in order to guarantee that every transition queued receives a corresponding TransitionCompleted event.
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
				// At least one transition in the batch requires a new 'keep alive' timer.
				StartNewKeepAliveTimer();
			}
			else
			{
				// None of the transitions in the batch requires a 'keep alive' timer. Simply discard them all.
				CleanTransitionsBatch();
			}
		}
	}

	private void StartNewKeepAliveTimer()
	{
		// This timer delays the release of the batch's transitions so that ItemCollectionTransitionProgress.Complete()
		// which uses transition weak references is able to trigger an ItemCollectionTransitionProvider.NotifyTransitionCompleted
		// call for completion. This is important for TransitionManager.OnTransitionProviderTransitionCompleted to be able to
		// update the ItemsRepeater's items ownership.
		MUX_ASSERT(_transitionsMap[_transitionsBatch].Count > 0);

		var keepAliveTimer = new DispatcherTimer();
		keepAliveTimer.Interval = TimeSpan.FromSeconds(5);
		keepAliveTimer.Tick += OnKeepAliveTimerTick;
		keepAliveTimer.Start();

		_keepAliveTimersMap[keepAliveTimer] = _transitionsBatch;
	}
}
