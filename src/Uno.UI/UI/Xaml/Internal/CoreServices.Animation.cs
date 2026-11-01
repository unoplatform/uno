// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference xcpcore.cpp (deferred animation operation queue), tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace Uno.UI.Xaml.Core;

internal partial class CoreServices
{
	private readonly List<DeferredAnimationOperation> m_deferredAnimationOperationQueue = new();

	// Perform deferred operation for each entry in  m_deferredAnimationOperationQueue
	internal void FlushDeferredAnimationOperationQueue(bool doDeferredOperation = true)
	{
		while (m_deferredAnimationOperationQueue.Count > 0)
		{
			var deferredAnimationOperation = m_deferredAnimationOperationQueue[0];
			m_deferredAnimationOperationQueue.RemoveAt(0);

			if (doDeferredOperation)
			{
				// This will result in a call out to user code, which might fail (either
				// within the framework, or in the user code). If that's the case, and
				// assuming it didn't result in a failfast, we'll abort flushing the queue
				// for now, but leave it untouched and pick it up again at some point in
				// the future.
				deferredAnimationOperation.Execute();
			}
		}

		// TODO Uno: SetHasDeferredAnimationOperationsEventSignaledStatus / SetDeferredAnimationOperationsCompleteEvent are test
		// synchronization events; the runtime tests poll HasDeferredAnimationOperations instead.
	}

	/// <summary>
	/// Whether animation operations are waiting for the next tick (WinUI's HasDeferredAnimationOperations test event).
	/// </summary>
	internal bool HasDeferredAnimationOperations => m_deferredAnimationOperationQueue.Count > 0;

	// Queue an animation operation to be performed at a later time
	internal void EnqueueDeferredAnimationOperation(DeferredAnimationOperation deferredAnimationOperation)
		=> m_deferredAnimationOperationQueue.Add(deferredAnimationOperation);

	// Remove from the queue any animation operations that match the set of DO/DP pairs
	internal void RemoveMatchingDeferredAnimationOperations(DependencyObject targetObject, DependencyProperty targetProperty)
		=> m_deferredAnimationOperationQueue.RemoveAll(operation =>
			ReferenceEquals(operation.Target().Object, targetObject) && operation.Target().Property == targetProperty);
}
