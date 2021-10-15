using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Private.Controls
{
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
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

			m_refreshPullDirection = refreshPullDirection;
			if (animationHandler != null)
			{
				m_animationHandler = animationHandler;
			}
			else
			{
				m_animationHandler = new ScrollViewerIRefreshInfoProviderDefaultAnimationHandler(null, m_refreshPullDirection).as< IAdapterAnimationHandler > ();
			}
		}

		IRefreshInfoProvider AdaptFromTree(UIElement & root, Size & refreshVisualizerSize)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

			UIElement & winrtRoot = root;
			FxScrollViewer rootAsSV = winrtRoot as FxScrollViewer();
			int depth = 0;
			if (rootAsSV)
			{
				return Adapt(winrtRoot.as< FxScrollViewer > (), refreshVisualizerSize);
			}
			else
			{
				while (depth < MAX_BFS_DEPTH)
				{
					FxScrollViewer helperResult = AdaptFromTreeRecursiveHelper(winrtRoot, depth);
					if (helperResult)
					{
						return Adapt(helperResult, refreshVisualizerSize);
						break;
					}
					depth++;
				}
			}

			return null;
		}

		IRefreshInfoProvider Adapt(FxScrollViewer & adaptee, Size & refreshVisualizerSize)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_PTR, METH_NAME, this, adaptee);
			if (!adaptee)
			{
				throw hresult_invalid_argument("Adaptee cannot be null.");
			}

			if (m_scrollViewer)
			{
				CleanupScrollViewer();
			}

			if (m_infoProvider)
			{
				CleanupIRefreshInfoProvider();
			}

			m_infoProvider = null;
			m_interactionTracker = null;
			m_visualInteractionSource = null;
			m_scrollViewer = adaptee;


			if (!m_scrollViewer.Content())
			{
				throw hresult_invalid_argument("Adaptee's content property cannot be null.");
			}

			UIElement content = GetScrollContent();
			if (!content)
			{
				throw hresult_invalid_argument("Adaptee's content property must be a UIElement.");
			}

			var contentParent = VisualTreeHelper.GetParent(content);
			if (!contentParent)
			{
				//If the Content property does not have a parent this likely means the OnLoaded event of the SV has not fired yet.
				//Attach to this event to finish the adaption.
				m_scrollViewer_LoadedToken = m_scrollViewer.Loaded({ this, &ScrollViewerIRefreshInfoProviderAdapter.OnScrollViewerLoaded });
			}
			else
			{
				OnScrollViewerLoaded(null, null);
				UIElement contentParentAsUIElement = contentParent as UIElement();
				if (!contentParentAsUIElement)
				{
					throw hresult_invalid_argument("Adaptee's content's parent must be a UIElement.");
				}
			}
			Visual contentVisual = ElementCompositionPreview.GetElementVisual(content);
			Compositor compositor = contentVisual.Compositor();

			m_infoProvider = make_self<RefreshInfoProviderImpl>(m_refreshPullDirection, refreshVisualizerSize, compositor);

			m_infoProvider_RefreshStartedToken = m_infoProvider.RefreshStarted({ this, &ScrollViewerIRefreshInfoProviderAdapter.OnRefreshStarted });
			m_infoProvider_RefreshCompletedToken = m_infoProvider.RefreshCompleted({ this, &ScrollViewerIRefreshInfoProviderAdapter.OnRefreshCompleted });

			m_interactionTracker = InteractionTracker.CreateWithOwner(compositor, m_infoProvider.as< IInteractionTrackerOwner > ());

			m_interactionTracker.MinPosition(float3(0.0f));
			m_interactionTracker.MaxPosition(float3(0.0f));
			m_interactionTracker.MinScale(1.0f);
			m_interactionTracker.MaxScale(1.0f);

			if (m_visualInteractionSource)
			{
				m_interactionTracker.InteractionSources().Add(m_visualInteractionSource);
				m_visualInteractionSourceIsAttached = true;
			}

			PointerEventHandler myEventHandler =
		
			[=](var sender, var args)
		
	{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, "ScrollViewer.PointerPressedHandler", this);
				if (args.Pointer().PointerDeviceType() == PointerDeviceType.Touch && m_visualInteractionSource)
				{
					if (m_visualInteractionSourceIsAttached)
					{
						PointerPoint pp = args.GetCurrentPoint(null);

						if (pp)
						{
							bool tryRedirectForManipulationSuccessful = true;

							try
							{
								PTR_TRACE_INFO(null, TRACE_MSG_METH_METH, "ScrollViewer.PointerPressedHandler", this, "TryRedirectForManipulation");
								m_visualInteractionSource.TryRedirectForManipulation(pp);
							}
							catch (hresult_error&e)
                    {
								// Swallowing Access Denied error because of InteractionTracker bug 17434718 which has been causing crashes at least in RS3, RS4 and RS5.
								if (e.to_abi() != E_ACCESSDENIED)
								{
									throw;
								}

								tryRedirectForManipulationSuccessful = false;
							}

							if (tryRedirectForManipulationSuccessful)
							{
								m_infoProvider.SetPeekingMode(!IsWithinOffsetThreshold());
							}
							}
						}
						else
						{
							throw hresult_invalid_argument("Invalid IRefreshInfoProvider adaptation of scroll viewer, this can occur when calling TryRedirectForManipulation to an unattached visual interaction source.");
						}
					}
				};

				m_boxedPointerPressedEventHandler = box_value<PointerEventHandler>(myEventHandler);
				m_scrollViewer.AddHandler(UIElement.PointerPressedEvent(), m_boxedPointerPressedEventHandler, true /* handledEventsToo */);
				m_scrollViewer_DirectManipulationCompletedToken = m_scrollViewer.DirectManipulationCompleted({ this, &ScrollViewerIRefreshInfoProviderAdapter.OnScrollViewerDirectManipulationCompleted });
				m_scrollViewer_ViewChangingToken = m_scrollViewer.ViewChanging({ this, &ScrollViewerIRefreshInfoProviderAdapter.OnScrollViewerViewChanging });

				return m_infoProvider.as< IRefreshInfoProvider > ();
			}

			void SetAnimations(UIElement &refreshVisualizerContainer)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				if (!refreshVisualizerContainer)
				{
					throw hresult_invalid_argument("The refreshVisualizerContainer cannot be null.");
				}

				m_animationHandler.InteractionTrackerAnimation(refreshVisualizerContainer, GetScrollContent(), m_interactionTracker);
			}

			void OnRefreshStarted(object& /*sender*/,  object& /*args*/)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				var content = GetScrollContent();
				if (content)
				{
					content.CancelDirectManipulations();
				}

				double executionRatio = FALLBACK_EXECUTION_RATIO;
				if (m_infoProvider)
				{
					executionRatio = m_infoProvider.ExecutionRatio();
				}

				m_animationHandler.RefreshRequestedAnimation(null, content, executionRatio);
			}

			void OnRefreshCompleted(object& /*sender*/,  object& /*args*/)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				m_animationHandler.RefreshCompletedAnimation(null, GetScrollContent());
			}

			void OnScrollViewerLoaded(object& /*sender*/,  object& /*args*/)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				UIElement content = GetScrollContent();
				if (!content)
				{
					throw hresult_invalid_argument("Adaptee's content property must be a UIElement.");
				}

				var contentParent = VisualTreeHelper.GetParent(content);
				if (!contentParent)
				{
					throw hresult_invalid_argument("Adaptee cannot be null.");
				}

				UIElement contentParentAsUIElement = contentParent as UIElement();
				if (!contentParentAsUIElement)
				{
					throw hresult_invalid_argument("Adaptee's content's parent must be a UIElement.");
				}

				MakeInteractionSource(contentParentAsUIElement);

				m_scrollViewer.Loaded(m_scrollViewer_LoadedToken);
			}

			void OnScrollViewerDirectManipulationCompleted(object& /*sender*/,  object& /*args*/)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				if (m_infoProvider)
				{
					m_infoProvider.UpdateIsInteractingForRefresh(false);
				}
			}

			void OnScrollViewerViewChanging(object& /*sender*/,  Windows.UI.Xaml.Controls.ScrollViewerViewChangingEventArgs & args)
{
				if (m_infoProvider && m_infoProvider.IsInteractingForRefresh())
				{
					PTR_TRACE_INFO(null, TRACE_MSG_METH_DBL_DBL, METH_NAME, this, args.FinalView().HorizontalOffset(), args.FinalView().VerticalOffset());
					if (!IsWithinOffsetThreshold())
					{
						PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "No longer interacting for refresh due to ScrollViewer view change.");
						m_infoProvider.UpdateIsInteractingForRefresh(false);
					}
				}
			}

			bool IsOrientationVertical()
			{
				return (m_refreshPullDirection == RefreshPullDirection.TopToBottom || m_refreshPullDirection == RefreshPullDirection.BottomToTop);
			}

			UIElement GetScrollContent()
			{
				if (m_scrollViewer)
				{
					var content = m_scrollViewer.Content();
					return content as UIElement();
				}
				return null;
			}

			void MakeInteractionSource(UIElement&contentParent)
{
				PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
				Visual contentParentVisual = ElementCompositionPreview.GetElementVisual(contentParent);

				m_visualInteractionSourceIsAttached = false;
				m_visualInteractionSource = VisualInteractionSource.Create(contentParentVisual);
				m_visualInteractionSource.ManipulationRedirectionMode(VisualInteractionSourceRedirectionMode.CapableTouchpadOnly);
				m_visualInteractionSource.ScaleSourceMode(InteractionSourceMode.Disabled);
				m_visualInteractionSource.PositionXSourceMode(IsOrientationVertical() ? InteractionSourceMode.Disabled : InteractionSourceMode.EnabledWithInertia);
				m_visualInteractionSource.PositionXChainingMode(IsOrientationVertical() ? InteractionChainingMode.Auto : InteractionChainingMode.Never);
				m_visualInteractionSource.PositionYSourceMode(IsOrientationVertical() ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled);
				m_visualInteractionSource.PositionYChainingMode(IsOrientationVertical() ? InteractionChainingMode.Never : InteractionChainingMode.Auto);

				if (m_interactionTracker)
				{
					m_interactionTracker.InteractionSources().Add(m_visualInteractionSource);
					m_visualInteractionSourceIsAttached = true;
				}
			}

			FxScrollViewer AdaptFromTreeRecursiveHelper(DependencyObject root, int depth)
			{
				PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, depth);
				int numChildren = VisualTreeHelper.GetChildrenCount(root);
				if (depth == 0)
				{
					for (int i = 0; i < numChildren; i++)
					{
						DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
						FxScrollViewer childObjectAsSV = childObject as FxScrollViewer();
						if (childObjectAsSV)
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
						FxScrollViewer recursiveResult = AdaptFromTreeRecursiveHelper(childObject, depth - 1);
						if (recursiveResult)
						{
							return recursiveResult;
						}
					}
					return null;
				}
			}

			void CleanupScrollViewer()
			{
				var sv = m_scrollViewer;
				if (m_boxedPointerPressedEventHandler)
				{
					sv.RemoveHandler(UIElement.PointerPressedEvent(), m_boxedPointerPressedEventHandler);
					m_boxedPointerPressedEventHandler = null;
				}
				if (m_scrollViewer_DirectManipulationCompletedToken.value)
				{
					sv.DirectManipulationCompleted(m_scrollViewer_DirectManipulationCompletedToken);
					m_scrollViewer_DirectManipulationCompletedToken.value = 0;
				}
				if (m_scrollViewer_ViewChangingToken.value)
				{
					sv.ViewChanging(m_scrollViewer_ViewChangingToken);
					m_scrollViewer_ViewChangingToken.value = 0;
				}
				if (m_scrollViewer_LoadedToken.value)
				{
					sv.Loaded(m_scrollViewer_LoadedToken);
					m_scrollViewer_LoadedToken.value = 0;
				}
			}

			void CleanupIRefreshInfoProvider()
			{
				var provider = m_infoProvider;
				if (m_infoProvider_RefreshStartedToken.value)
				{
					provider.RefreshStarted(m_infoProvider_RefreshStartedToken);
					m_infoProvider_RefreshStartedToken.value = 0;
				}
				if (m_infoProvider_RefreshCompletedToken.value)
				{
					provider.RefreshCompleted(m_infoProvider_RefreshCompletedToken);
					m_infoProvider_RefreshCompletedToken.value = 0;
				}
			}

			bool IsWithinOffsetThreshold()
			{
				switch (m_refreshPullDirection)
				{
					case RefreshPullDirection.TopToBottom:
						return m_scrollViewer.VerticalOffset() < INITIAL_OFFSET_THRESHOLD;
					case RefreshPullDirection.BottomToTop:
						return m_scrollViewer.VerticalOffset() > m_scrollViewer.ScrollableHeight() - INITIAL_OFFSET_THRESHOLD;
					case RefreshPullDirection.LeftToRight:
						return m_scrollViewer.HorizontalOffset() < INITIAL_OFFSET_THRESHOLD;
					case RefreshPullDirection.RightToLeft:
						return m_scrollViewer.HorizontalOffset() > m_scrollViewer.ScrollableWidth() - INITIAL_OFFSET_THRESHOLD;
					default:
						MUX_ASSERT(false);
						return false;
				}
			}
		}
	}
