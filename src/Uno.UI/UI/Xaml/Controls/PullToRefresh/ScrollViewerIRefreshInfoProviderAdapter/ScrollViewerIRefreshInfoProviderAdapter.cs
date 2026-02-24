#if !__ANDROID__ && !__IOS__
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerIRefreshInfoProviderAdapter.cpp, commit de78834

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Controls._Tracing;
using RefreshPullDirection = Microsoft.UI.Xaml.Controls.RefreshPullDirection;

#pragma warning disable CS0105 // Ignore duplicate namespace for WinUI build
using Microsoft.UI.Xaml.Controls;
#pragma warning restore CS0105

using Microsoft.UI.Input;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewerIRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	// The maximum initial scroll viewer offset allowed before PTR is disabled, and we enter the peeking state
	private const float INITIAL_OFFSET_THRESHOLD = 1.0f;

	// The farthest down the tree we are willing to search for a SV in the adapt from tree method
	private const int MAX_BFS_DEPTH = 10;

	// In the event that we call Refresh before having an implementation of IRefreshInfoProvider to tell us
	// what value to use as the execution ratio, we use this value instead.
	private const double FALLBACK_EXECUTION_RATIO = 0.8;

	// The ScrollViewerAdapter is responsible for creating an implementation of the IRefreshInfoProvider interface from a ScrollViewer. This
	// Is accomplished by attaching an interaction tracker to the scrollViewer's content presenter and wiring the PTR functionality up to that.
	~ScrollViewerIRefreshInfoProviderAdapter()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_scrollViewer != null)
		{
			CleanupScrollViewer();
		}

		if (m_infoProvider is { } IRefreshInfoProvider)
		{
			CleanupIRefreshInfoProvider();
		}
	}

	public ScrollViewerIRefreshInfoProviderAdapter(RefreshPullDirection refreshPullDirection, IAdapterAnimationHandler animationHandler)
	{
		////PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		m_refreshPullDirection = refreshPullDirection;
		if (animationHandler != null)
		{
			m_animationHandler = animationHandler;
		}
		else
		{
			m_animationHandler = new ScrollViewerIRefreshInfoProviderDefaultAnimationHandler(null, m_refreshPullDirection);
		}
	}

	public IRefreshInfoProvider AdaptFromTree(UIElement root, Size refreshVisualizerSize)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		UIElement winrtRoot = root;
		ScrollViewer rootAsSV = winrtRoot as ScrollViewer;
		int depth = 0;
		if (rootAsSV is not null)
		{
			return Adapt(winrtRoot as ScrollViewer, refreshVisualizerSize);
		}
		else
		{
			while (depth < MAX_BFS_DEPTH)
			{
				ScrollViewer helperResult = AdaptFromTreeRecursiveHelper(winrtRoot, depth);
				if (helperResult is not null)
				{
					return Adapt(helperResult, refreshVisualizerSize);
				}
				depth++;
			}
		}

		return null;
	}

	IRefreshInfoProvider Adapt(ScrollViewer adaptee, Size refreshVisualizerSize)
	{
		////PTR_TRACE_INFO(null, TRACE_MSG_METH_PTR, METH_NAME, this, adaptee);
		if (adaptee is null)
		{
			throw new ArgumentNullException(nameof(adaptee), "Adaptee cannot be null.");
		}

		if (m_scrollViewer is not null)
		{
			CleanupScrollViewer();
		}

		if (m_infoProvider is not null)
		{
			CleanupIRefreshInfoProvider();
		}

		m_infoProvider = null;
		m_interactionTracker = null;
		m_visualInteractionSource = null;
		m_scrollViewer = adaptee;


		if (m_scrollViewer.Content is null)
		{
			throw new InvalidOperationException("Adaptee's content property cannot be null.");
		}

		UIElement content = GetScrollContent();
		if (content is null)
		{
			throw new InvalidOperationException("Adaptee's content property must be a UIElement.");
		}

		var contentParent = VisualTreeHelper.GetParent(content);
		if (contentParent is null)
		{
			//If the Content property does not have a parent this likely means the OnLoaded event of the SV has not fired yet.
			//Attach to this event to finish the adaption.
			m_scrollViewer.Loaded += OnScrollViewerLoaded;
			m_scrollViewer_LoadedToken.Disposable = Disposable.Create(() => m_scrollViewer.Loaded -= OnScrollViewerLoaded);
		}
		else
		{
			OnScrollViewerLoaded(null, null);
			var contentParentAsUIElement = contentParent as UIElement;
			if (contentParentAsUIElement is null)
			{
				throw new InvalidOperationException("Adaptee's content's parent must be a UIElement.");
			}
		}
		Visual contentVisual = ElementCompositionPreview.GetElementVisual(content);
		Compositor compositor = contentVisual.Compositor;

		m_infoProvider = new RefreshInfoProviderImpl(m_refreshPullDirection, refreshVisualizerSize, compositor);

		m_infoProvider.RefreshStarted += OnRefreshStarted;
		m_infoProvider_RefreshStartedToken.Disposable = Disposable.Create(() => m_infoProvider.RefreshStarted -= OnRefreshStarted);
		m_infoProvider.RefreshCompleted += OnRefreshCompleted;
		m_infoProvider_RefreshCompletedToken.Disposable = Disposable.Create(() => m_infoProvider.RefreshCompleted -= OnRefreshCompleted);

		m_interactionTracker = InteractionTracker.CreateWithOwner(compositor, m_infoProvider as IInteractionTrackerOwner);

		m_interactionTracker.MinPosition = new System.Numerics.Vector3(0.0f);
		m_interactionTracker.MaxPosition = new System.Numerics.Vector3(0.0f);
		m_interactionTracker.MinScale = 1.0f;
		m_interactionTracker.MaxScale = 1.0f;

		if (m_visualInteractionSource is not null)
		{
			m_interactionTracker.InteractionSources.Add(m_visualInteractionSource);
			m_visualInteractionSourceIsAttached = true;
		}

		PointerEventHandler myEventHandler = (sender, args) =>
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, "ScrollViewer.PointerPressedHandler", this);
			if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch && m_visualInteractionSource is not null)
			{
				if (m_visualInteractionSourceIsAttached)
				{
					PointerPoint pp = args.GetCurrentPoint(null);

					if (pp is not null)
					{
						bool tryRedirectForManipulationSuccessful = true;

						try
						{
							//PTR_TRACE_INFO(null, TRACE_MSG_METH_METH, "ScrollViewer.PointerPressedHandler", this, "TryRedirectForManipulation");
							m_visualInteractionSource.TryRedirectForManipulation(pp);
						}
						catch (Exception)
						{
							// Swallowing Access Denied error because of InteractionTracker bug 17434718 which has been causing crashes at least in RS3, RS4 and RS5.
							//if (e.to_abi() != E_ACCESSDENIED)
							//{
							throw;
							//}

							//tryRedirectForManipulationSuccessful = false;
						}

						if (tryRedirectForManipulationSuccessful)
						{
							m_infoProvider.SetPeekingMode(!IsWithinOffsetThreshold());
						}
					}
				}
				else
				{
					throw new InvalidOperationException("Invalid IRefreshInfoProvider adaptation of scroll viewer, this can occur when calling TryRedirectForManipulation to an unattached visual interaction source.");
				}
			}
		};

		m_boxedPointerPressedEventHandler = new PointerEventHandler(myEventHandler);
		m_scrollViewer.AddHandler(UIElement.PointerPressedEvent, m_boxedPointerPressedEventHandler, true /* handledEventsToo */);
		m_scrollViewer.DirectManipulationCompleted += OnScrollViewerDirectManipulationCompleted;
		m_scrollViewer_DirectManipulationCompletedToken.Disposable = Disposable.Create(() => m_scrollViewer.DirectManipulationCompleted -= OnScrollViewerDirectManipulationCompleted);
		m_scrollViewer.ViewChanged += OnScrollViewerViewChanging; // Uno specific: Using ViewChanged as ViewChanging isn't implemented
		m_scrollViewer_ViewChangingToken.Disposable = Disposable.Create(() => m_scrollViewer.ViewChanged -= OnScrollViewerViewChanging);

		return m_infoProvider;
	}

	public void SetAnimations(UIElement refreshVisualizerContainer)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (refreshVisualizerContainer is null)
		{
			throw new InvalidOperationException("The refreshVisualizerContainer cannot be null.");
		}

		m_animationHandler.InteractionTrackerAnimation(refreshVisualizerContainer, GetScrollContent(), m_interactionTracker);
	}

	private void OnRefreshStarted(object sender, object args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		var content = GetScrollContent();
		if (content is not null)
		{
			// UNO TODO:
			//content.CancelDirectManipulations();
		}

		double executionRatio = FALLBACK_EXECUTION_RATIO;
		if (m_infoProvider is not null)
		{
			executionRatio = m_infoProvider.ExecutionRatio;
		}

		m_animationHandler.RefreshRequestedAnimation(null, content, executionRatio);
	}

	private void OnRefreshCompleted(object sender, object args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		m_animationHandler.RefreshCompletedAnimation(null, GetScrollContent());
	}

	private void OnScrollViewerLoaded(object sender, object args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		UIElement content = GetScrollContent();
		if (content is null)
		{
			throw new InvalidOperationException("Adaptee's content property must be a UIElement.");
		}

		var contentParent = VisualTreeHelper.GetParent(content);
		if (contentParent is null)
		{
			throw new InvalidOperationException("Adaptee cannot be null.");
		}

		UIElement contentParentAsUIElement = contentParent as UIElement;
		if (contentParentAsUIElement is null)
		{
			throw new InvalidOperationException("Adaptee's content's parent must be a UIElement.");
		}

		MakeInteractionSource(contentParentAsUIElement);

		m_scrollViewer_LoadedToken.Disposable = null;
	}

	private void OnScrollViewerDirectManipulationCompleted(object sender, object args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_infoProvider is not null)
		{
			m_infoProvider.UpdateIsInteractingForRefresh(false);
		}
	}

	private void OnScrollViewerViewChanging(object sender, Microsoft.UI.Xaml.Controls.ScrollViewerViewChangedEventArgs/*ScrollViewerViewChangingEventArgs*/ args)
	{
		if (m_infoProvider is not null && m_infoProvider.IsInteractingForRefresh)
		{
#if HAS_UNO
			// During overpan, any scroll back will trigger UpdateIsInteractingForRefresh which causes premature Refresh or Idle to occur.
			// We should prevent that while the user still had the touch held down (read: in InteractionTrackerInteractingState).
			if (m_interactionTracker?.State is InteractionTrackerInteractingState)
			{
				return;
			}
#endif

			//PTR_TRACE_INFO(null, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, args.FinalView().HorizontalOffset(), args.FinalView().VerticalOffset());
			if (!IsWithinOffsetThreshold())
			{
				//PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "No longer interacting for refresh due to ScrollViewer view change.");
				m_infoProvider.UpdateIsInteractingForRefresh(false);
			}
		}
	}

	private bool IsOrientationVertical()
	{
		return (m_refreshPullDirection == RefreshPullDirection.TopToBottom || m_refreshPullDirection == RefreshPullDirection.BottomToTop);
	}

	private UIElement GetScrollContent()
	{
		if (m_scrollViewer is not null)
		{
			var content = m_scrollViewer.Content;
			return content as UIElement;
		}
		return null;
	}

	private void MakeInteractionSource(UIElement contentParent)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		Visual contentParentVisual = ElementCompositionPreview.GetElementVisual(contentParent);

		m_visualInteractionSourceIsAttached = false;
		m_visualInteractionSource = VisualInteractionSource.Create(contentParentVisual);
		m_visualInteractionSource.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
		m_visualInteractionSource.ScaleSourceMode = InteractionSourceMode.Disabled;
		m_visualInteractionSource.PositionXSourceMode = IsOrientationVertical() ? InteractionSourceMode.Disabled : InteractionSourceMode.EnabledWithInertia;
		m_visualInteractionSource.PositionXChainingMode = IsOrientationVertical() ? InteractionChainingMode.Auto : InteractionChainingMode.Never;
		m_visualInteractionSource.PositionYSourceMode = IsOrientationVertical() ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled;
		m_visualInteractionSource.PositionYChainingMode = IsOrientationVertical() ? InteractionChainingMode.Never : InteractionChainingMode.Auto;

		if (m_interactionTracker is not null)
		{
			m_interactionTracker.InteractionSources.Add(m_visualInteractionSource);
			m_visualInteractionSourceIsAttached = true;
		}
	}

	private ScrollViewer AdaptFromTreeRecursiveHelper(DependencyObject root, int depth)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, depth);
		int numChildren = VisualTreeHelper.GetChildrenCount(root);
		if (depth == 0)
		{
			for (int i = 0; i < numChildren; i++)
			{
				DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
				ScrollViewer childObjectAsSV = childObject as ScrollViewer;
				if (childObjectAsSV is not null)
				{
					return childObjectAsSV;
				}
			}
			return null;
		}
		else
		{
			for (int i = 0; i < numChildren; i++)
			{
				DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
				ScrollViewer recursiveResult = AdaptFromTreeRecursiveHelper(childObject, depth - 1);
				if (recursiveResult is not null)
				{
					return recursiveResult;
				}
			}
			return null;
		}
	}

	private void CleanupScrollViewer()
	{
		var sv = m_scrollViewer;
		if (m_boxedPointerPressedEventHandler is not null)
		{
			sv.RemoveHandler(UIElement.PointerPressedEvent, m_boxedPointerPressedEventHandler);
			m_boxedPointerPressedEventHandler = null;
		}
		if (m_scrollViewer_DirectManipulationCompletedToken.Disposable is not null)
		{
			m_scrollViewer_DirectManipulationCompletedToken.Disposable = null;
		}
		if (m_scrollViewer_ViewChangingToken.Disposable is not null)
		{
			m_scrollViewer_ViewChangingToken.Disposable = null;
		}
		if (m_scrollViewer_LoadedToken.Disposable is not null)
		{
			m_scrollViewer_LoadedToken.Disposable = null;
		}
	}

	private void CleanupIRefreshInfoProvider()
	{
		var provider = m_infoProvider;
		m_infoProvider_RefreshStartedToken.Disposable = null;
		m_infoProvider_RefreshCompletedToken.Disposable = null;
	}

	private bool IsWithinOffsetThreshold()
	{
		switch (m_refreshPullDirection)
		{
			case RefreshPullDirection.TopToBottom:
				return m_scrollViewer.VerticalOffset < INITIAL_OFFSET_THRESHOLD;
			case RefreshPullDirection.BottomToTop:
				return m_scrollViewer.VerticalOffset > m_scrollViewer.ScrollableHeight - INITIAL_OFFSET_THRESHOLD;
			case RefreshPullDirection.LeftToRight:
				return m_scrollViewer.HorizontalOffset < INITIAL_OFFSET_THRESHOLD;
			case RefreshPullDirection.RightToLeft:
				return m_scrollViewer.HorizontalOffset > m_scrollViewer.ScrollableWidth - INITIAL_OFFSET_THRESHOLD;
			default:
				MUX_ASSERT(false);
				return false;
		}
	}

	public void Dispose() { }

#if HAS_UNO
	internal void SetupVisualizer(RefreshVisualizer visualizer)
	{
		if (visualizer is { })
		{
			visualizer.ScrollViewer = m_scrollViewer;
			visualizer.InteractionTracker = m_interactionTracker;
		}
	}
#endif
}
#endif
