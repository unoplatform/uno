#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using DirectUI;
using CalendarViewDayItemChangingEventSourceType = Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Controls.CalendarView, Windows.UI.Xaml.Controls.CalendarViewDayItemChangingEventArgs>;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewGeneratorMonthViewHost : ITreeBuilder
	{
		// most codes are copied from ListViewBase_Partial_containerPhase.cpp
		// ListViewBase CCC event is restricted with ListViewBase, and it considers about UIPlaceHolder
		// the CalendarView version removes UIPlaceHolder and handles blackout state.
		// Other logicals are still same as ListViewBase.

		internal override void SetupContainerContentChangingAfterPrepare(
			DependencyObject pContainer,
			object pItem,
			int itemIndex,
			Size measureSize)
		{
			// this is being called by modern panels after the prepare has occurred.
			// we will setup information that we will need during the lifetime of this container

			CalendarViewDayItem spContainer;
			CalendarViewDayItemChangingEventArgs? spArgsConcrete = default;

			// raw pointer, since we're not a refcounted object
			VirtualizationInformation? pVirtualizationInformation = null;

			spContainer = (CalendarViewDayItem)pContainer;

			if (spContainer is { })
			{
				// is this a new style container? We can know by looking at the virtualizationInformation struct which is
				// a ModernCollectionBase concept
				CalendarViewDayItemChangingEventArgs spArgs;

				pVirtualizationInformation = (spContainer as UIElement).GetVirtualizationInformation();

				global::System.Diagnostics.Debug.Assert(pVirtualizationInformation is { });

				spArgs = (spContainer as CalendarViewDayItem).GetBuildTreeArgs();
				spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;
			}

			// Uno only null-ref fix:
			if (pVirtualizationInformation is null || spArgsConcrete is null)
			{
				return;
			}

			// store the size we would measure with
			pVirtualizationInformation.MeasureSize = measureSize;

			// initialize values in the args
			spArgsConcrete.WantsCallBack = false; // let them explicitly call-out if they want it

			spArgsConcrete.Item = spContainer; // there is now a hard ref
			spArgsConcrete.InRecycleQueue = false;
			spArgsConcrete.Phase = 0;

			// raise the event. This is the synchronous version. we will raise it 'async' as well when we have time
			// but we guarantee calling it after prepare

			CalendarViewDayItemChangingEventSourceType? pEventSource = null;

			Owner.GetCalendarViewDayItemChangingEventSourceNoRef(out pEventSource);

			if (Owner.ShouldRaiseEvent(pEventSource)) // app code hooks the event
			{
				// force measure. This will be no-op since content has not been set/changed
				// but we need it for the contenttemplateroot
				(spContainer as CalendarViewDayItem)?.Measure(measureSize);

				pEventSource.Invoke(Owner, spArgsConcrete);
			}

			RegisterWorkFromCICArgs(spArgsConcrete);

		}

#if false
		private void RegisterWorkForContainer(
			UIElement pContainer)
		{
			CalendarViewDayItemChangingEventArgs spArgs;
			CalendarViewDayItem spContainer;

			spContainer = (CalendarViewDayItem)pContainer;
			spArgs = (spContainer as CalendarViewDayItem).GetBuildTreeArgs();

			RegisterWorkFromCICArgs(spArgs);
		}
#endif

		private void RemoveToBeClearedContainer(CalendarViewDayItem pContainer)
		{
			// we might have been inserted into the list for deferred clear container calls.
			// the fact that we are now being prepared, means that we don't have to perform that clear call.
			// yay! that means we are going to not perform work that has quite a bit of perf overhead.
			// ---
			// we happen to know that the clear will have been pushed to the back of the vector, so optimize
			// the panning scenario by checking in reverse order

			// special case the last element (since we push_back during when we called clear and we expect the next
			// action to be this prepare).
			uint toBeClearedContainerCount = 0;
			EnsureToBeClearedContainers();
			toBeClearedContainerCount = (uint)m_toBeClearedContainers.Count;

			for (uint current = toBeClearedContainerCount - 1; current >= 0 && toBeClearedContainerCount > 0; --current)
			{
				CalendarViewDayItem spCurrentContainer;
				// go from back to front, since we're most likely in the back.
				spCurrentContainer = m_toBeClearedContainers[(int)current];

				if (spCurrentContainer == (CalendarViewDayItem)(pContainer))
				{
					m_toBeClearedContainers.RemoveAt((int)current);
					break;
				}

				if (current == 0)
				{
					// uint
					break;
				}
			}
		}

		bool ITreeBuilder.IsRegisteredForCallbacks
		{
			get
			{
				return m_isRegisteredForCallbacks;
			}
			set
			{
				m_isRegisteredForCallbacks = value;
			}
		}

		bool ITreeBuilder.IsBuildTreeSuspended
		{
			get
			{
				var pOwner = Owner;
				//var pReturnValue = pOwner.IsCollapsed || !pOwner.AreAllAncestorsVisible;
				var pReturnValue = false; // TODO UNO
				return pReturnValue;
			}
		}

		// the async version of doWork that is being called by NWDrawTree
		bool ITreeBuilder.BuildTree()
		{
			bool pWorkLeft;
			int timeElapsedInMS = 0;
			BudgetManager spBudget;

			pWorkLeft = true;

			spBudget = DXamlCore.Current.GetBudgetManager();
			timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick();

			if ((uint)(timeElapsedInMS) <= m_budget)
			{
				var pCalendarPanel = Panel;

				if (pCalendarPanel is { })
				{
					// we are going to do different types of work:
					// 1. process incremental visualization
					// 2. process deferred clear container work
					// 3. process extra cache

					// at this point, cache indices are set correctly
					// We might be going several passes over the containers. Currently we are not keeping those containers
					// in a particular datastructure, but we are re-using the children collection on our moderncollectionbasepanel
					// We also have a nice hint (m_lowestPhaseInQueue) that tells us what phase to look out for. While we are doing our
					// walk, we're going to build up a structure that allows us to do the second walks much faster.
					// When we are done, we'll throw it away.

					// we do not want to do incremental loading when we are not in the live tree.
					// 1. process incremental visualization
					if (Owner.IsInLiveTree && pCalendarPanel.IsInLiveTree)
					{
						ProcessIncrementalVisualization(spBudget, pCalendarPanel);
					}

					// 2. Clear containers
					// BUG#1331271 - make sure containers can be cleared even if CalendarView is not in live tree
					ClearContainers(spBudget);

					uint containersToClearCount = 0;
					containersToClearCount = (uint)m_toBeClearedContainers.Count;

					// we have work left if we still have containers that need to finish their phases
					// or when we have containers that need to be cleared
					pWorkLeft = m_lowestPhaseInQueue != -1 || containersToClearCount > 0;
				}
			}

			return pWorkLeft;
		}

		internal override void RaiseContainerContentChangingOnRecycle(
			UIElement pContainer,
			object? pItem)
		{
			CalendarViewDayItemChangingEventArgs spArgs;
			CalendarViewDayItemChangingEventArgs spArgsConcrete;
			CalendarViewDayItem spContainer;

			spContainer = (CalendarViewDayItem)pContainer;
			spArgs = (spContainer as CalendarViewDayItem).GetBuildTreeArgs();
			spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;

			CalendarViewDayItemChangingEventSourceType? pEventSource = null;

			Owner.GetCalendarViewDayItemChangingEventSourceNoRef(out pEventSource);

			if (Owner.ShouldRaiseEvent(pEventSource))
			{
				bool wantCallback = false;

				spArgsConcrete.InRecycleQueue = true;
				spArgsConcrete.Phase = 0;
				spArgsConcrete.WantsCallBack = false;
				spArgsConcrete.Callback = null;
				spArgsConcrete.Item = spContainer;

				pEventSource.Invoke(Owner, spArgs);
				wantCallback = (spArgs as CalendarViewDayItemChangingEventArgs).WantsCallBack;

				if (wantCallback)
				{
					if (m_lowestPhaseInQueue == -1)
					{
						uint phaseArgs = 0;
						phaseArgs = (spArgs as CalendarViewDayItemChangingEventArgs).Phase;

						// there was nothing registered
						m_lowestPhaseInQueue = phaseArgs;

						// that means we need to register ourselves with the buildtreeservice so that
						// we can get called back to do some work
						if (!m_isRegisteredForCallbacks)
						{
							BuildTreeService spBuildTree;
							spBuildTree = DXamlCore.Current.GetBuildTreeService();
							spBuildTree.RegisterWork(this);
						}
					}
				}
				else
				{
					spArgsConcrete.ResetLifetime();
				}
			}
			else
			{
				spArgsConcrete.ResetLifetime();
			}

		}

		private void ProcessIncrementalVisualization(
			BudgetManager spBudget,
			CalendarPanel pCalendarPanel)
		{
			if (m_lowestPhaseInQueue > -1)
			{
				int timeElapsedInMS = 0;
				IItemContainerMapping spMapping;
				// A block structure has been considered, but we do expect to continuously mutate the phase on containers, which would have
				// cost us perf while reflecting that in the blocks. Instead, I keep an array of the size of the amount of containers i'm interested in.
				// The idea is that walking through that multiple times is still pretty darn fast.

				PanelScrollingDirection direction = PanelScrollingDirection.None;

				// the following four indices will be fetched from the panel
				// notice how they are not guaranteed not to be stale: one scenario in which they are
				// plain and simply wrong is when you collapse/remove a panel from the tree and start
				// mutating the collection. Arrange will not get a chance to run after the mutation and
				// if we are still registered to do work, we will not be able to fetch the container.
				int cacheStart = -1;
				int visibleStart = -1;
				int visibleEnd = -1;
				int cacheEnd = -1;

				spMapping = pCalendarPanel.GetItemContainerMapping();
				cacheStart = pCalendarPanel.FirstCacheIndexBase;
				visibleStart = pCalendarPanel.FirstVisibleIndexBase;
				visibleEnd = pCalendarPanel.LastVisibleIndexBase;
				cacheEnd = pCalendarPanel.LastCacheIndexBase;

				// these four match the indices, except they are mapped into a lookup array.
				// notice however, that we understand how visibleindex could have been -1.
				//int cacheStartInVector = -1;
				int visibleStartInVector = -1;
				int visibleEndInVector = -1;
				int cacheEndInVector = -1;

				// translate to array indices
				if (cacheEnd > -1) // -1 means there is no thing.. no visible or cached containers
				{
					//cacheStartInVector = 0;
					visibleStartInVector = visibleStart > -1 ? visibleStart - cacheStart : 0;
					visibleEndInVector = visibleEnd > -1 ? visibleEnd - cacheStart : visibleStartInVector;
					cacheEndInVector = cacheEnd - cacheStart;

				}
				else
				{
					// well, nothing to do,
					m_lowestPhaseInQueue = -1;
				}

				// start off uninitialized
				int currentPositionInVector = -1;

				direction = pCalendarPanel.PanningDirectionBase;

				// trying to find containers in this phase
				long processingPhase = m_lowestPhaseInQueue;
				// when we are done with a full iteration, will be looking for the nextlowest phase
				// note: long here so we can set it to a max that is out of range of the public phase property which is INT
				// max is used to indicate there is nothing left to work through.
				long nextLowest = long.MaxValue;

				// policy is to go through visible children first, then buffer in panning direction, then buffer in non-panning direction
				// this method will help us do that
				void ProcessCurrentPosition()
				{
					bool increasePhase = false;
					bool forward = direction != PanelScrollingDirection.Backward;

					// initialize
					if (currentPositionInVector == -1)
					{
						currentPositionInVector = forward ? visibleStartInVector : visibleEndInVector;
					}
					else
					{
						if (forward)
						{
							// go forward until you reach the end of the forward buffer, and start in the opposite buffer
							// going in the opposite direction
							if (currentPositionInVector >= visibleStartInVector)
							{
								++currentPositionInVector;
								if (currentPositionInVector > cacheEndInVector)
								{
									currentPositionInVector = visibleStartInVector - 1; // set to the start of the left section
								}
							}
							else
							{
								// processing to the left
								--currentPositionInVector;
							}

							// run off or no cache on left side
							if (currentPositionInVector < 0)
							{
								currentPositionInVector = visibleStartInVector;
								increasePhase = true;
							}
						}
						else
						{
							if (currentPositionInVector <= visibleEndInVector)
							{
								--currentPositionInVector;
								if (currentPositionInVector < 0)
								{
									currentPositionInVector = visibleEndInVector + 1; // set to the start of the right section
								}
							}
							else
							{
								// processing to the right
								++currentPositionInVector;
							}

							// run off or no cache on right side
							if (currentPositionInVector > cacheEndInVector)
							{
								currentPositionInVector = visibleEndInVector;
								increasePhase = true;
							}
						}

						if (increasePhase)
						{
							processingPhase = nextLowest;
							// when increasing, signal using max() that we can stop after this iteration.
							// this will be undone by the loop, resetting it to the actual value
							nextLowest = long.MaxValue;
						}
					}
				}
				;

				// the array that we keep iterating until we're done
				// -1 means, not fetched yet
				long[] lookup;
				lookup = new long[cacheEndInVector + 1];
				for (var i = 0; i < lookup.Length; i++)
					lookup[i] = -1;

				// initialize before going into the loop
				ProcessCurrentPosition();

				while (processingPhase != long.MaxValue
					&& (uint)(timeElapsedInMS) < m_budget
					&& lookup.Length > 0)
				{
					long phase = 0;
					DependencyObject spContainer;
					CalendarViewDayItem? spContainerAsCalendarViewDayItem = default;
					VirtualizationInformation? pVirtualizationInformation = null;
					CalendarViewDayItemChangingEventArgs? spArgs = default;
					CalendarViewDayItemChangingEventArgs spArgsConcrete;
					// always update the current position when done, except when the phase requested is lower than current phase
					bool shouldUpdateCurrentPosition = true;

					// what is the phase?
					phase = lookup[currentPositionInVector];
					if (phase == -1)
					{
						// not initialized yet
						uint argsPhase = 0;

						spContainer = spMapping.ContainerFromIndex(cacheStart + currentPositionInVector);
						if (spContainer is null)
						{
							// this is possible when mutations have occurred to the collection but we
							// cannot be reached through layout. This is very hard to figure out, so we just harden
							// the code here to deal with nulls.
							ProcessCurrentPosition();
							continue;
						}

						spContainerAsCalendarViewDayItem = (CalendarViewDayItem)spContainer;

						pVirtualizationInformation = (spContainer as UIElement)?.GetVirtualizationInformation();
						spArgs = (spContainerAsCalendarViewDayItem as CalendarViewDayItem).GetBuildTreeArgs();

						argsPhase = spArgs.Phase;
						phase = argsPhase; // fits easily

						lookup[currentPositionInVector] = phase;
					}

					if (spArgs is null)
					{
						// we might have skipped getting the args, let's do that now.
						spContainer = spMapping.ContainerFromIndex(cacheStart + currentPositionInVector);

						spContainerAsCalendarViewDayItem = (CalendarViewDayItem)spContainer;
						pVirtualizationInformation = (spContainer as UIElement)?.GetVirtualizationInformation();

						spArgs = (spContainerAsCalendarViewDayItem as CalendarViewDayItem).GetBuildTreeArgs();
					}

					// guaranteed to have spArgs now
					spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;

					if (phase == processingPhase)
					{
						// processing this guy
						bool wantsCallBack = false;
						Size measureSize = default;

						TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs> spCallback;

						// guaranteed to have pVirtualizationInformation by now

						global::System.Diagnostics.Debug.Assert(pVirtualizationInformation is { });

						measureSize = pVirtualizationInformation!.MeasureSize;

						// did we store a callback
						spCallback = spArgsConcrete.Callback;

						// raise event
						if (spCallback is { })
						{
							spArgsConcrete.WantsCallBack = false;
							// clear out the delegate
							spArgsConcrete.Callback = null;

							spCallback.Invoke(Owner, spArgs);

							// the invoke will cause them to call RegisterCallback which will overwrite the delegate (fine)
							// and set the boolean below to true
							wantsCallBack = spArgsConcrete.WantsCallBack;
						}

						// the user might have changed elements. In order to keep the budget fair, we need to try and incur
						// most of the cost right now.
						(spContainerAsCalendarViewDayItem as CalendarViewDayItem)?.Measure(measureSize);

						// register callback
						if (wantsCallBack)
						{
							uint phaseFromArgs = 0;
							phaseFromArgs = spArgsConcrete.Phase;
							phase = phaseFromArgs;
							lookup[currentPositionInVector] = phase;

							// if the appcode requested a phase that is lower than the current processing phase, it is kind of weird
							if (phase < processingPhase)
							{
								// after we change the processingphase, our next lowest is going to be the current phase (we didn't finish it yet)
								nextLowest = processingPhase;

								// change our processing phase to the requested phase. It is going to be the one we work on next
								processingPhase = phase;
								m_lowestPhaseInQueue = processingPhase;

								// the pointer is pointing to the current container which is great
								shouldUpdateCurrentPosition = false;
							}
							else
							{
								// update the next lowest to the best of our current understanding
								nextLowest = Math.Min(nextLowest, (long)(phase));
							}
						}
						else
						{
							// won't be called again for the lifetime of this container
							spArgsConcrete.ResetLifetime();

							// we do not have to update the next lowest. We are still processing this phase and will
							// continue to do so (procesingPhase is still valid).
						}
					}
					else //if (phase == processingPhase)
					{
						// if we hit a container that is registered for a callback (so he wants to iterate over phases)
						// but is currently at a different phase, we need to make sure that the next lowest is set.
						bool wantsCallBack = false;
						wantsCallBack = spArgsConcrete.WantsCallBack;

						if (wantsCallBack)
						{
							global::System.Diagnostics.Debug.Assert(phase > processingPhase);
							// update the next lowest, now that we have seen a phase that is higher than our current processing phase
							nextLowest = Math.Min(nextLowest, (long)(phase));
						}
					}

					// updates the current position in the correct direction
					if (shouldUpdateCurrentPosition)
					{
						ProcessCurrentPosition();
					}

					// updates the time
					timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick();
				}

				if (processingPhase == long.MaxValue)
				{
					// nothing left to process
					m_lowestPhaseInQueue = -1;
				}
				else
				{
					// we broke out of the loop for some other reason (policy)
					// should be safe at this point
					global::System.Diagnostics.Debug.Assert(processingPhase < int.MaxValue);
					m_lowestPhaseInQueue = (int)(processingPhase);
				}
			}

		}

		private void EnsureToBeClearedContainers()
		{
			if (m_toBeClearedContainers is null)
			{
				TrackerCollection<CalendarViewDayItem> spContainersForClear;

				spContainersForClear = new TrackerCollection<CalendarViewDayItem>();
				m_toBeClearedContainers = spContainersForClear;
			}

		}

		private void ClearContainers(
			BudgetManager spBudget)
		{
			uint containersToClearCount = 0;
			int timeElapsedInMS = 0;

			EnsureToBeClearedContainers();

			containersToClearCount = (uint)m_toBeClearedContainers.Count;
			for (uint toClearIndex = containersToClearCount - 1; toClearIndex >= 0 && containersToClearCount > 0; --toClearIndex)
			{
				CalendarViewDayItem spContainer;

				timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick();

				if ((uint)(timeElapsedInMS) > m_budget)
				{
					break;
				}

				spContainer = m_toBeClearedContainers[(int)toClearIndex];
				m_toBeClearedContainers.RemoveAtEnd();

				// execute the deferred work
				// apparently we were not going to reuse this container immediately again, so
				// let's do the work now

				// we don't need the spItem because 1. we didn't save this information, 2. CalendarViewGeneratorHost.ClearContainerForItem is no-op for now
				// if we need this we could simple restore the spItem from the container.
				ClearContainerForItem(spContainer as CalendarViewDayItem, null /* spItem */);

				// potentially raise the event
				if (spContainer is { })
				{
					RaiseContainerContentChangingOnRecycle(spContainer as UIElement, null);
				}

				if (toClearIndex == 0)
				{
					// uint
					break;
				}
			}

		}

#if false
		private void ShutDownDeferredWork()
		{
			IItemContainerMapping spMapping;

			var pCalendarPanel = Panel;

			if (pCalendarPanel is { })
			{
				// go through everyone that might have work registered for a prepare
				int cacheStart, cacheEnd = 0;
				cacheStart = pCalendarPanel.FirstCacheIndexBase;
				cacheEnd = pCalendarPanel.LastCacheIndexBase;
				spMapping = pCalendarPanel.GetItemContainerMapping();

				for (int i = cacheStart; i < cacheEnd; ++i)
				{
					DependencyObject spContainer;
					CalendarViewDayItemChangingEventArgs spArgs;
					spContainer = spMapping.ContainerFromIndex(i);

					if (spContainer is null)
					{
						// apparently a sentinel. This should not occur, however, during shutdown we could
						// run into this since measure might not have been processed yet
						continue;
					}

					spArgs = (spContainer as CalendarViewDayItem)!.GetBuildTreeArgs();

					(spArgs as CalendarViewDayItemChangingEventArgs).ResetLifetime();
				}
			}

		}
#endif

		private void RegisterWorkFromCICArgs(
			CalendarViewDayItemChangingEventArgs pArgs)
		{
			bool wantsCallback = false;
			CalendarViewDayItem spCalendarViewDayItem;
			CalendarViewDayItemChangingEventArgs pConcreteArgsNoRef = (CalendarViewDayItemChangingEventArgs)(pArgs);

			wantsCallback = pConcreteArgsNoRef.WantsCallBack;
			spCalendarViewDayItem = pConcreteArgsNoRef.Item;

			// we are going to want to be called back if:
			// 1. we are still showing the placeholder
			// 2. app code registered to be called back
			if (wantsCallback)
			{
				uint phase = 0;

				phase = pConcreteArgsNoRef.Phase;

				// keep this state on the listviewbase
				if (m_lowestPhaseInQueue == -1)
				{
					// there was nothing registered
					m_lowestPhaseInQueue = phase;

					// that means we need to register ourselves with the buildtreeservice so that
					// we can get called back to do some work
					if (!m_isRegisteredForCallbacks)
					{
						BuildTreeService spBuildTree;
						spBuildTree = DXamlCore.Current.GetBuildTreeService();
						spBuildTree.RegisterWork(this);
					}

					global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);
				}
				else if (m_lowestPhaseInQueue > phase)
				{
					m_lowestPhaseInQueue = phase;
				}
			}
			else
			{
				// well, app code doesn't want a callback so cleanup the args
				pConcreteArgsNoRef.ResetLifetime();
			}

		}
	}
}
