// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Imported in uno on 2021/03/21 from commit 307bd99682cccaa128483036b764c0b7c862d666
// https://github.com/microsoft/microsoft-ui-xaml/blob/307bd99682cccaa128483036b764c0b7c862d666/dev/SwipeControl/SwipeControl.cpp

using System;
using System.Numerics;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Composition;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Extensions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.UI;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SwipeControl : ContentControl
	{

		// Change to 'true' to turn on debugging outputs in Output window
		//bool SwipeControlTrace.s_IsDebugOutputEnabled = false;
		//bool SwipeControlTrace.s_IsVerboseDebugOutputEnabled = false;

		private const double c_epsilon = 0.0001;
		private const float c_ThresholdValue = 100.0f;
		private const float c_MinimumCloseVelocity = 31.0f;

		[ThreadStatic]
		static WeakReference<SwipeControl> s_lastInteractedWithSwipeControl = null;

		public SwipeControl()
		{
			s_lastInteractedWithSwipeControl ??= new WeakReference<SwipeControl>(null);

			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_SwipeControl);
			this.SetDefaultStyleKey();

			Loaded += UnfinalizeOnLoad;
			Unloaded += FinalizeOnUnload;
		}

		// [BEGIN] Uno workaround:
		//  + we make sure to un-subscribe from events on unload to avoid leaks
		//  + we must not use finalizer on uno (invoked from a bg thread)

		private static void UnfinalizeOnLoad(object sender, RoutedEventArgs routedEventArgs)
			=> ((SwipeControl)sender).SwipeControl_Unfinalizer();

		private static void FinalizeOnUnload(object sender, RoutedEventArgs routedEventArgs)
			=> ((SwipeControl)sender).SwipeControl_Finalizer();

		private void SwipeControl_Unfinalizer()
		{
			if (_isReady)
			{
				DetachEventHandlers(); // Do not double subscribe
				AttachEventHandlers(isUnoUnfinalizer: true);
			}
		}

		private void SwipeControl_Finalizer()
		//~SwipeControl()
		// [END] Uno workaround
		{
			DetachEventHandlers();

			if (s_lastInteractedWithSwipeControl.TryGetTarget(out var lastInteractedWithSwipeControl))
			{
				if (lastInteractedWithSwipeControl == this)
				{
					s_lastInteractedWithSwipeControl.SetTarget(null);
					var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
					if (globalTestHooks is { })
					{
						globalTestHooks.NotifyLastInteractedWithSwipeControlChanged();
					}
				}
			}
		}

		#region ISwipeControl
		public async void Close()
		{
			//CheckThread();
			try
			{

				if (m_isOpen && !m_lastActionWasClosing && !m_isInteracting)
				{
					m_lastActionWasClosing = true;

					//Uno workaround:
					m_isInteracting = true;
					_desiredPosition = Vector2.Zero;
					UpdateStackPanelDesiredPosition();

					// This delay is to allow users to see the fully open state before it closes back.
					await Task.Delay(TimeSpan.FromSeconds(0.250));

					await AnimateTransforms(false, 0d);
					OnSwipeManipulationCompleted(this, null);

					//if (!m_isIdle)
					//{
					//	Vector3 initialPosition = default;
					//	switch (m_createdContent)
					//	{
					//		case CreatedContent.Left:
					//			initialPosition.X = (float)(-m_swipeContentStackPanel.ActualWidth);
					//			break;
					//		case CreatedContent.Top:
					//			initialPosition.Y = (float)(-m_swipeContentStackPanel.ActualHeight);
					//			break;
					//		case CreatedContent.Right:
					//			initialPosition.X = (float)(m_swipeContentStackPanel.ActualWidth);
					//			break;
					//		case CreatedContent.Bottom:
					//			initialPosition.Y = (float)(m_swipeContentStackPanel.ActualHeight);
					//			break;
					//		case CreatedContent.None:
					//			break;
					//		default:
					//			global::System.Diagnostics.Debug.Assert(false);
					//			break;
					//	}

					//m_interactionTracker.TryUpdatePosition(initialPosition);
					//}

					//Vector3 addedVelocity = default;
					//switch (m_createdContent)
					//{
					//	case CreatedContent.Left:
					//		addedVelocity.X = c_MinimumCloseVelocity;
					//		break;
					//	case CreatedContent.Top:
					//		addedVelocity.Y = c_MinimumCloseVelocity;
					//		break;
					//	case CreatedContent.Right:
					//		addedVelocity.X = -c_MinimumCloseVelocity;
					//		break;
					//	case CreatedContent.Bottom:
					//		addedVelocity.Y = -c_MinimumCloseVelocity;
					//		break;
					//	case CreatedContent.None:
					//		break;
					//	default:
					//		global::System.Diagnostics.Debug.Assert(false);
					//		break;
					//}

					//m_interactionTracker.TryUpdatePositionWithAdditionalVelocity(addedVelocity);
				}
			}
			catch (Exception ex)
			{
				Application.Current.RaiseRecoverableUnhandledException(ex);
			}
		}
		#endregion

		#region FrameworkElementOverrides
		protected override void OnApplyTemplate()
		{
			ThrowIfHasVerticalAndHorizontalContent(setIsHorizontal: true);

			DetachEventHandlers();
			GetTemplateParts();
			EnsureClip();
			AttachEventHandlers();

			_isReady = true;
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;
			if (property == LeftItemsProperty)
			{
				OnLeftItemsCollectionChanged(args);
			}

			if (property == RightItemsProperty)
			{
				OnRightItemsCollectionChanged(args);
			}

			if (property == TopItemsProperty)
			{
				OnTopItemsCollectionChanged(args);
			}

			if (property == BottomItemsProperty)
			{
				OnBottomItemsCollectionChanged(args);
			}
		}

		//Swipe control is usually placed in a list view item. When this is the case the swipe item needs to be the same size as the list view item.
		//This is to ensure that swiping from anywhere on the list view item causes pointer pressed events in the SwipeControl.  Without this measure
		//override it is usually not the case that swipe control will fill the available space.  This is because list view item is a content control
		//and those by convension only provide it's children space for at most their desired size. However list view item itself will take up a different
		//ammount of space. In the past we solved this issue by requiring the list view item to have the HorizontalContentAlignment and VerticalContentAlignment
		//set to stretch. This property changes the measure cycle to give as much space as possible to the list view items children.  Instead we can
		//just do this ourselves in this measure override and prevent the confusing need for properties set on the parent of swipe control to use it at all.
		protected override Size MeasureOverride(Size availableSize)
		{
			base.MeasureOverride(availableSize);

			m_rootGrid.Measure(availableSize);
			Size contentDesiredSize = m_rootGrid.DesiredSize;
			if (!double.IsInfinity(availableSize.Width))
			{
				contentDesiredSize.Width = availableSize.Width;
			}

			if (!double.IsInfinity(availableSize.Height))
			{
				contentDesiredSize.Height = availableSize.Height;
			}

			return contentDesiredSize;
		}
		#endregion

		#region IInteractionTrackerOwner
		// Uno workaround: Interaction tracker is not supported yet, use Manipulation events instead
#if false

		void CustomAnimationStateEntered(
			InteractionTracker sender,
			InteractionTrackerCustomAnimationStateEnteredArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			m_isInteracting = true;

			if (m_isIdle)
			{
				m_isIdle = false;
				if (var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks())
				{
					globalTestHooks.NotifyIdleStatusChanged(this);
				}
			}
		}

		void RequestIgnored(
			InteractionTracker sender,

		InteractionTrackerRequestIgnoredArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);
		}

		void IdleStateEntered(
			InteractionTracker sender,

		InteractionTrackerIdleStateEnteredArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			m_isInteracting = false;
			UpdateIsOpen(m_interactionTracker.Position() != float3.zero());

			if (m_isOpen)
			{
				if (m_currentItems && m_currentItems.Mode == SwipeMode.Execute && m_currentItems.Size() > 0)
				{
					var swipeItem = (SwipeItem)(m_currentItems.GetAt(0));
					get_self<SwipeItem>(swipeItem).InvokeSwipe(this);
				}
			}
			else
			{
				if (var swipeContentStackPanel = m_swipeContentStackPanel)
				{
					swipeContentStackPanel.Background(null);
					if (var swipeContentStackPanelChildren = swipeContentStackPanel.Children)
					{
						swipeContentStackPanelChildren.Clear();
					}
				}
				if (var swipeContentRoot = m_swipeContentRoot)
				{
					swipeContentRoot.Background(null);
				}

				m_currentItems.set(null);
				m_createdContent = CreatedContent.None;
			}

			if (!m_isIdle)
			{
				m_isIdle = true;
				if (var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks())
				{
					globalTestHooks.NotifyIdleStatusChanged(this);
				}
			}
		}

		void InteractingStateEntered(
			InteractionTracker sender,

		InteractionTrackerInteractingStateEnteredArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_isIdle)
			{
				m_isIdle = false;
				if (var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks())
				{
					globalTestHooks.NotifyIdleStatusChanged(this);
				}
			}

			m_lastActionWasClosing = false;
			m_lastActionWasOpening = false;
			m_isInteracting = true;

			//Once the user has started interacting with a SwipeControl in the closed state we are free to unblock contents.
			//Contents of items opposite the currently opened ones will not be created.
			if (!m_isOpen)
			{
				m_blockNearContent = false;
				m_blockFarContent = false;
				m_interactionTracker.Properties.InsertBoolean(s_blockNearContentPropertyName, false);
				m_interactionTracker.Properties.InsertBoolean(s_blockFarContentPropertyName, false);
			}
		}

		void InertiaStateEntered(
			InteractionTracker sender,

		InteractionTrackerInertiaStateEnteredArgs & args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			m_isInteracting = false;

			if (m_isIdle)
			{
				m_isIdle = false;
				if (var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks())
				{
					globalTestHooks.NotifyIdleStatusChanged(this);
				}
			}

			//It is possible that the user has flicked from a negative position to a position that would result in the interaction
			//tracker coming to rest at the positive open position (or vise versa). The != zero check does not account for this.
			//Instead we check to ensure that the current position and the ModifiedRestingPosition have the same sign (multiply to a positive number)
			//If they do not then we are in this situation and want the end result of the interaction to be the closed state, so close without any animation and return
			//to prevent further processing of this inertia state.
			var flickToOppositeSideCheck = m_interactionTracker.Position() * args.ModifiedRestingPosition().Value();
			if (m_isHorizontal ? flickToOppositeSideCheck.x < 0 : flickToOppositeSideCheck.y < 0)
			{
				CloseWithoutAnimation();
				return;
			}

			UpdateIsOpen(args.ModifiedRestingPosition().Value() != float3.zero());
			// If the user has panned the interaction tracker past 0 in the opposite direction of the previously
			// opened swipe items then when we set m_isOpen to true the animations will snap to that value.
			// To avoid this we block that side of the animation until the interacting state is entered.
			if (m_isOpen)
			{
				switch (m_createdContent)
				{
					case CreatedContent.Bottom:
					case CreatedContent.Right:
						m_blockNearContent = true;
						m_blockFarContent = false;
						m_interactionTracker.Properties.InsertBoolean(s_blockNearContentPropertyName, true);
						m_interactionTracker.Properties.InsertBoolean(s_blockFarContentPropertyName, false);
						break;
					case CreatedContent.Top:
					case CreatedContent.Left:
						m_blockNearContent = false;
						m_blockFarContent = true;
						m_interactionTracker.Properties.InsertBoolean(s_blockNearContentPropertyName, false);
						m_interactionTracker.Properties.InsertBoolean(s_blockFarContentPropertyName, true);
						break;
					case CreatedContent.None:
						m_blockNearContent = false;
						m_blockFarContent = false;
						m_interactionTracker.Properties.InsertBoolean(s_blockNearContentPropertyName, false);
						m_interactionTracker.Properties.InsertBoolean(s_blockFarContentPropertyName, false);
						break;
					default:
						assert(false);
				}
			}
		}

		void ValuesChanged(
			InteractionTracker sender,

		InteractionTrackerValuesChangedArgs & args)
		{
			SWIPECONTROL_TRACE_VERBOSE(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			var lastInteractedWithSwipeControl = s_lastInteractedWithSwipeControl;
			if (m_isInteracting && (!lastInteractedWithSwipeControl || lastInteractedWithSwipeControl != this))
			{
				if (lastInteractedWithSwipeControl)
				{
					lastInteractedWithSwipeControl.CloseIfNotRemainOpenExecuteItem();
				}

				s_lastInteractedWithSwipeControl = get_weak();

				if (var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks())
				{
					globalTestHooks.NotifyLastInteractedWithSwipeControlChanged();
				}
			}

			float value = 0.0f;

			if (m_isHorizontal)
			{
				value = args.Position().x;
				if (!m_blockNearContent && m_createdContent != CreatedContent.Left && value < -c_epsilon)
				{
					CreateLeftContent();
				}
				else if (!m_blockFarContent && m_createdContent != CreatedContent.Right && value > c_epsilon)
				{
					CreateRightContent();
				}
			}
			else
			{
				value = args.Position().y;
				if (!m_blockNearContent && m_createdContent != CreatedContent.Top && value < -c_epsilon)
				{
					CreateTopContent();
				}
				else if (!m_blockFarContent && m_createdContent != CreatedContent.Bottom && value > c_epsilon)
				{
					CreateBottomContent();
				}
			}

			UpdateThresholdReached(value);
		}
#endif
		#endregion

		#region TestHookHelpers
		internal static SwipeControl GetLastInteractedWithSwipeControl()
		{
			if (s_lastInteractedWithSwipeControl.TryGetTarget(out var lastInteractedWithSwipeControl))
			{
				return lastInteractedWithSwipeControl;
			}
			return null;
		}

		internal bool GetIsOpen()
		{
			return m_isOpen;
		}

		internal bool GetIsIdle()
		{
			return m_isIdle;
		}
		#endregion

		private void OnLeftItemsCollectionChanged(DependencyPropertyChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (args.OldValue is { })
			{
				var observableVector = args.NewValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged -= OnLeftItemsChanged;
			}

			if (args.NewValue is { })
			{
				ThrowIfHasVerticalAndHorizontalContent();
				var observableVector = args.NewValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged += OnLeftItemsChanged;
			}

			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasLeftContentPropertyName, args.NewValue is {} && (args.NewValue as IObservableVector<SwipeItem>).Count > 0);
			//}
			// Uno workaround:
			_hasLeftContent = args.NewValue is { } && (args.NewValue as IObservableVector<SwipeItem>).Count > 0;

			if (m_createdContent == CreatedContent.Left)
			{
				CreateLeftContent();
			}
		}

		private void OnRightItemsCollectionChanged(DependencyPropertyChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (args.OldValue is { })
			{
				var observableVector = args.OldValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged -= OnRightItemsChanged;
			}

			if (args.NewValue is { })
			{
				ThrowIfHasVerticalAndHorizontalContent();
				var observableVector = args.NewValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged += OnRightItemsChanged;
			}

			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasRightContentPropertyName, args.NewValue is {} && (args.NewValue as IObservableVector<SwipeItem>).Count > 0);
			//}
			// Uno workaround:
			_hasRightContent = args.NewValue is { } && (args.NewValue as IObservableVector<SwipeItem>).Count > 0;

			if (m_createdContent == CreatedContent.Right)
			{
				CreateRightContent();
			}
		}

		private void OnTopItemsCollectionChanged(DependencyPropertyChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (args.OldValue is { })
			{
				var observableVector = args.OldValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged -= OnTopItemsChanged;
			}

			if (args.NewValue is { })
			{
				ThrowIfHasVerticalAndHorizontalContent();
				var observableVector = args.NewValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged += OnTopItemsChanged;
			}

			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasTopContentPropertyName, args.NewValue is {} && (args.NewValue as IObservableVector<SwipeItem>).Count > 0);
			//}
			// Uno workaround:
			_hasTopContent = args.NewValue is { } && (args.NewValue as IObservableVector<SwipeItem>).Count > 0;

			if (m_createdContent == CreatedContent.Top)
			{
				CreateTopContent();
			}
		}

		private void OnBottomItemsCollectionChanged(DependencyPropertyChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (args.OldValue is { })
			{
				var observableVector = args.OldValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged -= OnBottomItemsChanged;
			}

			if (args.NewValue is { })
			{
				ThrowIfHasVerticalAndHorizontalContent();
				var observableVector = args.NewValue as IObservableVector<SwipeItem>;
				observableVector.VectorChanged += OnBottomItemsChanged;
			}

			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasBottomContentPropertyName, args.NewValue is {} && (args.NewValue as IObservableVector<SwipeItem>).Count > 0);
			//}
			// Uno workaround:
			_hasBottomContent = args.NewValue is { } && (args.NewValue as IObservableVector<SwipeItem>).Count > 0;

			if (m_createdContent == CreatedContent.Bottom)
			{
				CreateBottomContent();
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (!m_hasInitialLoadedEventFired)
			{
				m_hasInitialLoadedEventFired = true;
				InitializeInteractionTracker();
				TryGetSwipeVisuals();
			}

			//If the swipe control has been added to the tree for a subsequent time, for instance when a list view item has been recycled,
			//Ensure that we are in the closed interaction tracker state.
			CloseWithoutAnimation();
		}

		private void AttachEventHandlers(bool isUnoUnfinalizer = false)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			//global::System.Diagnostics.Debug.Assert(m_loadedToken.value == 0);
			if (isUnoUnfinalizer) // Uno workaround: We detach from event on unload and re-attach on loaded
			{
				OnLoaded(this, null);
			}
			else
			{
				Loaded += OnLoaded;
				m_hasInitialLoadedEventFired = false;
			}

			//global::System.Diagnostics.Debug.Assert(m_onSizeChangedToken.value == 0);
			SizeChanged += OnSizeChanged;

			//global::System.Diagnostics.Debug.Assert(m_onSwipeContentStackPanelSizeChangedToken.value == 0);
			m_swipeContentStackPanel.SizeChanged += OnSwipeContentStackPanelSizeChanged;

			// also get any action from any inside button, or a clickable/tappable control
			if (m_onPointerPressedEventHandler is null)
			{
				m_onPointerPressedEventHandler = OnPointerPressedEvent;
				AddHandler(UIElement.PointerPressedEvent, m_onPointerPressedEventHandler, true);
			}

			//global::System.Diagnostics.Debug.Assert(m_inputEaterTappedToken.value == 0);
			m_inputEater.Tapped += InputEaterGridTapped;

			// Uno workaround:
			UnoAttachEventHandlers();
		}

		private void DetachEventHandlers()
		{
			SWIPECONTROL_TRACE_INFO(null/*, TRACE_MSG_METH, METH_NAME, this*/);

			Loaded -= OnLoaded;

			SizeChanged -= OnSizeChanged;

			// Uno workaround: Add null check because m_swipeContentStackPanel is set later.
			if (m_swipeContentStackPanel != null)
			{
				m_swipeContentStackPanel.SizeChanged -= OnSwipeContentStackPanelSizeChanged;
			}

			if (m_onPointerPressedEventHandler is { })
			{
				RemoveHandler(UIElement.PointerPressedEvent, m_onPointerPressedEventHandler);
				m_onPointerPressedEventHandler = null;
			}

			if (m_inputEater is { })
			{
				m_inputEater.Tapped -= InputEaterGridTapped;
			}

			DetachDismissingHandlers();

			// Uno workaround:
			UnoDetachEventHandlers();
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs args)
		{
			EnsureClip();
			foreach (var uiElement in m_swipeContentStackPanel.GetChildren())
			{
				AppBarButton appBarButton = uiElement as AppBarButton;
				if (appBarButton is { })
				{
					if (m_isHorizontal)
					{
						appBarButton.Height = ActualHeight;
						if (m_currentItems is { } && m_currentItems.Mode == SwipeMode.Execute)
						{
							appBarButton.Width = ActualWidth;
						}
					}
					else
					{
						appBarButton.Width = ActualWidth;
						if (m_currentItems is { } && m_currentItems.Mode == SwipeMode.Execute)
						{
							appBarButton.Height = ActualHeight;
						}
					}
				}
			}
		}

		private void OnSwipeContentStackPanelSizeChanged(object sender, SizeChangedEventArgs args)
		{
			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.MinPosition = new Vector3(
			//		(float)-args.NewSize.Width, (float)-args.NewSize.Height, 0.0f
			//	);
			//	m_interactionTracker.MaxPosition = new Vector3(
			//		(float)args.NewSize.Width, (float)args.NewSize.Height, 0.0f
			//	);
			//	ConfigurePositionInertiaRestingValues();
			//}
		}

		private void OnPointerPressedEvent(
			object sender,
			PointerRoutedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch /*&& m_visualInteractionSource is {}*/)
			{
				if (m_currentItems is { } &&
					m_currentItems.Mode == SwipeMode.Execute &&
					m_currentItems.Size > 0 &&
					m_currentItems.GetAt(0).BehaviorOnInvoked == SwipeBehaviorOnInvoked.RemainOpen &&
					m_isOpen)
				{
					//If the swipe control is currently open on an Execute item's who's behaviorOnInvoked property is set to RemainOpen
					//we don't want to allow the user interaction to effect the swipe control anymore, so don't redirect the manipulation
					//to the interaction tracker.
					return;
				}

				//try
				//{
				//	m_visualInteractionSource.TryRedirectForManipulation(args.GetCurrentPoint(this));
				//}
				//catch (Exception e)
				//{
				//	// Swallowing Access Denied error because of InteractionTracker bug 17434718 which has been
				//	// causing crashes at least in RS3, RS4 and RS5.
				//	if (e.to_abi() != E_ACCESSDENIED)
				//	{
				//		throw;
				//	}
				//}
			}
		}

		private void InputEaterGridTapped(object sender, TappedRoutedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_isOpen)
			{
				CloseIfNotRemainOpenExecuteItem();
				args.Handled = true;
			}
		}

		private void AttachDismissingHandlers()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			DetachDismissingHandlers();

			//if (UIElement10 uiElement10 = this)
			//{
			var xamlRoot = this.XamlRoot;
			if (xamlRoot is { })
			{
				if (xamlRoot.Content is { } xamlRootContent)
				{
					var handler = new PointerEventHandler((_, args) =>
					{
						DismissSwipeOnAnExternalTap(args.GetCurrentPoint(null).Position);
					});
					xamlRootContent.AddHandler(PointerPressedEvent, handler, true);
					m_xamlRootPointerPressedEventRevoker = Disposable.Create(() => xamlRootContent.RemoveHandler(PointerPressedEvent, handler));

					var keyHandler = new KeyEventHandler((_, arg) =>
					{
						CloseIfNotRemainOpenExecuteItem();
					});
					xamlRootContent.AddHandler(KeyDownEvent, handler, true);
					m_xamlRootKeyDownEventRevoker = Disposable.Create(() => xamlRootContent.RemoveHandler(PointerPressedEvent, handler));
				}

				xamlRoot.Changed += CurrentXamlRootChanged;
				m_xamlRootChangedRevoker = Disposable.Create(() => xamlRoot.Changed -= CurrentXamlRootChanged);
			}
			//}
			//else
			//{
			//	if (var currentWindow = Window.Current())
			//	{
			//		if (var coreWindow = currentWindow.CoreWindow())
			//		{
			//			m_coreWindowPointerPressedRevoker = coreWindow.PointerPressed(auto_revoke,  {
			//				this, DismissSwipeOnAnExternalCoreWindowTap
			//			});
			//			m_coreWindowKeyDownRevoker = coreWindow.KeyDown(auto_revoke,  {
			//				this, DismissSwipeOnCoreWindowKeyDown
			//			});
			//			m_windowMinimizeRevoker = coreWindow.VisibilityChanged(auto_revoke,  {
			//				this, CurrentWindowVisibilityChanged
			//			});
			//			m_windowSizeChangedRevoker = currentWindow.SizeChanged(auto_revoke,  {
			//				this, CurrentWindowSizeChanged
			//			});
			//		}
			//	}
			//}

			if (CoreWindow.GetForCurrentThreadSafe() is { } coreWindow)
			{
				if (coreWindow.Dispatcher is { } dispatcher)
				{
					dispatcher.AcceleratorKeyActivated += DismissSwipeOnAcceleratorKeyActivator;
					m_acceleratorKeyActivatedRevoker = Disposable.Create(() => dispatcher.AcceleratorKeyActivated -= DismissSwipeOnAcceleratorKeyActivator);
				}
			}
		}

		private void DetachDismissingHandlers()
		{
			SWIPECONTROL_TRACE_INFO(null/*, TRACE_MSG_METH, METH_NAME, this*/);

			m_xamlRootPointerPressedEventRevoker?.Dispose();
			m_xamlRootKeyDownEventRevoker?.Dispose();
			m_xamlRootChangedRevoker?.Dispose();

			m_acceleratorKeyActivatedRevoker?.Dispose();
			//m_coreWindowPointerPressedRevoker?.Dispose();
			//m_coreWindowKeyDownRevoker?.Dispose();
			//m_windowMinimizeRevoker?.Dispose();
			//m_windowSizeChangedRevoker?.Dispose();
		}

		private void DismissSwipeOnAcceleratorKeyActivator(CoreDispatcher sender, AcceleratorKeyEventArgs args)
		{
			CloseIfNotRemainOpenExecuteItem();
		}

		private void CurrentXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
		{
			CloseIfNotRemainOpenExecuteItem();
		}

#if false
		private void DismissSwipeOnCoreWindowKeyDown(CoreWindow sender, KeyEventArgs args)
		{
			CloseIfNotRemainOpenExecuteItem();
		}

		private void CurrentWindowSizeChanged(DependencyObject sender, WindowSizeChangedEventArgs args)
		{
			CloseIfNotRemainOpenExecuteItem();
		}

		private void CurrentWindowVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
		{
			CloseIfNotRemainOpenExecuteItem();
		}

		private void DismissSwipeOnAnExternalCoreWindowTap(CoreWindow sender, PointerEventArgs args)
		{
			DismissSwipeOnAnExternalTap(args.CurrentPoint.RawPosition);
		}
#endif

		private void DismissSwipeOnAnExternalTap(Point tapPoint)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			GeneralTransform transform = TransformToVisual(null);
			Point p = Point.Zero;

			// start of the swipe control
			var transformedElementOrigin = transform.TransformPoint(p);

			// If point is not within the item's bounds, close it.
			if (tapPoint.X < transformedElementOrigin.X || tapPoint.Y < transformedElementOrigin.Y ||
				(tapPoint.X - transformedElementOrigin.X) > ActualWidth ||
				(tapPoint.Y - transformedElementOrigin.Y) > ActualHeight)
			{
				CloseIfNotRemainOpenExecuteItem();
			}
		}

		private void GetTemplateParts()
		{
			m_rootGrid = GetTemplateChild<Grid>(s_rootGridName);
			m_inputEater = GetTemplateChild<Grid>(s_inputEaterName);
			m_content = GetTemplateChild<Grid>(s_ContentRootName);
			m_swipeContentRoot = GetTemplateChild<Grid>(s_swipeContentRootName);
			m_swipeContentStackPanel = GetTemplateChild<StackPanel>(s_swipeContentStackPanelName);

			//Before RS5 these elements were not in the template but were instead created in code behind when the swipe content was created.
			//Post RS5 the code behind expects these elements to always be in the tree.
			if (m_swipeContentRoot is null)
			{
				Grid swipeContentRoot = new Grid();
				swipeContentRoot.Name = "SwipeContentRoot";
				m_swipeContentRoot = swipeContentRoot;
				m_rootGrid.Children.Insert(0, swipeContentRoot);
			}

			if (m_swipeContentStackPanel is null)
			{
				StackPanel swipeContentStackPanel = new StackPanel();
				swipeContentStackPanel.Name("SwipeContentStackPanel");
				m_swipeContentStackPanel = swipeContentStackPanel;
				m_swipeContentRoot.Children.Add(swipeContentStackPanel);
			}

			m_swipeContentStackPanel.Orientation = m_isHorizontal ? Orientation.Horizontal : Orientation.Vertical;

			var lookedUpStyle = SharedHelpers.FindInApplicationResources(s_swipeItemStyleName, null);
			if (lookedUpStyle is { })
			{
				m_swipeItemStyle = lookedUpStyle as Style;
			}
		}

		//* Uno workaround: Animation are not yet supported by composition API, we are using XAML animation instead.
#if false
		void InitializeInteractionTracker()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			IInteractionTrackerOwner interactionTrackerOwner = this;

			if (!m_compositor)
			{
				m_compositor.set(ElementCompositionPreview.GetElementVisual(m_rootGrid).Compositor());
			}

			m_visualInteractionSource.set(VisualInteractionSource.Create(FindVisualInteractionSourceVisual()));
			m_visualInteractionSource.IsPositionXRailsEnabled(m_isHorizontal);
			m_visualInteractionSource.IsPositionYRailsEnabled(!m_isHorizontal);
			m_visualInteractionSource.ManipulationRedirectionMode(VisualInteractionSourceRedirectionMode.CapableTouchpadOnly);
			m_visualInteractionSource.PositionXSourceMode(m_isHorizontal ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled);
			m_visualInteractionSource.PositionYSourceMode(!m_isHorizontal ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled);
			if (m_isHorizontal)
			{
				m_visualInteractionSource.PositionXChainingMode(InteractionChainingMode.Never);
			}
			else
			{
				m_visualInteractionSource.PositionYChainingMode(InteractionChainingMode.Never);
			}

			m_interactionTracker.set(InteractionTracker.CreateWithOwner(m_compositor, interactionTrackerOwner));
			m_interactionTracker.InteractionSources().Add(m_visualInteractionSource);
			m_interactionTracker.Properties.InsertBoolean(s_isFarOpenPropertyName, false);
			m_interactionTracker.Properties.InsertBoolean(s_isNearOpenPropertyName, false);
			m_interactionTracker.Properties.InsertBoolean(s_blockNearContentPropertyName, false);
			m_interactionTracker.Properties.InsertBoolean(s_blockFarContentPropertyName, false);
			m_interactionTracker.Properties.InsertBoolean(s_hasLeftContentPropertyName, LeftItems() && LeftItems().Size() > 0);
			m_interactionTracker.Properties.InsertBoolean(s_hasRightContentPropertyName, RightItems() && RightItems().Size() > 0);
			m_interactionTracker.Properties.InsertBoolean(s_hasTopContentPropertyName, TopItems() && TopItems().Size() > 0);
			m_interactionTracker.Properties.InsertBoolean(s_hasBottomContentPropertyName, BottomItems() && BottomItems().Size() > 0);
			m_interactionTracker.MaxPosition({
				std.numeric_limits<float>.infinity(), std.numeric_limits<float>.infinity(), 0.0f
			});
			m_interactionTracker.MinPosition({
				-1.0f * std.numeric_limits<float>.infinity(), -1.0f * std.numeric_limits<float>.infinity(), 0.0f
			});

			// Create and initialize the Swipe animations:
			// If the swipe control is already opened it should not be possible to open the opposite side's items, without first closing the swipe control.
			// This prevents the user from flicking the swipe control closed and accidently opening the other due to inertia.
			// To acheive this we insert the isFarOpen and isNearOpen boolean properties on the interaction tracker and alter the expression output based on these.
			// The opened state is maintained in the interaction trackers IdleStateEntered handler, this means we need to ensure this state is entered each time the swipe control
			// is opened or closed.

			// A more readable version of the expression:

			/ m_swipeAnimation.set(m_compositor.CreateExpressionAnimation("isHorizontal ?"
			"Vector3(tracker.isFarOpen || tracker.blockNearContent ? Clamp(-tracker.Position.X, -this.Target.Size.X, 0) :"
			"tracker.isNearOpen  || tracker.blockFarContent ? Clamp(-tracker.Position.X,  0, this.Target.Size.X) :"
			"Clamp(-tracker.Position.X, (tracker.hasRightContent ? -10000 : 0), (tracker.hasLeftContent ? 10000 : 0)), 0, 0) :"
			"Vector3(0, tracker.isFarOpen  || tracker.blockNearContent ? Clamp(-tracker.Position.Y, -this.Target.Size.Y, 0) :"
			"tracker.isNearOpen || tracker.blockFarContent ? Clamp(-tracker.Position.Y, 0,  this.Target.Size.Y) :"
			"Clamp(-tracker.Position.Y, (tracker.hasBottomContent ? -10000 : 0), (tracker.hasTopContent ? 10000 : 0)), 0)"));
			*/

			m_swipeAnimation.set(m_compositor.CreateExpressionAnimation(isHorizontalPropertyName() + " ?"
			"Vector3(" + trackerPropertyName() + "." + isFarOpenPropertyName() + " || " + trackerPropertyName() + "." + blockNearContentPropertyName() + " ? Clamp(-" + trackerPropertyName() + ".Position.X, -this.Target.Size.X, 0) :"
				+ trackerPropertyName() + "." + isNearOpenPropertyName() + " || " + trackerPropertyName() + "." + blockFarContentPropertyName() + " ? Clamp(-" + trackerPropertyName() + ".Position.X,  0, this.Target.Size.X) :"
			"Clamp(-" + trackerPropertyName() + ".Position.X, (" + trackerPropertyName() + "." + hasRightContentPropertyName() + " ? -10000 : 0), (" + trackerPropertyName() + "." + hasLeftContentPropertyName() + " ? 10000 : 0)), 0, 0) :"
			"Vector3(0, " + trackerPropertyName() + "." + isFarOpenPropertyName() + " || " + trackerPropertyName() + "." + blockNearContentPropertyName() + "  ? Clamp(-" + trackerPropertyName() + ".Position.Y, -this.Target.Size.Y, 0) :"
				+ trackerPropertyName() + "." + isNearOpenPropertyName() + " || " + trackerPropertyName() + "." + blockFarContentPropertyName() + " ? Clamp(-" + trackerPropertyName() + ".Position.Y, 0,  this.Target.Size.Y) :"
			"Clamp(-" + trackerPropertyName() + ".Position.Y, (" + trackerPropertyName() + "." + hasBottomContentPropertyName() + " ? -10000 : 0), (" + trackerPropertyName() + "." + hasTopContentPropertyName() + " ? 10000 : 0)), 0)"));

			m_swipeAnimation.SetReferenceParameter(s_trackerPropertyName, m_interactionTracker);
			m_swipeAnimation.SetBooleanParameter(s_isHorizontalPropertyName, m_isHorizontal);
			if (IsTranslationFacadeAvailableForSwipeControl(m_content))
			{
				m_swipeAnimation.Target(s_translationPropertyName);
			}

			//A more readable version of the expression:

			/ m_executeExpressionAnimation.set(m_compositor.CreateExpressionAnimation("(foregroundVisual." + GetAnimationTarget() + " * 0.5) + (isHorizontal ?"
			"Vector3((isNearContent ? -0.5, 0.5) * this.Target.Size.X, 0, 0) : "
			"Vector3(0, (isNearContent ? -0.5, 0.5) * this.Target.Size.Y, 0))"));
			*/

			m_executeExpressionAnimation.set(m_compositor.CreateExpressionAnimation("(" + foregroundVisualPropertyName() + "." + GetAnimationTarget(m_swipeContentStackPanel) + " * 0.5) + (" + isHorizontalPropertyName() + " ? "
			"Vector3((" + isNearContentPropertyName() + " ? -0.5 : 0.5) * this.Target.Size.X, 0, 0) : "
			"Vector3(0, (" + isNearContentPropertyName() + " ? -0.5 : 0.5) * this.Target.Size.Y, 0))"));

			m_executeExpressionAnimation.SetBooleanParameter(s_isHorizontalPropertyName, m_isHorizontal);
			if (IsTranslationFacadeAvailableForSwipeControl(m_swipeContentStackPanel))
			{
				m_executeExpressionAnimation.Target(s_translationPropertyName);
			}

			//A more readable version of the expression:

			/ m_clipExpressionAnimation.set(m_compositor.CreateExpressionAnimation(L"isHorizontal ?
			Max(swipeRootVisual.Size.X + (isNearContent ? tracker.Position.X : -tracker.Position.X), 0) :
			Max(swipeRootVisual.Size.Y + (isNearContent ? tracker.Position.Y : -tracker.Position.Y), 0)"));*/

			m_clipExpressionAnimation.set(m_compositor.CreateExpressionAnimation(isHorizontalPropertyName() + " ? "
			"Max(" + swipeRootVisualPropertyName() + ".Size.X + (" + isNearContentPropertyName() + " ? " + trackerPropertyName() + ".Position.X : -" + trackerPropertyName() + ".Position.X) , 0) : "
			"Max(" + swipeRootVisualPropertyName() + ".Size.Y + (" + isNearContentPropertyName() + " ? " + trackerPropertyName() + ".Position.Y : -" + trackerPropertyName() + ".Position.Y) , 0)"));

			m_clipExpressionAnimation.SetReferenceParameter(s_trackerPropertyName, m_interactionTracker);
			m_clipExpressionAnimation.SetBooleanParameter(s_isHorizontalPropertyName, m_isHorizontal);
		}

		void ConfigurePositionInertiaRestingValues()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_isHorizontal)
			{
				IVector<InteractionTrackerInertiaModifier> xModifiers = new Vector<InteractionTrackerInertiaModifier>();

				ExpressionAnimation leftCondition = m_compositor.CreateExpressionAnimation("this.Target." + hasLeftContentPropertyName() + " && !this.Target." + isFarOpenPropertyName() + " && this.Target.NaturalRestingPosition.x <= -1 * (this.Target." + isNearOpenPropertyName() + " ? " + swipeContentSizeParameterName() + " : min(" + swipeContentSizeParameterName() + ", " + maxThresholdPropertyName() + "))");
				leftCondition.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualWidth));
				leftCondition.SetScalarParameter(s_maxThresholdPropertyName, c_ThresholdValue);
				ExpressionAnimation leftRestingPoint = m_compositor.CreateExpressionAnimation("-" + swipeContentSizeParameterName());
				leftRestingPoint.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualWidth));
				InteractionTrackerInertiaRestingValue leftOpen = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				leftOpen.Condition(leftCondition);
				leftOpen.RestingValue(leftRestingPoint);
				xModifiers.Append(leftOpen);

				ExpressionAnimation rightCondition = m_compositor.CreateExpressionAnimation("this.Target." + hasRightContentPropertyName() + " && !this.Target." + isNearOpenPropertyName() + " && this.Target.NaturalRestingPosition.x >= (this.Target." + isFarOpenPropertyName() + " ? " + swipeContentSizeParameterName() + " : min(" + swipeContentSizeParameterName() + ", " + maxThresholdPropertyName() + "))");
				rightCondition.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualWidth));
				rightCondition.SetScalarParameter(s_maxThresholdPropertyName, c_ThresholdValue);
				ExpressionAnimation rightRestingValue = m_compositor.CreateExpressionAnimation(s_swipeContentSizeParameterName);
				rightRestingValue.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualWidth));
				InteractionTrackerInertiaRestingValue rightOpen = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				rightOpen.Condition(rightCondition);
				rightOpen.RestingValue(rightRestingValue);
				xModifiers.Append(rightOpen);

				ExpressionAnimation condition = m_compositor.CreateExpressionAnimation("true");
				ExpressionAnimation restingValue = m_compositor.CreateExpressionAnimation("0");
				InteractionTrackerInertiaRestingValue neutralX = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				neutralX.Condition(condition);
				neutralX.RestingValue(restingValue);
				xModifiers.Append(neutralX);

				m_interactionTracker.ConfigurePositionXInertiaModifiers(xModifiers);
			}
			else
			{
				IVector<InteractionTrackerInertiaModifier> yModifiers = new Vector<InteractionTrackerInertiaModifier>();

				ExpressionAnimation topCondition = m_compositor.CreateExpressionAnimation("this.Target." + hasTopContentPropertyName() + " && !this.Target." + isFarOpenPropertyName() + " && this.Target.NaturalRestingPosition.y <= -1 * (this.Target." + isNearOpenPropertyName() + " ? " + swipeContentSizeParameterName() + " : min(" + swipeContentSizeParameterName() + ", " + maxThresholdPropertyName() + "))");
				topCondition.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualHeight));
				topCondition.SetScalarParameter(s_maxThresholdPropertyName, c_ThresholdValue);
				ExpressionAnimation topRestingValue = m_compositor.CreateExpressionAnimation("-" + swipeContentSizeParameterName());
				topRestingValue.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualHeight));
				InteractionTrackerInertiaRestingValue topOpen = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				topOpen.Condition(topCondition);
				topOpen.RestingValue(topRestingValue);
				yModifiers.Append(topOpen);

				ExpressionAnimation bottomCondition = m_compositor.CreateExpressionAnimation("this.Target." + hasBottomContentPropertyName() + " && !this.Target." + isNearOpenPropertyName() + " && this.Target.NaturalRestingPosition.y >= (this.Target." + isFarOpenPropertyName() + " ? " + swipeContentSizeParameterName() + " : min(" + swipeContentSizeParameterName() + ", " + maxThresholdPropertyName() + "))");
				bottomCondition.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualHeight));
				bottomCondition.SetScalarParameter(s_maxThresholdPropertyName, c_ThresholdValue);
				ExpressionAnimation bottomRestingValue = m_compositor.CreateExpressionAnimation(s_swipeContentSizeParameterName);
				bottomRestingValue.SetScalarParameter(s_swipeContentSizeParameterName, (float)(m_swipeContentStackPanel.ActualHeight));
				InteractionTrackerInertiaRestingValue bottomOpen = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				bottomOpen.Condition(bottomCondition);
				bottomOpen.RestingValue(bottomRestingValue);
				yModifiers.Append(bottomOpen);

				ExpressionAnimation condition = m_compositor.CreateExpressionAnimation("true");
				ExpressionAnimation restingValue = m_compositor.CreateExpressionAnimation("0");
				InteractionTrackerInertiaRestingValue neutralY = InteractionTrackerInertiaRestingValue.Create(m_compositor);
				neutralY.Condition(condition);
				neutralY.RestingValue(restingValue);
				yModifiers.Append(neutralY);

				m_interactionTracker.ConfigurePositionYInertiaModifiers(yModifiers);
			}
		}

		Visual FindVisualInteractionSourceVisual()
		{
			Visual visualInteractionSource = null;

			// Don't walk up the tree too far largely as an optimization for when SwipeControl isn't used
			// with a list.  The general-case when using swipe with a ListView will probably have the
			// LVIP as the visual parent of the SwipeControl but enabling checking for a few more
			// levels above that could enable more complex list item templates where SwipeControl
			// isn't the root element.
			int maxSteps = 5;
			int steps = 0;
			var current = VisualTreeHelper.GetParent(this);
			while (current && steps < maxSteps)
			{
				if (var lvip = current.try_as<ListViewItemPresenter>())
				{
					visualInteractionSource = ElementCompositionPreview.GetElementVisual(lvip);
					break;
				}

				current = VisualTreeHelper.GetParent(current);
				++steps;
			}

			if (!visualInteractionSource)
			{
				visualInteractionSource = ElementCompositionPreview.GetElementVisual(this);
			}

			return visualInteractionSource;
		}
#endif

		private void EnsureClip()
		{
			float width = (float)(ActualWidth);
			float height = (float)(ActualHeight);
			Rect rect = new Rect(0.0f, 0.0f, width, height);
			Microsoft.UI.Xaml.Media.RectangleGeometry rectangleGeometry = new RectangleGeometry();
			rectangleGeometry.Rect = rect;
			Clip = rectangleGeometry;
		}

		private void CloseWithoutAnimation()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			bool wasIdle = m_isIdle;
			//m_interactionTracker.TryUpdatePosition(new Vector3(
			//	0.0f, 0.0f, 0.0f
			//));

			// Uno workaround:
			_desiredPosition = Vector2.Zero;
			UpdateStackPanelDesiredPosition();
			UpdateTransforms();

			if (wasIdle)
			{
				IdleStateEntered(null, null);
			}
		}

		private void CloseIfNotRemainOpenExecuteItem()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_currentItems is { } &&
				m_currentItems.Mode == SwipeMode.Execute &&
				m_currentItems.Size > 0 &&
				m_currentItems.GetAt(0).BehaviorOnInvoked == SwipeBehaviorOnInvoked.RemainOpen &&
				m_isOpen)
			{
				//If we have a Mode set to Execute, and an item with BehaviorOnInvoked set to RemainOpen, we do not want to close, so no-op
				return;
			}

			Close();
		}

		private void CreateLeftContent()
		{
			var items = LeftItems;
			if (items is { })
			{
				m_createdContent = CreatedContent.Left;
				CreateContent(items);
			}
		}

		private void CreateRightContent()
		{
			var items = RightItems;
			if (items is { })
			{
				m_createdContent = CreatedContent.Right;
				CreateContent(items);
			}
		}

		private void CreateBottomContent()
		{
			var items = BottomItems;
			if (items is { })
			{
				m_createdContent = CreatedContent.Bottom;
				CreateContent(items);
			}
		}

		private void CreateTopContent()
		{
			var items = TopItems;
			if (items is { })
			{
				m_createdContent = CreatedContent.Top;
				CreateContent(items);
			}
		}

		private void CreateContent(SwipeItems items)
		{
			if (m_swipeContentStackPanel is { } && m_swipeContentStackPanel.Children is { })
			{
				m_swipeContentStackPanel.Children.Clear();
			}

			m_currentItems = items;
			if (m_currentItems is { })
			{
				AlignStackPanel();
				PopulateContentItems();
				SetupExecuteExpressionAnimation();
				SetupClipAnimation();
				UpdateColors();
			}
		}

		private void AlignStackPanel()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_currentItems.Size > 0)
			{
				switch (m_currentItems.Mode)
				{
					case SwipeMode.Execute:
						{
							if (m_isHorizontal)
							{
								m_swipeContentStackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
								m_swipeContentStackPanel.VerticalAlignment = VerticalAlignment.Center;
							}
							else
							{
								m_swipeContentStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
								m_swipeContentStackPanel.VerticalAlignment = VerticalAlignment.Stretch;
							}

							break;
						}
					case SwipeMode.Reveal:
						{
							if (m_isHorizontal)
							{
								var swipeContentStackPanelHorizontalAlignment = m_createdContent == CreatedContent.Left ? HorizontalAlignment.Left :
									m_createdContent == CreatedContent.Right ? HorizontalAlignment.Right :
									HorizontalAlignment.Stretch;

								m_swipeContentStackPanel.HorizontalAlignment = swipeContentStackPanelHorizontalAlignment;
								m_swipeContentStackPanel.VerticalAlignment = VerticalAlignment.Center;
							}
							else
							{
								var swipeContentStackPanelVerticalAlignment = m_createdContent == CreatedContent.Top ? VerticalAlignment.Top :
									m_createdContent == CreatedContent.Bottom ? VerticalAlignment.Bottom :
									VerticalAlignment.Stretch;

								m_swipeContentStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
								m_swipeContentStackPanel.VerticalAlignment = swipeContentStackPanelVerticalAlignment;
							}

							break;
						}
					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}
			}
		}

		private void PopulateContentItems()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			foreach (var swipeItem in m_currentItems)
			{
				m_swipeContentStackPanel.Children.Add(GetSwipeItemButton(swipeItem));
			}

			TryGetSwipeVisuals();
		}

		private void SetupExecuteExpressionAnimation()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			//if (IsTranslationFacadeAvailableForSwipeControl(m_swipeContentStackPanel))
			//{
			//	m_swipeContentStackPanel.StopAnimation(m_executeExpressionAnimation);
			//	m_swipeContentStackPanel.Translation = new Vector3(
			//		0.0f, 0.0f, 0.0f
			//	);
			//}
			//else if (m_swipeContentVisual is { })
			//{
			//	m_swipeContentVisual.StopAnimation(GetAnimationTarget(m_swipeContentStackPanel));
			//	m_swipeContentVisual.Properties.InsertVector3(GetAnimationTarget(m_swipeContentStackPanel), new Vector3(
			//		0.0f, 0.0f, 0.0f
			//	));
			//}

			if (m_currentItems.Mode == SwipeMode.Execute)
			{
				//global::System.Diagnostics.Debug.Assert((m_createdContent != CreatedContent.None));
				//m_executeExpressionAnimation.SetBooleanParameter(s_isNearContentPropertyName, m_createdContent == CreatedContent.Left || m_createdContent == CreatedContent.Top);
				//if (IsTranslationFacadeAvailableForSwipeControl(m_swipeContentStackPanel))
				//{
				//	m_swipeContentStackPanel.StartAnimation(m_executeExpressionAnimation);
				//}

				//if (m_swipeContentVisual is {})
				//{
				//	m_swipeContentVisual.StartAnimation(GetAnimationTarget(m_swipeContentStackPanel), m_executeExpressionAnimation);
				//}
			}
		}

		private void SetupClipAnimation()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			//if (m_insetClip is null)
			//{
			//	m_insetClip = m_compositor.CreateInsetClip();
			//	m_swipeContentRootVisual.Clip = m_insetClip;
			//}
			//else
			//{
			//	m_insetClip.StopAnimation(s_leftInsetTargetName);
			//	m_insetClip.StopAnimation(s_rightInsetTargetName);
			//	m_insetClip.StopAnimation(s_topInsetTargetName);
			//	m_insetClip.StopAnimation(s_bottomInsetTargetName);
			//	m_insetClip.LeftInset = 0.0f;
			//	m_insetClip.RightInset = 0.0f;
			//	m_insetClip.TopInset = 0.0f;
			//	m_insetClip.BottomInset = 0.0f;
			//}

			//m_clipExpressionAnimation.SetBooleanParameter(s_isNearContentPropertyName, m_createdContent == CreatedContent.Left || m_createdContent == CreatedContent.Top);

			//if (m_createdContent == CreatedContent.None)
			//{
			//	//If we have no created content then we don't need to start the clip animation yet.
			//	return;
			//}

			//m_insetClip.StartAnimation(DirectionToInset(m_createdContent), m_clipExpressionAnimation);
		}

		private void UpdateColors()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_currentItems.Mode == SwipeMode.Execute)
			{
				UpdateColorsIfExecuteItem();
			}
			else
			{
				UpdateColorsIfRevealItems();
			}
		}

		AppBarButton GetSwipeItemButton(SwipeItem swipeItem)
		{
			AppBarButton itemAsButton = new AppBarButton();
			swipeItem.GenerateControl(itemAsButton, m_swipeItemStyle);

			if (swipeItem.Background is null)
			{
				var lookedUpBrush = SharedHelpers.FindInApplicationResources(m_currentItems.Mode == SwipeMode.Reveal ? s_swipeItemBackgroundResourceName : m_thresholdReached ? s_executeSwipeItemPostThresholdBackgroundResourceName : s_executeSwipeItemPreThresholdBackgroundResourceName);
				if (lookedUpBrush is { })
				{
					itemAsButton.Background = lookedUpBrush as Brush;
				}
			}

			if (swipeItem.Foreground is null)
			{
				var lookedUpBrush = SharedHelpers.FindInApplicationResources(m_currentItems.Mode == SwipeMode.Reveal ? s_swipeItemForegroundResourceName : m_thresholdReached ? s_executeSwipeItemPostThresholdForegroundResourceName : s_executeSwipeItemPreThresholdForegroundResourceName);
				if (lookedUpBrush is { })
				{
					itemAsButton.Foreground = lookedUpBrush as Brush;
				}
			}

			if (m_isHorizontal)
			{
				itemAsButton.Height = ActualHeight;
				if (m_currentItems.Mode == SwipeMode.Execute)
				{
					itemAsButton.Width = ActualWidth;
				}
			}
			else
			{
				itemAsButton.Width = (ActualWidth);
				if (m_currentItems.Mode == SwipeMode.Execute)
				{
					itemAsButton.Height = (ActualHeight);
				}
			}

			return itemAsButton;
		}

		private void UpdateColorsIfExecuteItem()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_currentItems is null || m_currentItems.Mode != SwipeMode.Execute)
			{
				return;
			}

			SwipeItem swipeItem = null;
			if (m_currentItems.Size > 0)
			{
				swipeItem = m_currentItems.GetAt(0);
			}

			UpdateExecuteBackgroundColor(swipeItem);
			UpdateExecuteForegroundColor(swipeItem);
		}

		private void UpdateExecuteBackgroundColor(SwipeItem swipeItem)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			Brush background = null;

			if (!m_thresholdReached)
			{
				var lookedUpBackgroundBrush = SharedHelpers.FindInApplicationResources(s_executeSwipeItemPreThresholdBackgroundResourceName);
				if (lookedUpBackgroundBrush is { })
				{
					background = lookedUpBackgroundBrush as Brush;
				}
			}
			else
			{
				var lookedUpBackgroundBrush = SharedHelpers.FindInApplicationResources(s_executeSwipeItemPostThresholdBackgroundResourceName);
				if (lookedUpBackgroundBrush is { })
				{
					background = lookedUpBackgroundBrush as Brush;
				}
			}

			if (swipeItem is { } && swipeItem.Background is { })
			{
				background = swipeItem.Background;
			}

			m_swipeContentStackPanel.Background = background;
			m_swipeContentRoot.Background = null;
		}

		private void UpdateExecuteForegroundColor(SwipeItem swipeItem)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_swipeContentStackPanel.Children.Count > 0)
			{
				if (m_swipeContentStackPanel.Children[0] is AppBarButton appBarButton)
				{
					Brush foreground = null;

					if (!m_thresholdReached)
					{
						var lookedUpForegroundBrush = SharedHelpers.FindInApplicationResources(s_executeSwipeItemPreThresholdForegroundResourceName);
						if (lookedUpForegroundBrush is { })
						{
							foreground = lookedUpForegroundBrush as Brush;
						}
					}
					else
					{
						var lookedUpForegroundBrush = SharedHelpers.FindInApplicationResources(s_executeSwipeItemPostThresholdForegroundResourceName);
						if (lookedUpForegroundBrush is { })
						{
							foreground = lookedUpForegroundBrush as Brush;
						}
					}

					if (swipeItem is { } && swipeItem.Foreground is { })
					{
						foreground = swipeItem.Foreground;
					}

					appBarButton.Foreground = foreground;
					appBarButton.Background = new SolidColorBrush(Colors.Transparent);
				}
			}
		}

		private void UpdateColorsIfRevealItems()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (m_currentItems.Mode != SwipeMode.Reveal)
			{
				return;
			}

			Brush rootGridBackground = null;

			var lookedUpBrush = SharedHelpers.FindInApplicationResources(s_swipeItemBackgroundResourceName);
			if (lookedUpBrush is { })
			{
				rootGridBackground = lookedUpBrush as Brush;
			}
			if (m_currentItems.Size > 0)
			{
				switch (m_createdContent)
				{
					case CreatedContent.Left:
					case CreatedContent.Top:
						{
							var itemBackground = m_currentItems.GetAt((uint)m_swipeContentStackPanel.Children.Count - 1).Background;
							if (itemBackground != null)
							{
								rootGridBackground = itemBackground;
							}

							break;
						}
					case CreatedContent.Right:
					case CreatedContent.Bottom:
						{
							var itemBackground = m_currentItems.GetAt(0).Background;
							if (itemBackground != null)
							{
								rootGridBackground = itemBackground;
							}

							break;
						}
					case CreatedContent.None:
						{
							break;
						}
					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}
			}

			m_swipeContentRoot.Background = rootGridBackground;
			m_swipeContentStackPanel.Background = null;
		}

		private void OnLeftItemsChanged(IObservableVector<SwipeItem> sender, IVectorChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			ThrowIfHasVerticalAndHorizontalContent();
			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasLeftContentPropertyName, sender.Count > 0);
			//}
			// Uno workaround:
			_hasLeftContent = sender.Count > 0;

			if (m_createdContent == CreatedContent.Left)
			{
				CreateLeftContent();
			}
		}

		private void OnRightItemsChanged(IObservableVector<SwipeItem> sender, IVectorChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			ThrowIfHasVerticalAndHorizontalContent();

			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasRightContentPropertyName, sender.Count > 0);
			//}
			// Uno workaround:
			_hasRightContent = sender.Count > 0;

			if (m_createdContent == CreatedContent.Right)
			{
				CreateRightContent();
			}
		}

		private void OnTopItemsChanged(IObservableVector<SwipeItem> sender, IVectorChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			ThrowIfHasVerticalAndHorizontalContent();
			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasTopContentPropertyName, sender.Count > 0);
			//}
			// Uno workaround:
			_hasTopContent = sender.Count > 0;

			if (m_createdContent == CreatedContent.Top)
			{
				CreateTopContent();
			}
		}

		private void OnBottomItemsChanged(IObservableVector<SwipeItem> sender, IVectorChangedEventArgs args)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			ThrowIfHasVerticalAndHorizontalContent();
			//if (m_interactionTracker is {})
			//{
			//	m_interactionTracker.Properties.InsertBoolean(s_hasBottomContentPropertyName, sender.Count > 0);
			//}
			// Uno workaround:
			_hasBottomContent = sender.Count > 0;

			if (m_createdContent == CreatedContent.Bottom)
			{
				CreateBottomContent();
			}
		}

		private void TryGetSwipeVisuals()
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			//if (IsTranslationFacadeAvailableForSwipeControl(m_content))
			//{
			//	m_swipeAnimation.Target = GetAnimationTarget(m_content);
			//	m_content.StartAnimation(m_swipeAnimation);
			//}
			//else
			//{
			//	var mainContentVisual = ElementCompositionPreview.GetElementVisual(m_content);
			//	if (mainContentVisual is {} && m_mainContentVisual != mainContentVisual)
			//	{
			//		m_mainContentVisual = mainContentVisual;

			//		if (DownlevelHelper.SetIsTranslationEnabledExists())
			//		{
			//			ElementCompositionPreview.SetIsTranslationEnabled(m_content, true);
			//			mainContentVisual.Properties.InsertVector3(s_translationPropertyName, new Vector3(
			//				0.0f, 0.0f, 0.0f
			//			));
			//		}

			//		mainContentVisual.StartAnimation(GetAnimationTarget(m_content), m_swipeAnimation);

			//		m_executeExpressionAnimation.SetReferenceParameter(s_foregroundVisualPropertyName, mainContentVisual);
			//	}
			//}

			//if (IsTranslationFacadeAvailableForSwipeControl(m_swipeContentStackPanel))
			//{
			//	m_swipeAnimation.Target = GetAnimationTarget(m_swipeContentStackPanel);
			//}
			//else
			//{
			//	var swipeContentVisual = ElementCompositionPreview.GetElementVisual(m_swipeContentStackPanel);
			//	if (swipeContentVisual is {} && m_swipeContentVisual != swipeContentVisual)
			//	{
			//		m_swipeContentVisual = swipeContentVisual;

			//		if (DownlevelHelper.SetIsTranslationEnabledExists())
			//		{
			//			ElementCompositionPreview.SetIsTranslationEnabled(m_swipeContentStackPanel, true);
			//			swipeContentVisual.Properties.InsertVector3(s_translationPropertyName, new Vector3(
			//				0.0f, 0.0f, 0.0f
			//			));
			//		}

			//		ConfigurePositionInertiaRestingValues();
			//	}
			//}

			//var swipeContentRootVisual = ElementCompositionPreview.GetElementVisual(m_swipeContentRoot);
			//if (swipeContentRootVisual is {} && m_swipeContentRootVisual != swipeContentRootVisual)
			//{
			//	m_swipeContentRootVisual = swipeContentRootVisual;
			//	m_clipExpressionAnimation.SetReferenceParameter(s_swipeRootVisualPropertyName, swipeContentRootVisual);
			//	if (m_insetClip is {})
			//	{
			//		swipeContentRootVisual.Clip = m_insetClip;
			//	}
			//}
		}

		private void UpdateIsOpen(bool isOpen)
		{
			SWIPECONTROL_TRACE_INFO(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			if (isOpen)
			{
				if (!m_isOpen)
				{
					m_isOpen = true;
					m_lastActionWasOpening = true;
					switch (m_createdContent)
					{
						case CreatedContent.Right:
						case CreatedContent.Bottom:
							//m_interactionTracker.Properties.InsertBoolean(s_isFarOpenPropertyName, true);
							//m_interactionTracker.Properties.InsertBoolean(s_isNearOpenPropertyName, false);
							// Uno workaround:
							_isFarOpen = true;
							_isNearOpen = false;
							break;
						case CreatedContent.Left:
						case CreatedContent.Top:
							//m_interactionTracker.Properties.InsertBoolean(s_isFarOpenPropertyName, false);
							//m_interactionTracker.Properties.InsertBoolean(s_isNearOpenPropertyName, true);
							// Uno workaround:
							_isFarOpen = false;
							_isNearOpen = true;
							break;
						case CreatedContent.None:
							//m_interactionTracker.Properties.InsertBoolean(s_isFarOpenPropertyName, false);
							//m_interactionTracker.Properties.InsertBoolean(s_isNearOpenPropertyName, false);
							// Uno workaround:
							_isFarOpen = false;
							_isNearOpen = false;
							break;
						default:
							global::System.Diagnostics.Debug.Assert(false);
							break;
					}

					if (m_currentItems.Mode != SwipeMode.Execute)
					{
						AttachDismissingHandlers();
					}

					var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
					if (globalTestHooks is { })
					{
						globalTestHooks.NotifyOpenedStatusChanged(this);
					}
				}
			}
			else
			{
				if (m_isOpen)
				{
					m_isOpen = false;
					m_lastActionWasClosing = true;
					DetachDismissingHandlers();
					//m_interactionTracker.Properties.InsertBoolean(s_isFarOpenPropertyName, false);
					//m_interactionTracker.Properties.InsertBoolean(s_isNearOpenPropertyName, false);
					// Uno workaround:
					_isFarOpen = false;
					_isNearOpen = false;

					var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
					if (globalTestHooks is { })
					{
						globalTestHooks.NotifyOpenedStatusChanged(this);
					}
				}
			}
		}

		private void UpdateThresholdReached(float value)
		{
			SWIPECONTROL_TRACE_VERBOSE(this/*, TRACE_MSG_METH, METH_NAME, this*/);

			bool oldValue = m_thresholdReached;
			float effectiveStackPanelSize = (float)((m_isHorizontal ? m_swipeContentStackPanel.ActualWidth : m_swipeContentStackPanel.ActualHeight) - 1);
			if (!m_isOpen || m_lastActionWasOpening)
			{
				//If we are opening new swipe items then we need to scroll open c_ThresholdValue
				m_thresholdReached = Math.Abs(value) > Math.Min(effectiveStackPanelSize, c_ThresholdValue);
			}
			else
			{
				//If we already have an open swipe item then swiping it closed by any amount will close it.
				m_thresholdReached = Math.Abs(value) < effectiveStackPanelSize;
			}

			if (m_thresholdReached != oldValue)
			{
				UpdateColorsIfExecuteItem();
			}
		}

		private void ThrowIfHasVerticalAndHorizontalContent(bool setIsHorizontal = false)
		{
			bool hasLeftContent = LeftItems is { } && LeftItems.Size > 0;
			bool hasRightContent = RightItems is { } && RightItems.Size > 0;
			bool hasTopContent = TopItems is { } && TopItems.Size > 0;
			bool hasBottomContent = BottomItems is { } && BottomItems.Size > 0;
			if (setIsHorizontal)
			{
				m_isHorizontal = hasLeftContent || hasRightContent || !(hasTopContent || hasBottomContent);
			}

			if (this.Template is { })
			{
				if (m_isHorizontal && (hasTopContent || hasBottomContent))
				{
					throw new ArgumentException("This SwipeControl is horizontal and can not have vertical items.");
				}

				if (!m_isHorizontal && (hasLeftContent || hasRightContent))
				{
					throw new ArgumentException("This SwipeControl is vertical and can not have horizontal items.");
				}
			}
			else
			{
				if ((hasLeftContent || hasRightContent) && (hasTopContent || hasBottomContent))
				{
					throw new ArgumentException("SwipeControl can't have both horizontal items and vertical items set at the same time.");
				}
			}
		}

#if false
		private string GetAnimationTarget(UIElement child)
		{
			if (DownlevelHelper.SetIsTranslationEnabledExists() || SharedHelpers.IsTranslationFacadeAvailable(child))
			{
				return s_translationPropertyName;
			}
			else
			{
				return s_offsetPropertyName;
			}
		}

		private SwipeControl GetThis()
		{
			return this;
		}

		private bool IsTranslationFacadeAvailableForSwipeControl(UIElement element)
		{
			//For now Facade's are causing more issues than they are worth for swipe control. Revist this
			//when we have a little more time.

			//There are concerns about swipe consumers having taken a dependency on the ElementCompositionPreview
			//Api's that this is exclusive with and also the target property of the swipe expression animations
			//is not resolving with the use of Facade's
			return false;
			//return SharedHelpers.IsTranslationFacadeAvailable(element);
		}

		private string DirectionToInset(CreatedContent createdContent)
		{
			switch (createdContent)
			{
				case CreatedContent.Right:
					return s_leftInsetTargetName;
				case CreatedContent.Left:
					return s_rightInsetTargetName;
				case CreatedContent.Bottom:
					return s_topInsetTargetName;
				case CreatedContent.Top:
					return s_bottomInsetTargetName;
				case CreatedContent.None:
					return "";
				default:
					global::System.Diagnostics.Debug.Assert(false);
					return "";
			}
		}
#endif
	}
}
