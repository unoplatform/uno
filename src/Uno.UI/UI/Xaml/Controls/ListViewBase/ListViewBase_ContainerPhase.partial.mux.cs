// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_ContainerPhase.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListViewBase
{
	// Uncomment to output ListViewBase debugging information
	//#define LVB_DEBUG

	public void SetDesiredContainerUpdateDuration(TimeSpan duration)
	{
		// duration is expressed in 100-nanosecond units
		// I will need ms.
		m_budget = (uint)duration.Ticks / 10000;
	}

	// Determines whether we want to defer the prepare of this container
	// In the case of ListViewBase, this is determined by external API
	private protected override bool IsPrepareContainerForItemDeferred(DependencyObject element)
	{
		VirtualizationInformation? pVirtualizationInformation = null;
		bool defer = false;

		// listview supports deferring setting the content.
		// is this a new style container? We can know by looking at the virtualizationInformation struct which is
		// a ModernCollectionBase concept

		global::System.Diagnostics.Debug.Assert(element is UIElement);

		// m_disableScrollingPlaceholders may be set to temporarily disable display of placeholders.
		// This is typically the case when switching between views of SemanticZoom on phone
		if (!m_disableScrollingPlaceholders)
		{
			pVirtualizationInformation = ((UIElement)element).GetVirtualizationInformationOrNull();

			if (pVirtualizationInformation is not null)
			{
				var item = pVirtualizationInformation.Item;

				if (item is not UIElement)
				{
					UIElement spElement;
					Panel spPanel;
					IChildTransitionContextProvider? spTCP;
					bool isIgnoringTransitions = false;

					spPanel = ItemsPanelRoot;

					// If we are ignoring transitions then it means we are in the middle of scrolling.
					// In this case always show scrolling placeholders.
					isIgnoringTransitions = spPanel.IsIgnoringTransitions;

					if (!isIgnoringTransitions)
					{
						spElement = (UIElement)element;
						spTCP = spPanel as IChildTransitionContextProvider;

						if (spTCP is not null)
						{
							int layoutTick = 0;
							ThemeTransitionContext tt;

							layoutTick = CoreImports.LayoutManager_GetLayoutTickForTransition(spElement);

							tt = spTCP.GetChildTransitionContext(spElement, layoutTick);

							if (tt != ThemeTransitionContext.None)
							{
								return defer;
							}
						}
					}

					// we're going to switch behavior on the existence of this struct.
					defer = ShowsScrollingPlaceholders;
				}
			}
		}

		return defer;
	}

	// Called after prepare. This will raise the event and put us into the queue
	private protected override void SetupContainerContentChangingAfterPrepare(
		DependencyObject container,
		object? item,
		int itemIndex,
		Size measureSize)
	{
#if !HAS_UNO // TODO Uno: telemetry.
		TraceSetupCCCBegin(itemIndex);
#endif
		DependencyObject? itemAsDO;

		// this is being called by modern panels after the prepare has occurred.
		// we will setup information that we will need during the lifetime of this container

		SelectorItem? containerAsISI = null;
		ContainerContentChangingEventArgs? args = null;
		ContainerContentChangingEventArgs argsConcrete;
		UIElement? templateRoot;
		bool handled = false;
		bool measureContentAfterCode = false;

		// raw pointer, since we're not a refcounted object
		VirtualizationInformation? virtualizationInformation = null;
		bool showingPlaceholder = false;
		itemAsDO = item as DependencyObject;

		// If the item is an element, there is no reason to do any of this fancy work
		// see the if condition in prepare
		if (itemAsDO != container)
		{
			containerAsISI = container as SelectorItem;
			if (containerAsISI is not null)
			{
				// is this a new style container? We can know by looking at the virtualizationInformation struct which is
				// a ModernCollectionBase concept
				virtualizationInformation = containerAsISI.GetVirtualizationInformationOrNull();
				if (virtualizationInformation is not null)
				{
					// we're going to switch behavior on the existence of this struct.

					// it is somewhat expensive to call ShowsScrollingPlaceholders and we have already called it
					// in ItemsControl::PrepareContainerForItemOverride. The result there has been to
					// set us as UIPlaceholder, so instead of doing this call:
					// IFC(get_ShowsScrollingPlaceholders(&deferredBehavior));
					// we will do this call:
					showingPlaceholder = containerAsISI.GetIsUIPlaceholder();
					args = virtualizationInformation.GetBuildTreeArgs();
				}
			}

			global::System.Diagnostics.Debug.Assert(virtualizationInformation is not null);
			global::System.Diagnostics.Debug.Assert(args is not null);
			argsConcrete = args!;

			// store the size we would measure with
			virtualizationInformation!.MeasureSize = measureSize;

			// initialize values in the args
			argsConcrete.WantsCallBack = false;              // let them explicitly call-out if they want it
			argsConcrete.Item = item!;                       // there is now a hard ref
			argsConcrete.InRecycleQueue = false;
			argsConcrete.Phase = 0;
			argsConcrete.ItemIndex = itemIndex;              // note how this is passed in, it is very expensive to calculate on the fly
			argsConcrete.Handled = false;
			argsConcrete.SetBindingPhase(ContainerContentChangingEventArgs.NoBindingNeededToken);                   // start off with no bindingphase needed

			// force measure. This will be no-op since content has not been set/changed
			// but we need it for the contenttemplateroot
			containerAsISI!.Measure(measureSize);
			bool isContainerFromTemplateRoot = virtualizationInformation.IsContainerFromTemplateRoot;

			// raise the event. This is the synchronous version. we will raise it 'async' as well when we have time
			// but we guarantee calling it after prepare
			if (ShouldRaiseEvent(ContainerContentChanging))
			{
				ListViewBase thisAsLVB = this;

				ContainerContentChanging?.Invoke(thisAsLVB, argsConcrete);
				handled = argsConcrete.Handled;

				if (handled)
				{
					// never set the content any more
					// the placeholder will be removed before the callback if there is one
					argsConcrete.ContentShouldBeSet = false;
				}
				else
				{
					if (showingPlaceholder)
					{
						argsConcrete.ContentShouldBeSet = true;
					}
					else
					{
						// default behavior of setting the content should be performed
						// this means that bindings and so on will work by default, but some big cost is being paid
						if (!isContainerFromTemplateRoot)
						{
							containerAsISI.Content = item;
						}

						argsConcrete.ContentShouldBeSet = false;
						measureContentAfterCode = true;
					}
				}
			}
			else
			{
				// app code did not hook the event
				if (showingPlaceholder)
				{
					// if we were showing placeholders we need to set the content
					// before we do the (implicit) callback
					argsConcrete.ContentShouldBeSet = true;
				}
				else
				{
					// app code did not hook the event and is not showing placeholders
					// we should set content now
					argsConcrete.ContentShouldBeSet = false;
					if (!isContainerFromTemplateRoot)
					{
						containerAsISI.Content = item;
					}

					measureContentAfterCode = true;
				}
			}

			// above we did the user code callbacks, now deal with the x:bind facility
			// xbind will put an extension on the root of the datatemplate tree
			if (isContainerFromTemplateRoot)
			{
				// If root of the template is the container, the container itself will have the extension
				templateRoot = containerAsISI;

				// We just want to pass along data context in this case, so that bindings continue to work.
				if (containerAsISI is FrameworkElement containerAsFE)
				{
					containerAsFE.DataContext = item;
				}

				// We do not want to set content ever if container is from item template.
				argsConcrete.ContentShouldBeSet = false;
			}
			else
			{
				templateRoot = containerAsISI.ContentTemplateRoot;
			}

			if (templateRoot is not null)
			{
				IDataTemplateComponent? templateComponent = XamlBindingHelper.GetDataTemplateComponent(templateRoot);
				if (templateComponent is not null)
				{
					// There is a DataTemplateComponent, use that.
					int requestedPhase = -1;
					templateComponent.ProcessBindings(item!, itemIndex, 0 /* phase */, out requestedPhase);
					// the public api on ProcessBinding for an extension is to not register for the callback like the app will do,
					// but return the phase it wants to be called back.
					argsConcrete.SetBindingPhase(requestedPhase);
					measureContentAfterCode = true;
				}
				else
				{
					// Fallback to using DataTemplateExtension to be compatible with apps built using older sdks.
					IDataTemplateExtension? extension = (templateRoot is FrameworkElement templateRootAsFE) ? DataTemplate.GetExtensionInstance(templateRootAsFE) : null;
					if (extension is not null)
					{
						// yep, there is an extension
						Exception? hr2 = null;
						int requestedPhase = -1;
						uint nextAppCallback = 0;
						// app usage might have changed the phase
						nextAppCallback = argsConcrete.Phase;
						argsConcrete.Phase = 0;  // working on phase 0

						try
						{
							requestedPhase = extension.ProcessBindings(args!);
						}
						catch (Exception e)
						{
							hr2 = e;
						}

						// put back what the app expected
						argsConcrete.Phase = nextAppCallback;

						// the public api on ProcessBinding for an extension is to not register for the callback like the app will do,
						// but return the phase it wants to be called back.
						argsConcrete.SetBindingPhase(requestedPhase);

						if (hr2 is not null)
						{
							global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(hr2).Throw();
						}

						measureContentAfterCode = true;
					}
				}
			}

			if (measureContentAfterCode)
			{
				// need to measure, so we keep the budget as fair as possible
				containerAsISI.Measure(measureSize);
				containerAsISI.UpdateVisualState(true /* bUseTransitions */);
			}

#if !HAS_UNO // TODO Uno: telemetry.
			TraceVirtualizedItemAddedInfo(
				(UINT64)containerAsISI.GetHandle(),
				(UINT64)(m_tpScrollViewer != null ? m_tpScrollViewer.GetHandle() : nullptr),
				itemIndex,
				showingPlaceholder
			);
#endif

			RegisterWorkFromArgs(argsConcrete);
		}

#if !HAS_UNO // TODO Uno: telemetry.
		TraceSetupCCCEnd();
#endif
	}

	private protected override void RegisterWorkForContainer(
		UIElement pContainer)
	{
		ContainerContentChangingEventArgs? spArgs;

		spArgs = pContainer.GetVirtualizationInformationOrNull()!.GetBuildTreeArgs();
		if (spArgs is not null)
		{
			RegisterWorkFromArgs(spArgs);
		}
	}

	// Register for work from the BuildTree service based on the given args
	private protected override void RegisterWorkFromArgs(
		ContainerContentChangingEventArgs pArgs)
	{
		bool wantsAppCallback = false;
		bool wantsxbindingCallback = false;
		SelectorItem spContainer;
		var pConcreteArgsNoRef = pArgs;

		wantsAppCallback = pConcreteArgsNoRef.WantsCallBack;
		wantsxbindingCallback = pConcreteArgsNoRef.GetWantsBindingPhaseCallback();
		spContainer = pConcreteArgsNoRef.ItemContainer;

		// we are going to want to be called back if:
		// 1. we are still showing the placeholder
		// 2. app code registered to be called back
		// 3. binding code registered to be called back
		if (spContainer.GetIsUIPlaceholder() || wantsAppCallback || wantsxbindingCallback)
		{
			long phase = 1;

			// we will to the lowest phase of the set that the app and the binding has asked us to go to
			if (wantsAppCallback)
			{
				uint phaseArgs = 0;
				phaseArgs = pConcreteArgsNoRef.Phase;
				phase = phaseArgs;
			}

			// default behavior is that this has elevated us to phase 1.
			// the static cast is fine here

			// put in the public phase that the user has requested (phase)
			pConcreteArgsNoRef.Phase = (uint)(int)phase;

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
			else
			{
				m_lowestPhaseInQueue = Math.Min(m_lowestPhaseInQueue, phase);
			}

			// could be that the user wanted phase 5, but there is a x:bind for phase 2
			if (wantsxbindingCallback)
			{
				m_lowestPhaseInQueue = Math.Min(m_lowestPhaseInQueue, (long)pConcreteArgsNoRef.GetBindingPhase());
			}
		}
		else
		{
			// well, app code doesn't want a callback, and we don't need one (no placeholder mode)
			// so cleanup the args
			pConcreteArgsNoRef.ResetLifetime();
		}
	}

	internal void RegisterIfWorkPending()
	{
		if (m_lowestPhaseInQueue != -1 && !m_isRegisteredForCallbacks)
		{
			BuildTreeService spBuildTree;
			spBuildTree = DXamlCore.Current.GetBuildTreeService();
			spBuildTree.RegisterWork(this);
		}
	}

	internal bool IsBuildTreeSuspended
		=> Visibility == Visibility.Collapsed || !AreAllAncestorsVisible();

	// the async version of doWork that is being called by NWDrawTree
	internal bool BuildTree()
	{
		bool workLeft;
		int timeElapsedInMS = 0;
		BudgetManager? budget;
		uint containersToClearCount = 0;
		Panel? itemsHost;
		ICustomGeneratorItemsHost? customGeneratorPanel = null;

		workLeft = true;

		budget = DXamlCore.Current.GetBudgetManager();

		if (!budget.UseUnbudgetedContainerBuild())
		{
			timeElapsedInMS = budget.GetElapsedMilliSecondsSinceLastUITick();
		}

		if ((uint)timeElapsedInMS <= m_budget || budget.UseUnbudgetedContainerBuild())
		{
			itemsHost = ItemsPanelRoot;
			if (itemsHost is not null)
			{
				customGeneratorPanel = itemsHost as ICustomGeneratorItemsHost;
			}

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
			bool isInLiveTree = IsInLiveTree && itemsHost is not null && itemsHost.IsInLiveTree;

			// 1. process incremental visualization
			if (m_lowestPhaseInQueue > -1 && customGeneratorPanel is not null && isInLiveTree)
			{
				// A block structure has been considered, but we do expect to continuously mutate the phase on containers, which would have
				// cost us perf while reflecting that in the blocks. Instead, I keep an array of the size of the amount of containers i'm interested in.
				// The idea is that walking through that multiple times is still pretty darn fast.

				// Do not hold on spContainersIterator outside of this frame. Its state won't be valid outside of it.
				IContainerContentChangingIterator? containersIterator;
				containersIterator = customGeneratorPanel.GetContainersForIncrementalVisualization();

				uint containersCount = 0;
				if (containersIterator is not null)
				{
					containersCount = containersIterator.Size;
					if (containersCount == 0)
					{
						// Nothing to do.
						m_lowestPhaseInQueue = -1;
					}
				}
				else
				{
					// Nothing to do.
					m_lowestPhaseInQueue = -1;
				}

				if (m_lowestPhaseInQueue != -1)
				{
					// trying to find containers in this phase
					long processingPhase = m_lowestPhaseInQueue;

					// when we are done with a full iteration, will be looking for the nextlowest phase
					// note: INT64 here so we can set it to a max that is out of range of the public phase property which is INT
					// max is used to indicate there is nothing left to work through.
					long nextLowest = long.MaxValue;

					// the array that we keep iterating until we're done
					// -1 means, not fetched yet
					List<long> lookup = new();
					for (uint i = 0; i < containersCount; i++)
					{
						lookup.Add(-1);
					}

					global::System.Diagnostics.Debug.Assert(containersCount > 0);
					uint currentPositionInVector = unchecked((uint)-1);
					void ProcessCurrentPosition()
					{
						bool thereIsMore = containersIterator!.MoveNext();
						currentPositionInVector = unchecked(currentPositionInVector + 1) % containersCount;

						// we increase the phase when we loop back to the beginning.
						if (!thereIsMore)
						{
							processingPhase = nextLowest;
							// when increasing, signal using max() that we can stop after this iteration.
							// this will be undone by the loop, resetting it to the actual value
							nextLowest = long.MaxValue;

							containersIterator.Reset();
							thereIsMore = containersIterator.MoveNext();
						}
					}

					// initialize before going into the loop
					ProcessCurrentPosition();

					while (processingPhase != long.MaxValue &&
						((uint)timeElapsedInMS < m_budget || budget.UseUnbudgetedContainerBuild()))
					{
						long phase = 0;
						DependencyObject? container = null;
						VirtualizationInformation? virtualizationInformation = null;
						ContainerContentChangingEventArgs? args = null;
						ContainerContentChangingEventArgs argsConcrete;
						// always update the current position when done, except when the phase requested is lower than current phase
						bool shouldUpdateCurrentPosition = true;

						// what is the phase?
						phase = lookup[(int)currentPositionInVector];
						if (phase == -1)
						{
							// not initialized yet
							uint argsPhase = 0;
							container = containersIterator!.Current;
							if (container is null)
							{
								// this is possible when mutations have occurred to the collection but we
								// cannot be reached through layout. This is very hard to figure out, so we just harden
								// the code here to deal with nulls.
								ProcessCurrentPosition();
								continue;
							}
							global::System.Diagnostics.Debug.Assert(container is SelectorItem, "this should be a listviewitem or gridviewitem");

							virtualizationInformation = ((UIElement)container).GetVirtualizationInformationOrNull();
							args = virtualizationInformation!.GetBuildTreeArgs();

							argsPhase = args.GetLowestWantedPhase();
							phase = argsPhase;  // fits easily

							lookup[(int)currentPositionInVector] = phase;
						}

						if (args is null)
						{
							// we might have skipped getting the args, let's do that now.
							container = containersIterator!.Current;

							// no need to check for null here, because we already did that in previous block

							virtualizationInformation = ((UIElement)container!).GetVirtualizationInformationOrNull();
							args = virtualizationInformation!.GetBuildTreeArgs();
						}

						// guaranteed to have spArgs now
						argsConcrete = args;

						if (phase == processingPhase)
						{
							// processing this guy
							bool wantsAppCallback = false;
							Size measureSize = default;
							uint appCallbackPhase = 0;
							int bindingCallbackPhase = 0;

							TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>? callback = null;

							// guaranteed to have pVirtualizationInformation by now
							global::System.Diagnostics.Debug.Assert(virtualizationInformation is not null);
							global::System.Diagnostics.Debug.Assert(container is not null);

							measureSize = virtualizationInformation!.MeasureSize;

							// get out of placeholder mode
							if (((SelectorItem)container!).GetIsUIPlaceholder())
							{
								bool contentShouldBeSet = false;
								contentShouldBeSet = argsConcrete.ContentShouldBeSet;

								// get out of placeholder mode
								((SelectorItem)container).SetIsUIPlaceholder(false);

								if (contentShouldBeSet)
								{
									object item;
									item = args.Item;

									// the below is now very expensive
									((ContentControl)container).Content = item;
									argsConcrete.ContentShouldBeSet = false;
								}

								((SelectorItem)container).ChangeVisualStateFromOwner(true);
								((SelectorItem)container).Measure(measureSize);
							}

							// do we need to raise this for the binding system or for app callback (or both)
							appCallbackPhase = argsConcrete.Phase;
							bindingCallbackPhase = argsConcrete.GetBindingPhase();

							if (appCallbackPhase == processingPhase || bindingCallbackPhase == processingPhase)
							{
								// we are going to call someone back, maybe both

								if (appCallbackPhase == processingPhase)
								{
									// did we store a callback
									callback = argsConcrete.Callback;
								}

								int itemIndex = 0;
								itemIndex = IndexFromContainer(container);

								// make sure args are correct. The itemindex might have changed
								// note that we keep the item and container set since 'setupAfterPrepare'
								// and we will only clean them when the app doesn't want any more callbacks
								argsConcrete.ItemIndex = itemIndex;

								// they might not want a callback now, but we need to remember that we
								// might want one after the binding phase
								wantsAppCallback = argsConcrete.WantsCallBack;

								// raise event
								if (callback is not null)
								{
									argsConcrete.WantsCallBack = false;
									// clear out the delegate
									argsConcrete.Callback = null;
									argsConcrete.Handled = false;

									callback.Invoke(this, args);

									// the invoke will cause them to call RegisterCallback which will overwrite the delegate (fine)
									// and set the boolean below to true
									wantsAppCallback = argsConcrete.WantsCallBack;
								}

								if (bindingCallbackPhase == processingPhase)
								{
									UIElement? templateRoot;
									templateRoot = ((SelectorItem)container).ContentTemplateRoot;
									if (templateRoot is not null)
									{
										IDataTemplateComponent? templateComponent = XamlBindingHelper.GetDataTemplateComponent(templateRoot);
										if (templateComponent is not null)
										{
											// we have a template component, lets use that
											object item;
											int requestedPhase = -1;

											item = args.Item;
											itemIndex = args.ItemIndex;

											templateComponent.ProcessBindings(item, itemIndex, (int)processingPhase, out requestedPhase);

											// the public api on ProcessBinding for an extension is to not register for the callback like the app will do,
											// but return the phase it wants to be called back.
											argsConcrete.SetBindingPhase(requestedPhase);
										}
										else
										{
											// check if we have an extension for apps compiled with older sdk.
											IDataTemplateExtension? extension = (templateRoot is FrameworkElement templateRootAsFE) ? DataTemplate.GetExtensionInstance(templateRootAsFE) : null;
											if (extension is not null)
											{
												Exception? hr2 = null;
												uint nextAppCallback = 0;
												// app usage might have changed the phase
												nextAppCallback = argsConcrete.Phase;
												argsConcrete.Phase = (uint)processingPhase;

												// yep, there is an extension
												int requestedPhase = -1;
												try
												{
													requestedPhase = extension.ProcessBindings(args);
												}
												catch (Exception e)
												{
													hr2 = e;
												}

												// put back what the app expected
												argsConcrete.Phase = nextAppCallback;

												if (hr2 is not null)
												{
													global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(hr2).Throw();
												}

												// the public api on ProcessBinding for an extension is to not register for the callback like the app will do,
												// but return the phase it wants to be called back.
												argsConcrete.SetBindingPhase(requestedPhase);
											}
										}
									}
								}
							}

#if !HAS_UNO // TODO Uno: telemetry.
							TraceVirtualizedItemUpdatedInfo(
								(UINT64)container.GetHandle(),
								(UINT)phase
							);
#endif

							// always make sure we go out of placeholder mode independent of setting content
							if (((SelectorItem)container).GetIsUIPlaceholder())
							{
								((SelectorItem)container).SetIsUIPlaceholder(false);
								((SelectorItem)container).UpdateVisualState(true /* bUseTransitions */);
							}

							// the user might have changed elements. In order to keep the budget fair, we need to try and incur
							// most of the cost right now.
							((SelectorItem)container).Measure(measureSize);

							// register callback
							if (wantsAppCallback || argsConcrete.GetWantsBindingPhaseCallback())
							{
								uint phaseFromArgs = argsConcrete.GetLowestWantedPhase();
								phase = phaseFromArgs;
								lookup[(int)currentPositionInVector] = phase;

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
									nextLowest = Math.Min(nextLowest, (long)phase);
								}
							}
							else
							{
								// won't be called again for the lifetime of this container
								argsConcrete.ResetLifetime();

								// we do not have to update the next lowest. We are still processing this phase and will
								// continue to do so (procesingPhase is still valid).
							}
						}
						else
						{
							// if we hit a container that is registered for a callback (so he wants to iterate over phases)
							// but is currently at a different phase, we need to make sure that the next lowest is set.
							bool wantsAppCallback = false;
							wantsAppCallback = argsConcrete.WantsCallBack;

							if (wantsAppCallback || argsConcrete.GetWantsBindingPhaseCallback())
							{
								global::System.Diagnostics.Debug.Assert(phase > processingPhase);
								// update the next lowest, now that we have seen a phase that is higher than our current processing phase
								nextLowest = Math.Min(nextLowest, (long)phase);
							}
						}

						// updates the current position in the correct direction
						if (shouldUpdateCurrentPosition)
						{
							ProcessCurrentPosition();
						}

						if (!budget.UseUnbudgetedContainerBuild())
						{
							// updates the time
							timeElapsedInMS = budget.GetElapsedMilliSecondsSinceLastUITick();
						}
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
						m_lowestPhaseInQueue = (int)processingPhase;
					}
				}

				// cache count of contianers that were processed
				m_lastIncrementalVisualizationContainerCount = containersCount;
			}

			// 2. Clear containers
			containersToClearCount = (uint)m_toBeClearedContainers.Count;
			for (uint toClearIndex = containersToClearCount - 1; containersToClearCount > 0; --toClearIndex)
			{
				UIElement container;
				object? item;

				if (budget is null)
				{
					budget = DXamlCore.Current.GetBudgetManager();
				}

				timeElapsedInMS = budget.GetElapsedMilliSecondsSinceLastUITick();

				if ((uint)timeElapsedInMS > m_budget)
				{
					break;
				}

				// we stored a pair of container and item in two separate collections (because of tracking)
				container = m_toBeClearedContainers[(int)toClearIndex];
				item = m_toBeClearedItems[(int)toClearIndex];
				m_toBeClearedContainers.RemoveAt(m_toBeClearedContainers.Count - 1);
				m_toBeClearedItems.RemoveAt(m_toBeClearedItems.Count - 1);

				// execute the deferred work
				// apparently we were not going to reuse this container immediately again, so
				// let's do the work now
				base.ClearContainerForItemOverride(container, item!);

				if (toClearIndex == 0)
				{
					// UINT
					break;
				}
			}

			containersToClearCount = (uint)m_toBeClearedContainers.Count;

			// we have work left if we still have containers that need to finish their phases and we are still in the
			// live tree (or)
			// when we have containers that need to be cleared. Note that we will still clear containers even after
			// being taken out of the live tree.
			// If we are taken out of the live tree while still having phasing work, we need to make sure that the
			// next time we entre the tree, the phasing work continues. OnLoaded call does a check and register's work
			// for this case.
			workLeft = (m_lowestPhaseInQueue != -1 && isInLiveTree) || containersToClearCount > 0;
		}

#if !HAS_UNO // TODO Uno: LVB_DEBUG trace.
#if LVB_DEBUG
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: BuildTreeImpl. exit workLeft=%d", ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this, *workLeft));
#endif
#endif

		return workLeft;
	}

	internal void ShutDownDeferredWork()
	{
		Panel? spItemsHost;
		ICustomGeneratorItemsHost? spCustomGeneratorPanel = null;
		uint containersToClearCount = 0;

		spItemsHost = ItemsPanelRoot;
		if (spItemsHost is not null)
		{
			spCustomGeneratorPanel = spItemsHost as ICustomGeneratorItemsHost;
		}

		if (spCustomGeneratorPanel is not null)
		{
			// go through everyone that might have work registered for a prepare
			IContainerContentChangingIterator? spContainersIterator;
			spContainersIterator = spCustomGeneratorPanel.GetContainersForIncrementalVisualization();

			if (spContainersIterator is not null)
			{
				bool thereIsMore;
				thereIsMore = spContainersIterator.MoveNext();

				while (thereIsMore)
				{
					DependencyObject? spContainer;
					spContainer = spContainersIterator.Current;

					if (spContainer is not null)
					{
						ContainerContentChangingEventArgs spArgs;
						VirtualizationInformation? pVirtualizationInformation =
							((UIElement)spContainer).GetVirtualizationInformationOrNull();

						spArgs = pVirtualizationInformation!.GetBuildTreeArgs();
						spArgs.ResetLifetime();

					}
					else
					{
						// apparently a sentinel. This should not occur, however, during shutdown we could
						// run into this since measure might not have been processed yet
					}


					thereIsMore = spContainersIterator.MoveNext();
				}
			}
		}

		containersToClearCount = (uint)m_toBeClearedContainers.Count;

		// go to all the containers waiting to clear
		for (uint currentIndex = 0; currentIndex < containersToClearCount; ++currentIndex)
		{
			VirtualizationInformation? pVirtualizationInformation = null;
			UIElement spContainer;
			ContainerContentChangingEventArgs spArgs;

			spContainer = m_toBeClearedContainers[(int)currentIndex];

			pVirtualizationInformation = spContainer.GetVirtualizationInformationOrNull();

			spArgs = pVirtualizationInformation!.GetBuildTreeArgs();

			spArgs.ResetLifetime();
		}

		m_toBeClearedContainers.Clear();
		m_toBeClearedItems.Clear();
	}

	private protected override bool ShouldRaiseChoosingItemContainer()
	{
		bool shouldRaiseChoosingItemContainer = false;

		shouldRaiseChoosingItemContainer = ChoosingItemContainer is not null && ShouldRaiseEvent(ChoosingItemContainer);

		return shouldRaiseChoosingItemContainer;
	}

	private protected override bool ShouldRaiseChoosingGroupHeaderContainer()
	{
		bool shouldRaiseChoosingGroupHeaderContainer = false;

		shouldRaiseChoosingGroupHeaderContainer = ChoosingGroupHeaderContainer is not null && ShouldRaiseEvent(ChoosingGroupHeaderContainer);

		return shouldRaiseChoosingGroupHeaderContainer;
	}

	private protected override void RaiseChoosingItemContainer(
		ChoosingItemContainerEventArgs pArgs)
	{
		global::System.Diagnostics.Debug.Assert(ShouldRaiseEvent(ChoosingItemContainer));

		ListViewBase spThis = this;

		ChoosingItemContainer?.Invoke(spThis, pArgs);
	}

	private protected override void RaiseChoosingGroupHeaderContainer(
		ChoosingGroupHeaderContainerEventArgs pArgs)
	{
		global::System.Diagnostics.Debug.Assert(ShouldRaiseEvent(ChoosingGroupHeaderContainer));

		ListViewBase spThis = this;

		ChoosingGroupHeaderContainer?.Invoke(spThis, pArgs);
	}

	private protected override void RaiseContainerContentChangingOnRecycle(
		UIElement container,
		object? item)
	{
		ContainerContentChangingEventArgs? args;
		ContainerContentChangingEventArgs argsConcrete;
		VirtualizationInformation? virtualizationInformation = container.GetVirtualizationInformationOrNull();

		if (virtualizationInformation is not null)
		{
			args = virtualizationInformation.GetBuildTreeArgs();
			argsConcrete = args;

			UIElement? templateRoot;
			bool wantAppCallback = false;

			argsConcrete.InRecycleQueue = true;
			argsConcrete.Phase = 0;
			argsConcrete.WantsCallBack = false;
			argsConcrete.Callback = null;
			argsConcrete.Item = item!;
			argsConcrete.SetBindingPhase(ContainerContentChangingEventArgs.NoBindingNeededToken);

			if (ShouldRaiseEvent(ContainerContentChanging))
			{
				ContainerContentChanging?.Invoke(this, args);
				wantAppCallback = args.WantsCallBack;
			}

			templateRoot = ((ContentControl)container).ContentTemplateRoot;
			if (templateRoot is not null)
			{
				IDataTemplateComponent? templateComponent = XamlBindingHelper.GetDataTemplateComponent(templateRoot);
				if (templateComponent is not null)
				{
					templateComponent.Recycle();
				}
				else
				{
					IDataTemplateExtension? extension = (templateRoot is FrameworkElement templateRootAsFE) ? DataTemplate.GetExtensionInstance(templateRootAsFE) : null;
					// raise for binding
					if (extension is not null)
					{
						extension.ResetTemplate();
					}
				}
			}

			if (wantAppCallback)
			{
				if (m_lowestPhaseInQueue == -1)
				{
					uint phaseArgs = 0;
					phaseArgs = args.Phase;

					// there was nothing registered
					m_lowestPhaseInQueue = phaseArgs;

					// that means we need to register ourselves with the buildtreeservice so that
					// we can get called back to do some work
					if (!m_isRegisteredForCallbacks)
					{
						BuildTreeService buildTree;
						buildTree = DXamlCore.Current.GetBuildTreeService();
						buildTree.RegisterWork(this);
					}
				}
			}
			else
			{
				argsConcrete.ResetLifetime();
			}
		}
	}
}

