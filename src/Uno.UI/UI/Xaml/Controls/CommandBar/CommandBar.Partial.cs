// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

#if __IOS__ || __ANDROID__
#define HAS_NATIVE_COMMANDBAR
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DirectUI;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Uno.UI.Controls;
#if __ANDROID__
using Android.Views;
#endif

using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CommandBar : IMenu
	{
		private enum OverflowInitialFocusItem
		{
			None,
			FirstItem,
			LastItem,
		}

		CommandBarElementCollection? m_tpPrimaryCommands;
		CommandBarElementCollection? m_tpSecondaryCommands;
		ObservableCollection<ICommandBarElement>? m_tpDynamicPrimaryCommands;
		ObservableCollection<ICommandBarElement>? m_tpDynamicSecondaryCommands;
		IterableWrappedObservableCollection<ICommandBarElement>? m_tpWrappedPrimaryCommands;
		IterableWrappedObservableCollection<ICommandBarElement>? m_tpWrappedSecondaryCommands;

		// Primary commands in the transition to move or restore the primary commands
		// to overflow or primary commands
		TrackerCollection<ICommandBarElement>? m_tpPrimaryCommandsInTransition;
		TrackerCollection<ICommandBarElement>? m_tpPrimaryCommandsInPreviousTransition;


		private readonly SerialDisposable m_unloadedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_primaryCommandsChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_secondaryCommandsChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_secondaryItemsControlLoadedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_contentRootSizeChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_overflowContentSizeChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_overflowPopupClosedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_overflowPresenterItemsPresenterKeyDownEventHandler = new SerialDisposable();

		private readonly SerialDisposable m_accessKeyInvokedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_overflowPopupOpenedEventHandler = new SerialDisposable();

		// Template parts.
		FrameworkElement? m_tpContentControl;
		FrameworkElement? m_tpOverflowContentRoot;
		Popup? m_tpOverflowPopup;
		ItemsPresenter? m_tpOverflowPresenterItemsPresenter;
		FrameworkElement? m_tpWindowedPopupPadding;

		double m_overflowContentMinWidth;
		double m_overflowContentTouchMinWidth;
		double m_overflowContentMaxWidth = 480;

		// Restorable primary command minimum width from overflow to the primary command collection
		double m_restorablePrimaryCommandMinWidth;


#pragma warning disable CS0414
#pragma warning disable CS0649
		bool m_skipProcessTabStopOverride;
#pragma warning restore CS0414
#pragma warning restore CS0649
		// DirectUI::InputDeviceType m_inputDeviceTypeUsedToOpen = DirectUI::InputDeviceType::Touch;


		bool m_hasAlreadyFiredOverflowChangingEvent;
		bool m_hasAppBarSeparatorInOverflow;
		bool m_isDynamicOverflowEnabled = true;
		int m_SecondaryCommandStartIndex;

		OverflowInitialFocusItem m_overflowInitialFocusItem = OverflowInitialFocusItem.None;

		AppBarSeparator? m_tpAppBarSeparatorInOverflow;

		// Whenever there is a change in the primary/secondary commands or a size change, we take note
		// of the focused command and we make sure we restore focus during the next layout pass.
		ICommandBarElement? m_focusedElementPriorToCollectionOrSizeChange;
		FocusState m_focusStatePriorToCollectionOrSizeChange;

		double m_lastAvailableWidth;

		IMenu? IMenu.ParentMenu
		{
			get => null;
			set => throw new NotImplementedException();
		}

#if __IOS__ || __ANDROID__
		internal NativeCommandBarPresenter? Presenter { get; set; }
#endif

		public CommandBar()
		{
			DefaultStyleKey = typeof(CommandBar);
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			m_unloadedEventHandler.Disposable = null;
			m_primaryCommandsChangedEventHandler.Disposable = null;
			m_secondaryCommandsChangedEventHandler.Disposable = null;
			m_secondaryItemsControlLoadedEventHandler.Disposable = null;
			m_contentRootSizeChangedEventHandler.Disposable = null;
			m_overflowContentSizeChangedEventHandler.Disposable = null;
			m_overflowPopupClosedEventHandler.Disposable = null;
			m_overflowPresenterItemsPresenterKeyDownEventHandler.Disposable = null;

			m_accessKeyInvokedEventHandler.Disposable = null;
			m_overflowPopupOpenedEventHandler.Disposable = null;

			// Make sure our popup is closed.
			if (m_tpOverflowPopup is { })
			{
				m_tpOverflowPopup.IsOpen = false;
			}

			var isOpen = IsOpen;
			if (!isOpen)
			{
				SetCompactMode(true);
			}
		}

		protected override void PrepareState()
		{
			base.PrepareState();

			CommandBarElementCollection spCollection_Primary;

			spCollection_Primary = new CommandBarElementCollection();
			spCollection_Primary.Init(this, notifyCollectionChanging: false);
			m_tpPrimaryCommands = spCollection_Primary;

			CommandBarElementCollection spCollection_Secondary;

			spCollection_Secondary = new CommandBarElementCollection();
			spCollection_Secondary.Init(this, notifyCollectionChanging: true);
			m_tpSecondaryCommands = spCollection_Secondary;

			// Set the value for our collection properties so that they are in the
			// effective value map and get processed during EnterImpl.
			PrimaryCommands = m_tpPrimaryCommands;
			SecondaryCommands = m_tpSecondaryCommands;

			m_tpPrimaryCommands.VectorChanged += OnPrimaryCommandsChanged;
			m_primaryCommandsChangedEventHandler.Disposable = Disposable.Create(() => m_tpPrimaryCommands.VectorChanged -= OnPrimaryCommandsChanged);

			m_tpSecondaryCommands.VectorChanged += OnSecondaryCommandsChanged;
			m_secondaryCommandsChangedEventHandler.Disposable = Disposable.Create(() => m_tpSecondaryCommands.VectorChanged -= OnSecondaryCommandsChanged);

			m_tpDynamicPrimaryCommands = new ObservableCollection<ICommandBarElement>();
			//	m_tpDynamicPrimaryCommands.Init(this, notifyCollectionChanging: false);

			m_tpDynamicSecondaryCommands = new ObservableCollection<ICommandBarElement>();
			//	m_tpDynamicSecondaryCommands.Init(this, notifyCollectionChanging: false);

			m_tpPrimaryCommandsInPreviousTransition = new TrackerCollection<ICommandBarElement>();
			m_tpPrimaryCommandsInTransition = new TrackerCollection<ICommandBarElement>();

			m_tpAppBarSeparatorInOverflow = new AppBarSeparator();

			CommandBarTemplateSettings = new CommandBarTemplateSettings();
		}


		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == DefaultLabelPositionProperty)
			{
				PropagateDefaultLabelPosition();
				UpdateVisualState();
			}
			else if (args.Property == IsDynamicOverflowEnabledProperty)
			{
				if (m_isDynamicOverflowEnabled != (bool)args.NewValue)
				{
					m_isDynamicOverflowEnabled = (bool)args.NewValue;

					ResetDynamicCommands();
					InvalidateMeasure();
					UpdateVisualState();
				}
			}
			else if (args.Property == ClosedDisplayModeProperty
				|| args.Property == OverflowButtonVisibilityProperty)
			{
				UpdateTemplateSettings();
			}
			else if (args.Property == VisibilityProperty)
			{
				ResetCommandBarElementFocus();
			}
		}

		protected override void OnApplyTemplate()
		{
			if (m_tpSecondaryItemsControlPart is { })
			{
				m_secondaryItemsControlLoadedEventHandler.Disposable = null;
			}

			if (m_tpOverflowContentRoot is { })
			{
				m_overflowContentSizeChangedEventHandler.Disposable = null;
			}

			if (m_tpOverflowPresenterItemsPresenter is { })
			{
				m_overflowPresenterItemsPresenterKeyDownEventHandler.Disposable = null;
			}

			if (m_tpOverflowPopup is { })
			{
				m_overflowPopupClosedEventHandler.Disposable = null;
			}

			// Clear our previous template parts.
			m_tpContentControl = null;
			m_tpOverflowContentRoot = null;
			m_tpOverflowPopup = null;
			m_tpOverflowPresenterItemsPresenter = null;
			m_tpWindowedPopupPadding = null;

			base.OnApplyTemplate();

			GetTemplatePart("PrimaryItemsControl", out m_tpPrimaryItemsControlPart);
			GetTemplatePart("SecondaryItemsControl", out m_tpSecondaryItemsControlPart);

#if __ANDROID__
			Presenter = (this as ViewGroup).FindFirstChild<NativeCommandBarPresenter>();
#elif __IOS__
			Presenter = this.FindFirstChild<NativeCommandBarPresenter?>();
#endif

			if (m_tpSecondaryItemsControlPart is { })
			{
				m_tpSecondaryItemsControlPart.Loaded += OnSecondaryItemsControlLoaded;
				m_secondaryItemsControlLoadedEventHandler.Disposable = Disposable.Create(() => m_tpSecondaryItemsControlPart.Loaded -= OnSecondaryItemsControlLoaded);
			}

			// Apply a shadow
			//IFC_RETURN(ApplyElevationEffect(m_tpSecondaryItemsControlPart.AsOrNull<IUIElement>().Get()));

			GetTemplatePart<FrameworkElement>("ContentControl", out var contentControl);
			GetTemplatePart<FrameworkElement>("OverflowContentRoot", out var overflowContentRoot);
			GetTemplatePart<Popup>("OverflowPopup", out var overflowPopup);
			GetTemplatePart<FrameworkElement>("WindowedPopupPadding", out var windowedPopupPadding);

			m_tpContentControl = contentControl;

			m_tpOverflowContentRoot = overflowContentRoot;
			if (m_tpOverflowContentRoot is { })
			{
				m_tpOverflowContentRoot.SizeChanged += OnOverflowContentRootSizeChanged;
				m_overflowContentSizeChangedEventHandler.Disposable = Disposable.Create(() => m_tpOverflowContentRoot.SizeChanged -= OnOverflowContentRootSizeChanged);
			}

			m_tpOverflowPopup = overflowPopup;
			if (m_tpOverflowPopup is { })
			{
				m_tpOverflowPopup.IsSubMenu = true;

				// Uno Only: AppBar/CommandBar will not "light-dismiss" close unless we listen to the Closed event for the overflow Popup. Missing implementation for Layout Transition Elements
				m_tpOverflowPopup.Closed += OnOverflowPopupClosed;
				m_overflowPopupClosedEventHandler.Disposable = Disposable.Create(() => m_tpOverflowPopup.Closed -= OnOverflowPopupClosed);
			}

			m_tpWindowedPopupPadding = windowedPopupPadding;

			// Query overflow menu min/max width from resource dictionary.
			m_overflowContentMinWidth = ResourceResolver.ResolveTopLevelResourceDouble("CommandBarOverflowMinWidth");
			m_overflowContentTouchMinWidth = ResourceResolver.ResolveTopLevelResourceDouble("CommandBarOverflowTouchMinWidth");
			m_overflowContentMaxWidth = ResourceResolver.ResolveTopLevelResourceDouble("CommandBarOverflowMaxWidth");

			// We set CommandBarTemplateSettings.OverflowContentMaxWidth immediately, rather than waiting for the CommandBar to open before setting it.
			// If we don't initialize it here, it will default to 0, meaning the overflow will stay at size 0,0 and will never fire the SizeChanged event.
			// The SizeChanged event is what triggers the call to UpdateTemplateSettings after the CommandBar opens.
			if (CommandBarTemplateSettings is { } templateSettings)
			{
				templateSettings.OverflowContentMaxWidth = m_overflowContentMaxWidth;
			}

			// Configure our template part items controls by setting their items source's to
			// the correct items vector.
			ConfigureItemsControls();

			var isOpen = IsOpen;

			// Put the primary commands into compact mode if not open.
			if (!isOpen)
			{
				SetCompactMode(true);
			}

			// Inform the secondary AppBarButtons whether or not any secondary AppBarToggleButtons exist.
			SetOverflowStyleParams();

			PropagateDefaultLabelPosition();

			var secondaryItemCount = m_tpDynamicSecondaryCommands?.Count ?? 0;
			for (int i = 0; i < secondaryItemCount; ++i)
			{
				SetOverflowStyleAndInputModeOnSecondaryCommand(i, true);
			}

			//Enabling Keytips and AccessKeys in CommandBar secondary commands
			if (m_tpExpandButton is { } && m_tpSecondaryItemsControlPart is { })
			{
				var isAKScope = m_tpExpandButton.IsAccessKeyScope;

				if (isAKScope)
				{
					m_tpSecondaryItemsControlPart.AccessKeyScopeOwner = m_tpExpandButton;

					AccessKeyInvoked += OnAccessKeyInvoked;
					m_accessKeyInvokedEventHandler.Disposable = Disposable.Create(() => AccessKeyInvoked -= OnAccessKeyInvoked);
				}
			}

			UpdateVisualState();
		}

		#region Uno Only

		private void OnOverflowPopupClosed(object? sender, object e)
		{
			IsOpen = false;
		}

#if __IOS__ || __ANDROID__
		private static DependencyProperty NavigationCommandProperty = Uno.UI.ToolkitHelper.GetProperty("Uno.UI.Toolkit.CommandBarExtensions", "NavigationCommand");

		internal override void UpdateThemeBindings(ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);

			// these commands are not part of visual tree, so we need to propagate it manually
			var commands = new[] { PrimaryCommands, SecondaryCommands }
				.Where(x => x != null)
				.SelectMany(x => x)
				.OfType<AppBarButton>()
				.Prepend(this.GetValue(NavigationCommandProperty) as AppBarButton);
			foreach (var command in commands)
			{
				command?.UpdateThemeBindings(updateReason);
			}
		}
#endif
		#endregion

		protected override Size MeasureOverride(Size availableSize)
		{
			if (m_isDynamicOverflowEnabled)
			{
				return MeasureOverrideForDynamicOverflow(availableSize);
			}
			else
			{
				return base.MeasureOverride(availableSize);
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = base.ArrangeOverride(finalSize);

			// We need to wait until the measure pass is done and the new command containers
			// are generated before we restore focus. Note that the measure pass will measure
			// both the CommandBar and the secondary commands' popup. The latter is why we
			// can't call RestoreCommandBarElementFocus at the end of CommandBar::MeasureOverride.
			RestoreCommandBarElementFocus();

			return size;
		}

		private Size MeasureOverrideForDynamicOverflow(Size availableSize)
		{
			var contentRootDesiredSize = new Size();
			var availablePrimarySize = new Size();

			MUX_ASSERT(m_isDynamicOverflowEnabled);

			var measureSize = base.MeasureOverride(availableSize);

			if (m_tpPrimaryItemsControlPart is { } && m_tpContentRoot is { })
			{
				// Get the available sizes for the primary commands.
				availablePrimarySize = m_tpPrimaryItemsControlPart.DesiredSize;

				m_tpContentRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				contentRootDesiredSize = m_tpContentRoot.DesiredSize;

				// Check the available width whether it needs to move the primary commands into overflow or
				// restore the primary commands from overflow to the primary commands collection
				if (contentRootDesiredSize.Width > availableSize.Width)
				{
					int itemsCount = 0;

					itemsCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

					if (itemsCount > 0)
					{
						int primaryCommandsCountInTransition = 0;
						Size primaryDesiredSize = new Size();

						m_tpPrimaryItemsControlPart.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
						primaryDesiredSize = m_tpPrimaryItemsControlPart.DesiredSize;

						FindMovablePrimaryCommands(
							availablePrimarySize.Width,
							primaryDesiredSize.Width,
							out primaryCommandsCountInTransition);

						if (primaryCommandsCountInTransition > 0)
						{
							TrimPrimaryCommandSeparatorInOverflow(ref primaryCommandsCountInTransition);

							if (primaryCommandsCountInTransition > 0)
							{
								if (!m_hasAlreadyFiredOverflowChangingEvent)
								{
									FireDynamicOverflowItemsChangingEvent(isForceToRestore: false);
								}

								// Update the transited  primary command min width that will be used for restoring the
								// primary commands from the overflow into the primary commands collection
								UpdatePrimaryCommandElementMinWidthInOverflow();

								MoveTransitionPrimaryCommandsIntoOverflow(primaryCommandsCountInTransition);

								// Update the overflow alignment for having toggle button
								SetOverflowStyleParams();

								// Save the current transition to compare with the next coming transition
								SaveMovedPrimaryCommandsIntoPreviousTransitionCollection();
							}

							// At this point, we'll have modified our primary and secondary command collections, which
							// impacts our visual state.  We should update our visual state to ensure that it's current.
							UpdateVisualState();
						}

					}
				}
				else if (m_lastAvailableWidth < availableSize.Width
					&& m_SecondaryCommandStartIndex > 0
					&& m_restorablePrimaryCommandMinWidth >= 0)
				{
					int restorableMinCount = 0;
					double restorablePrimaryCommandMinWidth = m_restorablePrimaryCommandMinWidth;

					double availableWidthToRestore = availableSize.Width - contentRootDesiredSize.Width;

					restorableMinCount = GetRestorablePrimaryCommandsMinimumCount();
					if (restorableMinCount > 0)
					{
						restorablePrimaryCommandMinWidth *= restorableMinCount;
					}

					if (availableWidthToRestore > restorablePrimaryCommandMinWidth)
					{
						FireDynamicOverflowItemsChangingEvent(true /* isForceToRestore */);
						m_hasAlreadyFiredOverflowChangingEvent = true;

						// There is the restorable primary commands from the overflow into the primary command collection.
						// Reset the dynamic primary and secondary commands collection then recalculate the primary commands
						// items control to fit the primary command with the current CommandBar available width.
						ResetDynamicCommands();
						SaveMovedPrimaryCommandsIntoPreviousTransitionCollection();
						m_tpPrimaryItemsControlPart.SetNeedsUpdateItems();


						SetOverflowStyleParams();

						// At this point, we'll have modified our primary and secondary command collections, which
						// impacts our visual state.  We should update our visual state to ensure that it's current.
						UpdateVisualState();
					}
				}
			}

			m_hasAlreadyFiredOverflowChangingEvent = false;
			m_lastAvailableWidth = availableSize.Width;

			return measureSize;
		}

		private protected override void ChangeVisualState(bool useTransitions)
		{
			base.ChangeVisualState(useTransitions);

			// AvailableCommandsStates
			{
				bool hasVisiblePrimaryElements = false;
				hasVisiblePrimaryElements = HasVisibleElements(m_tpDynamicPrimaryCommands);

				bool hasVisibleSecondaryElements = false;
				hasVisibleSecondaryElements = HasVisibleElements(m_tpDynamicSecondaryCommands);

				var state = (hasVisiblePrimaryElements && hasVisibleSecondaryElements ?
					"BothCommands" : (!hasVisibleSecondaryElements ? "PrimaryCommandsOnly" : "SecondaryCommandsOnly"));
				GoToState(useTransitions, state);
			}

			if (m_isDynamicOverflowEnabled)
			{
				GoToState(useTransitions, "DynamicOverflowEnabled");
			}
			else
			{
				GoToState(useTransitions, "DynamicOverflowDisabled");
			}

		}

		private void ConfigureItemsControls()
		{
			// UNO TODO: Do we really need this wrapping collection?
			ResetDynamicCommands();

			// The wrapping collections have a somewhat unusual reference pattern, which should be
			// documented here to avoid having it accidentally perturbed in the future in a way that
			// could lead to problems.  In short, the wrapping collections hold a reference to
			// the collections they wrap, but the wrapped collections don't hold any reference
			// in the reverse direction - aside from these tracker pointers, the only control
			// that holds a reference to the wrapping collections is the ItemsControl that
			// they're given to.  As a result, without these tracker pointers, the ItemsControl,
			// when it is deleted, would in turn delete the wrapping collection, since nothing else
			// holds a reference to it.  However, this is a problem since the wrapping collection adds
			// an event handler to the wrapped collection's VectorChanged event, which can lead to
			// the wrapped collection calling a method on the deleted wrapping collection
			// if we don't keep it alive after the ItemsControl is deleted.
			// The simplest way to keep it alive is just to have another reference to it,
			// which is what these tracker pointers do (until the CommandBar is deleted, at which point
			// that last reference will be removed and they'll be properly cleaned up).
			//m_tpDynamicPrimaryCommands?.Clear();
			//m_tpDynamicSecondaryCommands?.Clear();

			if (m_tpPrimaryItemsControlPart is { })
			{
				//ctl::ComPtr<wfc::IVector<xaml_controls::ICommandBarElement*>> spVector;
				//ctl::ComPtr<IterableWrappedObservableCollection<xaml_controls::ICommandBarElement>> spWrappedCollection;

				//IFC_RETURN(m_tpDynamicPrimaryCommands.As(&spVector))

				// Set the primary items control source.
				//IFC_RETURN(ctl::make(&spWrappedCollection));
				//IFC_RETURN(spWrappedCollection->SetWrappedCollection(spVector.Get()));
				//IFC_RETURN(m_tpPrimaryItemsControlPart->put_ItemsSource(ctl::as_iinspectable(spWrappedCollection.Get())));
				//SetPtrValue(m_tpWrappedPrimaryCommands, spWrappedCollection.Get());
				var spWrappedCollection = new IterableWrappedObservableCollection<ICommandBarElement>();
				//spWrappedCollection.SetWrappedCollection(m_tpDynamicPrimaryCommands);
				m_tpPrimaryItemsControlPart.ItemsSource = m_tpDynamicPrimaryCommands;
				m_tpWrappedPrimaryCommands = spWrappedCollection;
			}

			if (m_tpSecondaryItemsControlPart is { })
			{
				//ctl::ComPtr<wfc::IVector<xaml_controls::ICommandBarElement*>> spVector;
				//ctl::ComPtr<IterableWrappedObservableCollection<xaml_controls::ICommandBarElement>> spWrappedCollection;

				//IFC_RETURN(m_tpDynamicSecondaryCommands.As(&spVector))

				// Set the secondary items control source.
				//IFC_RETURN(ctl::make(&spWrappedCollection));
				//IFC_RETURN(spWrappedCollection->SetWrappedCollection(spVector.Get()));
				//IFC_RETURN(m_tpSecondaryItemsControlPart->put_ItemsSource(ctl::as_iinspectable(spWrappedCollection.Get())));
				//SetPtrValue(m_tpWrappedSecondaryCommands, spWrappedCollection.Get());

				var spWrappedCollection = new IterableWrappedObservableCollection<ICommandBarElement>();
				//spWrappedCollection.SetWrappedCollection(m_tpDynamicSecondaryCommands);
				m_tpSecondaryItemsControlPart.ItemsSource = m_tpDynamicSecondaryCommands;
				m_tpWrappedSecondaryCommands = spWrappedCollection;
			}
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			bool isHandled = false;
			isHandled = args.Handled;

			// Ignore already handled events
			if (isHandled)
			{
				return;
			}

			var key = args.Key;
			var originalKey = args.OriginalKey;

			// Determine whether this is a gamepad key event and whether it should be handled
			// by this method.  We only handle gamepad key events here if the menu is open
			// since it needs to by-pass the default gamepad navigation behavior to be
			// able to navigate into the overflow menu.
			bool isGamepadNavigationEvent = false;
			bool shouldHandleGamepadNavigationEvent = false;

			if (IsGamepadNavigationDirection(originalKey))
			{
				isGamepadNavigationEvent = true;

				bool isOpen = IsOpen;
				shouldHandleGamepadNavigationEvent = isOpen;
			}

			// Always handle non-gamepad events.  Only handle gamepad events when the menu is open.
			if (!isGamepadNavigationEvent || shouldHandleGamepadNavigationEvent)
			{
				bool wasHandled = false;
				switch (key)
				{
					case VirtualKey.Right:
					case VirtualKey.Left:
						wasHandled = OnLeftRightKeyDown(key == VirtualKey.Left);
						break;
					case VirtualKey.Up:
					case VirtualKey.Down:
						// Do not support focus wrapping for gamepad navigation because it can
						// trap the user which makes navigation difficult.
						wasHandled = OnUpDownKeyDown(key == VirtualKey.Up, !isGamepadNavigationEvent);
						break;
				}

				args.Handled = wasHandled;
			}
		}

		private bool OnLeftRightKeyDown(bool isLeftKey)
		{
			var wasHandled = false;

			// If the ALT key is not pressed, don't shift focus.
			var modifierKeys = VirtualKeyModifiers.None;
			GetKeyboardModifiers(out modifierKeys);
			if ((modifierKeys & VirtualKeyModifiers.Menu) != 0)
			{
				return false;
			}

			bool moveToRight = true;
			var flowDirection = FlowDirection.LeftToRight;

			// The direction that we'll shift through our lists is based on flow direction.
			flowDirection = FlowDirection;

			moveToRight = (flowDirection == FlowDirection.LeftToRight && !isLeftKey)
				|| (flowDirection == FlowDirection.RightToLeft && isLeftKey);

			ShiftFocusHorizontally(moveToRight);

			wasHandled = true;

			return wasHandled;
		}

		// If focus is on the expand button, then pressing up or down will
		// move focus to the overflow menu.
		private bool OnUpDownKeyDown(bool isUpKey, bool allowFocusWrap)
		{
			bool wasHandled = false;

			if (m_tpExpandButton == null)
			{
				return false;
			}

			// If the ALT key is not pressed, don't shift focus.
			var modifierKeys = VirtualKeyModifiers.None;
			GetKeyboardModifiers(out modifierKeys);
			if ((modifierKeys & VirtualKeyModifiers.Menu) != 0)
			{
				return false;
			}

			// Only handle the up/down keys when focus is on the more/expand button.
			var focusedElement = XamlRoot is null ?
				FocusManager.GetFocusedElement() :
				FocusManager.GetFocusedElement(XamlRoot);
			if (m_tpExpandButton == focusedElement)
			{
				bool isOpen = IsOpen;

				if (isOpen)
				{
					bool overflowOpensUp;
					overflowOpensUp = GetShouldOpenUp();

					if (isUpKey)
					{
						// We go to the last focusable element in the overflow if wrapping is allowed OR
						// if the overflow opens up.
						if (allowFocusWrap || overflowOpensUp)
						{
							// Focus the last element.
							SetFocusedElementInOverflow(false /* focusFirstElement */, out wasHandled);
						}
					}
					else
					{
						if (allowFocusWrap || !overflowOpensUp)
						{
							SetFocusedElementInOverflow(true /* focusFirstElement */, out wasHandled);
						}
					}
				}
				else
				{
					// Open the overflow menu and configure one of its items to get focus depending
					// on whether we are navigating up or down.
					m_overflowInitialFocusItem = (isUpKey ? OverflowInitialFocusItem.LastItem : OverflowInitialFocusItem.FirstItem);

					// Pressing up or down on the more/expand button opens the overflow.
					IsOpen = true;
					wasHandled = true;
				}
			}

			return wasHandled;
		}

		private void ShiftFocusVerticallyInOverflow(bool topToBottom, bool allowFocusWrap = true)
		{
			var focusedElement = XamlRoot is null ?
				FocusManager.GetFocusedElement() :
				FocusManager.GetFocusedElement(XamlRoot);
			DependencyObject? referenceElement = null;

			if (topToBottom)
			{
				referenceElement = FocusManager.FindLastFocusableElement(m_tpOverflowPresenterItemsPresenter);
			}
			else
			{
				referenceElement = FocusManager.FindFirstFocusableElement(m_tpOverflowPresenterItemsPresenter);
			}

			if (focusedElement == referenceElement)
			{
				bool overflowOpensUp;
				overflowOpensUp = GetShouldOpenUp();

				// We go to the expand button if wrapping is allowed OR one of the following is true:
				// a. Up key is pressed and the overflow opens down.
				// b. Down key is pressed and the overflow opens up.
				if (allowFocusWrap || !(overflowOpensUp ^ topToBottom))
				{
					this.SetFocusedElement(m_tpExpandButton, FocusState.Keyboard, animateIfBringIntoView: false);

					DXamlCore.Current.GetElementSoundPlayerServiceNoRef().RequestInteractionSoundForElement(ElementSoundKind.Focus, this);
				}
			}
			else
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				focusManager?.TryMoveFocusInstance(topToBottom ? FocusNavigationDirection.Down : FocusNavigationDirection.Up);
				DXamlCore.Current.GetElementSoundPlayerServiceNoRef().RequestInteractionSoundForElement(ElementSoundKind.Focus, this);
			}
		}

		private void HandleTabKeyPressedInOverflow(bool isShiftKeyPressed, out bool wasHandled)
		{
			wasHandled = false;

			var overflowNavigationMode = KeyboardNavigationMode.Local;
			if (m_tpSecondaryItemsControlPart is { })
			{
				overflowNavigationMode = m_tpSecondaryItemsControlPart.TabNavigation;
			}

			// If the overflow's tab navigation mode is set to once, always leave
			// on tab key presses.
			bool shouldFocusLeaveOverflow = (overflowNavigationMode == KeyboardNavigationMode.Once);

			// Otherwise, determine whether focus should leave the overflow menu based on whether
			// focus is currently on the first/last item depending on direction.
			if (!shouldFocusLeaveOverflow)
			{
				var focusedElement = XamlRoot is null ?
					FocusManager.GetFocusedElement() :
					FocusManager.GetFocusedElement(XamlRoot);
				DependencyObject? referenceElement = null;

				if (isShiftKeyPressed)
				{
					referenceElement = FocusManager.FindLastFocusableElement(m_tpOverflowPresenterItemsPresenter);
				}
				else
				{
					referenceElement = FocusManager.FindFirstFocusableElement(m_tpOverflowPresenterItemsPresenter);
				}

				// Only return focus to the bar if we're at either end of the
				// menu and moving focus would cause us to wrap around.
				shouldFocusLeaveOverflow = (focusedElement == referenceElement);
			}

			if (shouldFocusLeaveOverflow)
			{
				DependencyObject? focusCandidate = null;
				bool shouldMoveFocusOutsideOfCommandBar = false;

				if (isShiftKeyPressed)
				{
					// Backwards navigation out of the overflow menu always focuses the last focusable element
					// in the bar.
					focusCandidate = FocusManager.FindLastFocusableElement(this);
				}
				else
				{
					// Determine whether we should allow the focus to move outside of the CommandBar.
					// We should allow the focus to move out in the sticky case only.
					var isSticky = IsSticky;
					shouldMoveFocusOutsideOfCommandBar = isSticky;

					// If we should move outside of the CommandBar, then focus the last item in the CommandBar so that
					// we can later move focus again to focus the next element outside of the CommandBar.
					// Otherwise, we'll cycle focus back to the first item in the CommandBar.
					if (shouldMoveFocusOutsideOfCommandBar)
					{
						focusCandidate = FocusManager.FindLastFocusableElement(this);
					}
					else
					{
						focusCandidate = FocusManager.FindFirstFocusableElement(this);
					}
				}

				if (focusCandidate is { })
				{
					this.SetFocusedElement(
						focusCandidate,
						FocusState.Keyboard,
						animateIfBringIntoView: false,
						isProcessingTab: true,
						isShiftPressed: isShiftKeyPressed);

					if (shouldMoveFocusOutsideOfCommandBar)
					{
						// Make sure we don't try to override the following move focus operatiosn
						// otherwise we'll end up moving focus back into the overflow menu.
						m_skipProcessTabStopOverride = true;

						FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
						DXamlCore.Current.GetElementSoundPlayerServiceNoRef().RequestInteractionSoundForElement(ElementSoundKind.Focus, this);

						m_skipProcessTabStopOverride = false;
					}
				}

				wasHandled = true;
			}
		}

		// Pass in true to focus the first element in the overflow.
		// Pass in false to focus the last element.
		private void SetFocusedElementInOverflow(bool focusFirstElement, out bool wasFocusSet)
		{
			wasFocusSet = false;

			if (m_tpOverflowPresenterItemsPresenter == null)
			{
				return;
			}

			DependencyObject? focusableElement = null;
			if (focusFirstElement)
			{
				focusableElement = FocusManager.FindFirstFocusableElement(m_tpOverflowPresenterItemsPresenter);
			}
			else
			{
				focusableElement = FocusManager.FindLastFocusableElement(m_tpOverflowPresenterItemsPresenter);
			}

			if (focusableElement is { })
			{
				var wasFocusedUpdated = this.SetFocusedElement(focusableElement, FocusState.Keyboard, animateIfBringIntoView: false);
				wasFocusSet = wasFocusedUpdated;
			}
		}

		// Handle the cases where focus is currently in the CommandBar and the user hits Tab. If focus
		// is currently on the last focusable element in the bar and the user hits tab, we move the
		// focus onto the first focusable element in overflow menu rather than letting it go out of
		// the control.
		internal override TabStopProcessingResult ProcessTabStopOverride(
			DependencyObject? focusedElement,
			DependencyObject? candidateTabStopElement,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			// Give the AppBar code a chance to override the candidate.
			var result = base.ProcessTabStopOverride(focusedElement, candidateTabStopElement, isBackward, didCycleFocusAtRootVisualScope);

			// This method is only interested in the forward navigation case, so bail out early if otherwise.
			if (isBackward)
			{
				return result;
			}

			if (m_skipProcessTabStopOverride)
			{
				return result;
			}

			var isOpen = IsOpen;

			if (isOpen && m_tpOverflowPresenterItemsPresenter is { })
			{
				var lastFocusableElement = FocusManager.FindLastFocusableElement(this);

				// Move focus to the overflow menu when tabbing forwards and we're moving off
				// of the last focusable element in the bar.
				if (focusedElement != null && focusedElement == lastFocusableElement)
				{
					var newTabStop = FocusManager.FindFirstFocusableElement(m_tpOverflowPresenterItemsPresenter as ItemsPresenter);

					// If we found a candidate, then query its corresponding peer.
					if (newTabStop is { })
					{
						// If the AppBar overrode the tab stop, then we need to release its candidate otherwise
						// we'll leak
						if (result.IsOverriden)
						{
							MUX_ASSERT(result.NewTabStop != null);
							result.NewTabStop = null;
						}

						result.NewTabStop = newTabStop;
						result.IsOverriden = true;
					}
				}
			}

			return result;
		}

		// Handle the cases where focus is not currently in the CommandBar and the user hits Shift-Tab
		// to focus the control. If focus is moving onto the last focusable element in the bar, then we
		// instead move it onto the last focusable element in the overflow menu.
		// We also end up handling the case where the AppBar::ProcessTabStopOverride() implementation
		// tries to wrap focus back to the last element in the bar from the first element.  In that
		// situation, we also override it to move focus into the overflow menu instead.
		internal override TabStopProcessingResult ProcessCandidateTabStopOverride(
			DependencyObject? focusedElement,
			DependencyObject? candidateTabStopElement,
			DependencyObject? overriddenCandidateTabStopElement,
			bool isBackward)
		{
			var result = new TabStopProcessingResult()
			{
				NewTabStop = null,
				IsOverriden = false,
			};

			// This method is only interested in the backward navigation case, so bail out early if otherwise.
			if (!isBackward)
			{
				return result;
			}

			var isOpen = IsOpen;

			if (isOpen && m_tpOverflowPresenterItemsPresenter is { })
			{
				var lastFocusableElement = FocusManager.FindLastFocusableElement(this);

				// Move focus to the overflow menu when tabbing backwards and we're moving onto
				// the last focusable element in the bar.
				if (candidateTabStopElement != null && candidateTabStopElement == lastFocusableElement)
				{
					DependencyObject? newTabStop = null;

					// When overriding focus to go into the overflow menu, since TabNavigation==Once means that focus
					// only moves into a particular tree once, if that is set then the element we'll focus
					// is the first overflow item.  If it is any other value, we focus the last element since
					// this method is only applicable during backward navigation.
					{
						var overflowNavigationMode = KeyboardNavigationMode.Local;
						if (m_tpSecondaryItemsControlPart is { })
						{
							overflowNavigationMode = m_tpSecondaryItemsControlPart.TabNavigation;
						}

						if (overflowNavigationMode == KeyboardNavigationMode.Once)
						{
							newTabStop = FocusManager.FindFirstFocusableElement(m_tpOverflowPresenterItemsPresenter as ItemsPresenter);
						}
						else
						{
							newTabStop = FocusManager.FindLastFocusableElement(m_tpOverflowPresenterItemsPresenter as ItemsPresenter);
						}
					}

					if (newTabStop is { })
					{
						result.NewTabStop = newTabStop;
						result.IsOverriden = true;
					}
				}
			}
			return result;
		}

		private void ShiftFocusHorizontally(bool moveToRight)
		{
			// Determine whether we should shift focus horizontally.
			if (m_tpContentControl is { })
			{
				var focusedElement = XamlRoot is null ?
					FocusManager.GetFocusedElement() :
					FocusManager.GetFocusedElement(XamlRoot);

				// Don't do it if focus is in the custom content area.
				var isChildOfContentControl = m_tpContentControl.IsAncestorOf(focusedElement as DependencyObject);
				if (isChildOfContentControl)
				{
					// Bail out.
					return;
				}

				DependencyObject? referenceElement = null;

				if (moveToRight)
				{
					if (m_tpExpandButton is { })
					{
						referenceElement = m_tpExpandButton;
					}

					if (referenceElement == null && m_tpPrimaryItemsControlPart is { })
					{
						referenceElement = FocusManager.FindLastFocusableElement(m_tpPrimaryItemsControlPart);
					}
				}
				else
				{
					if (m_tpPrimaryItemsControlPart is { })
					{
						referenceElement = FocusManager.FindFirstFocusableElement(m_tpPrimaryItemsControlPart);
					}

					if (referenceElement == null && m_tpExpandButton is { })
					{
						referenceElement = m_tpExpandButton;
					}
				}

				if (focusedElement == referenceElement)
				{
					// Bail out.
					return;
				}
			}

			var focusManager = VisualTree.GetFocusManagerForElement(this);
			focusManager?.TryMoveFocusInstance(moveToRight ? FocusNavigationDirection.Right : FocusNavigationDirection.Left);
			DXamlCore.Current.GetElementSoundPlayerServiceNoRef().RequestInteractionSoundForElement(ElementSoundKind.Focus, this);
		}

		protected override void OnOpening(object e)
		{
			base.OnOpening(e);
			SetCompactMode(false);


			if (m_tpOverflowPopup is { })
			{
				m_tpOverflowPopup.IsOpen = true;
			}

			//UpdateInputDeviceTypeUsedToOpen();

			// After we call OnOpeningImpl, we'll make a call to UpdateVisualState which
			// uses the height of the overflow content root to determine whether it should
			// open up or down.  To make sure it is up-to-date for that call, we update
			// the menu's layout here.
			if (m_tpOverflowContentRoot is { })
			{
				m_tpOverflowContentRoot.UpdateLayout();
			}
		}

		protected override void OnClosing(object e)
		{
			base.OnClosing(e);
			CloseSubMenus(null, false);
		}

		protected override void OnClosed(object e)
		{
			SetCompactMode(true);

			if (m_tpOverflowPopup is { })
			{
				m_tpOverflowPopup.IsOpen = false;
			}

			// We need to call this last, rather than first (the usual pattern),
			// because this raises an event that apps can use to set IsOpen = true.
			// If that happened before the above, then we would effectively
			// see a sequence of Open -> Open -> Close, instead of
			// Open -> Close -> Open, which gets us into a state where
			// CompactMode is still set to true even when the CommandBar is open,
			// hiding AppBarButton labels and the overflow popup.
			base.OnClosed(e);
		}

		private void SetOverflowStyleAndInputModeOnSecondaryCommand(int index, bool isItemInOverflow)
		{
			var element = m_tpDynamicSecondaryCommands?[index];

			if (element is { })
			{
				SetOverflowStyleUsage(element, isItemInOverflow);
			}
			//DirectUI::InputDeviceType inputType = isItemInOverflow ? m_inputDeviceTypeUsedToOpen : DirectUI::InputDeviceType::None;
			//IFC_RETURN(SetInputModeOnSecondaryCommand(index, inputType));
		}

		private void SetOverflowStyleUsage(ICommandBarElement? element, bool isItemInOverflow)
		{
			if (element is ICommandBarOverflowElement elementAsOverflow)
			{
				elementAsOverflow.UseOverflowStyle = isItemInOverflow;
			}
		}
		//		_Check_return_ HRESULT CommandBar::UpdateInputDeviceTypeUsedToOpen()
		//{
		//    CContentRoot* contentRoot = VisualTree::GetContentRootForElement(GetHandle());
		//		m_inputDeviceTypeUsedToOpen = contentRoot->GetInputManager().GetLastInputDeviceType();

		//		UINT32 itemCount = 0;
		//		IFC_RETURN(m_tpDynamicSecondaryCommands.Get()->get_Size(&itemCount));
		//    for (UINT32 i = 0; i<itemCount; ++i)
		//    {
		//        IFC_RETURN(SetInputModeOnSecondaryCommand(i, m_inputDeviceTypeUsedToOpen));
		//	}

		//    return S_OK;
		//}

		//		_Check_return_ HRESULT CommandBar::SetInputModeOnSecondaryCommand(UINT32 index, DirectUI::InputDeviceType inputType)
		//{
		//    ctl::ComPtr<xaml_controls::ICommandBarElement> spElement;
		//		ctl::ComPtr<xaml_controls::IAppBarButton> spElementAsAppBarButton;
		//		ctl::ComPtr<xaml_controls::IAppBarToggleButton> spElementAsAppBarToggleButton;

		//		IFC_RETURN(m_tpDynamicSecondaryCommands.Get()->GetAt(index, &spElement));

		//    if (spElement)
		//    {
		//        // Only AppBarButton and AppBarToggleButton support SetInputMode.
		//        // We ignore other items such as AppBarSeparator.
		//        spElementAsAppBarToggleButton = spElement.AsOrNull<xaml_controls::IAppBarToggleButton>();
		//        if (spElementAsAppBarToggleButton)
		//        {
		//            static_cast<AppBarToggleButton*>(spElementAsAppBarToggleButton.Get())->SetInputMode(inputType);
		//	}

		//	spElementAsAppBarButton = spElement.AsOrNull<xaml_controls::IAppBarButton>();
		//        if (spElementAsAppBarButton)
		//        {
		//            static_cast<AppBarButton*>(spElementAsAppBarButton.Get())->SetInputMode(inputType);
		//}
		//    }

		//    return S_OK;
		//}

		private void SetOverflowStyleParams()
		{
			// We need to check to see if there are any AppBarToggleButtons in the list of secondary commands
			// and inform any AppBarButtons of that fact either way to ensure that
			// their text is always aligned with those of AppBarToggleButtons
			// There's no easy way to get specifically when AppBarToggleButtons are added or removed
			// since ItemRemoved doesn't provide a pointer to the removed item,
			// so we'll just iterate through the whole vector each time it changes to see
			// how many toggle buttons we have. We do a similar check for Icons to ensure
			// space is provided and all items are aligned.
			// The vector will basically never have more than ~10 items in it,
			// so the running time of this loop will be trivial.
			bool hasAppBarToggleButtons = false;
			bool hasAppBarIcons = false;
			bool hasAppBarAcceleratorText = false;

			int itemCount = 0;

			itemCount = m_tpDynamicSecondaryCommands?.Count ?? 0;
			for (int i = 0; i < itemCount; ++i)
			{
				var element = m_tpDynamicSecondaryCommands?[i];

				if (element is { })
				{
					AppBarButton? elementAsAppBarButton = null;
					AppBarToggleButton? elementAsAppBarToggleButton = null;
					IconElement? icon = null;
					string? acceleratorText = null;

					elementAsAppBarButton = element as AppBarButton;
					if (elementAsAppBarButton is { })
					{
						icon = elementAsAppBarButton.Icon;
						hasAppBarIcons = hasAppBarIcons || icon is { };

						acceleratorText = elementAsAppBarButton.KeyboardAcceleratorTextOverride;
						hasAppBarAcceleratorText = hasAppBarAcceleratorText || !string.IsNullOrWhiteSpace(acceleratorText);
					}
					else
					{
						elementAsAppBarToggleButton = element as AppBarToggleButton;
						if (elementAsAppBarToggleButton is { })
						{
							hasAppBarToggleButtons = true;

							icon = elementAsAppBarToggleButton.Icon;
							hasAppBarIcons = hasAppBarIcons || icon is { };

							acceleratorText = elementAsAppBarToggleButton.KeyboardAcceleratorTextOverride;
							hasAppBarAcceleratorText = hasAppBarAcceleratorText || !string.IsNullOrWhiteSpace(acceleratorText);
						}
					}

					if (hasAppBarIcons && hasAppBarToggleButtons && hasAppBarAcceleratorText)
					{
						break;
					}
				}
			}

			for (int i = 0; i < itemCount; ++i)
			{
				var element = m_tpDynamicSecondaryCommands?[i];

				if (element is { })
				{
					AppBarButton? elementAsAppBarButton = null;
					AppBarToggleButton? elementAsAppBarToggleButton = null;

					elementAsAppBarButton = element as AppBarButton;
					if (elementAsAppBarButton is { })
					{
						elementAsAppBarButton.SetOverflowStyleParams(hasAppBarIcons, hasAppBarToggleButtons, hasAppBarAcceleratorText);
					}
					else
					{
						elementAsAppBarToggleButton = element as AppBarToggleButton;
						if (elementAsAppBarToggleButton is { })
						{
							elementAsAppBarToggleButton.SetOverflowStyleParams(hasAppBarIcons, hasAppBarAcceleratorText);
						}
					}
				}
			}
		}

		private void PropagateDefaultLabelPosition()
		{
			var primaryItemCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

			for (int i = 0; i < primaryItemCount; ++i)
			{
				var spElement = m_tpDynamicPrimaryCommands?[i];

				if (spElement is { })
				{
					PropagateDefaultLabelPositionToElement(spElement);
				}
			}

			var secondaryItemCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

			for (int i = 0; i < secondaryItemCount; ++i)
			{
				var spElement = m_tpDynamicSecondaryCommands?[i];

				if (spElement is { })
				{
					PropagateDefaultLabelPositionToElement(spElement);
				}
			}
		}

		private void PropagateDefaultLabelPositionToElement(ICommandBarElement element)
		{
			if (element is ICommandBarLabeledElement spElementAsLabeledElement)
			{
				var defaultLabelPosition = DefaultLabelPosition;
				spElementAsLabeledElement.SetDefaultLabelPosition(defaultLabelPosition);
			}
		}

		//		CommandBar::HasBottomLabel(BOOLEAN* hasBottomLabel)
		//{
		//    xaml_controls::CommandBarDefaultLabelPosition defaultLabelPosition = xaml_controls::CommandBarDefaultLabelPosition_Bottom;
		//    *hasBottomLabel = FALSE;

		//    IFC_RETURN(get_DefaultLabelPosition(&defaultLabelPosition));

		//    if (defaultLabelPosition == xaml_controls::CommandBarDefaultLabelPosition_Bottom)
		//    {
		//        UINT32 primaryItemsCount = 0;
		//		IFC_RETURN(m_tpDynamicPrimaryCommands.Get()->get_Size(&primaryItemsCount));
		//        for (UINT32 i = 0; i<primaryItemsCount; ++i)
		//        {
		//            ctl::ComPtr<xaml_controls::ICommandBarElement> element;
		//		IFC_RETURN(m_tpDynamicPrimaryCommands.Get()->GetAt(i, &element));

		//            auto elementAsLabeledElement = element.AsOrNull<xaml_controls::ICommandBarLabeledElement>();
		//            if (elementAsLabeledElement)
		//            {
		//                IFC_RETURN(elementAsLabeledElement->GetHasBottomLabel(hasBottomLabel));

		//                if (* hasBottomLabel)
		//                {
		//                    break;
		//                }
		//}
		//        }
		//    }

		//    return S_OK;
		//}


		private bool IsGamepadNavigationDirection(VirtualKey key)
		{
			return
				IsGamepadNavigationRight(key) ||
				IsGamepadNavigationLeft(key) ||
				IsGamepadNavigationUp(key) ||
				IsGamepadNavigationDown(key);
		}

		bool IsGamepadNavigationRight(VirtualKey key)
		{
			return key == VirtualKey.GamepadLeftThumbstickRight || key == VirtualKey.GamepadDPadRight;
		}

		bool IsGamepadNavigationLeft(VirtualKey key)
		{
			return key == VirtualKey.GamepadLeftThumbstickLeft || key == VirtualKey.GamepadDPadLeft;
		}

		bool IsGamepadNavigationUp(VirtualKey key)
		{
			return key == VirtualKey.GamepadLeftThumbstickUp || key == VirtualKey.GamepadDPadUp;
		}

		bool IsGamepadNavigationDown(VirtualKey key)
		{
			return key == VirtualKey.GamepadLeftThumbstickDown || key == VirtualKey.GamepadDPadDown;
		}


		private void OnPrimaryCommandsChanged(IObservableVector<ICommandBarElement> sender, IVectorChangedEventArgs pArgs)
		{
			ResetDynamicCommands();

			var isOpen = IsOpen;

			var shouldBeCompact = !isOpen;

			var change = pArgs.CollectionChange;
			var changeIndex = pArgs.Index;

			if (change == CollectionChange.ItemInserted ||
				change == CollectionChange.ItemChanged)
			{
				var element = m_tpDynamicPrimaryCommands?[(int)changeIndex];
				if (element is { })
				{
					element.IsCompact = shouldBeCompact;
					PropagateDefaultLabelPositionToElement(element);
				}
			}
			else if (change == CollectionChange.Reset)
			{
				SetCompactMode(shouldBeCompact);

				var itemCount = m_tpDynamicPrimaryCommands?.Count ?? 0;
				for (int i = 0; i < itemCount; ++i)
				{
					var element = m_tpDynamicPrimaryCommands?[i];
					if (element is { })
					{
						PropagateDefaultLabelPositionToElement(element);
					}
				}
			}

			InvalidateMeasure();

			UpdateVisualState();
		}

		// Used to *set* overflow style state on items that are entering
		// the secondary items vector.
		private void OnSecondaryCommandsChanged(IObservableVector<ICommandBarElement> sender, IVectorChangedEventArgs pArgs)
		{
			ResetDynamicCommands();

			var change = pArgs.CollectionChange;
			var changeIndex = pArgs.Index;

			SetOverflowStyleParams();

			if (change == CollectionChange.ItemInserted ||
				change == CollectionChange.ItemChanged)
			{
				var element = m_tpDynamicSecondaryCommands?[(int)changeIndex];

				if (element is { })
				{
					PropagateDefaultLabelPositionToElement(element);
					SetOverflowStyleAndInputModeOnSecondaryCommand((int)changeIndex, true);
					PropagateDefaultLabelPositionToElement(element);
				}
			}
			else if (change == CollectionChange.Reset)
			{
				var itemCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

				for (int i = 0; i < itemCount; ++i)
				{
					var element = m_tpDynamicSecondaryCommands?[i];

					if (element is { })
					{
						SetOverflowStyleAndInputModeOnSecondaryCommand(i, true);
						PropagateDefaultLabelPositionToElement(element);
					}
				}
			}

			InvalidateMeasure();
			UpdateVisualState();
			UpdateTemplateSettings();
		}

		private void OnSecondaryItemsControlLoaded(object sender, RoutedEventArgs e)
		{
			// Hook-up a key-down handler to the overflow presenter's items presenter to override the
			// default arrow key behavior of the ScrollViewer, which scrolls the view.  We instead
			// want arrow keys to shift focus up/down.
			if (m_tpOverflowPresenterItemsPresenter == null)
			{
				m_tpOverflowPresenterItemsPresenter = m_tpSecondaryItemsControlPart?.GetTemplateChild<ItemsPresenter>("ItemsPresenter");

				if (m_tpOverflowPresenterItemsPresenter is { })
				{
					m_tpOverflowPresenterItemsPresenter.KeyDown += OnOverflowContentKeyDown;
					m_overflowPresenterItemsPresenterKeyDownEventHandler.Disposable = Disposable.Create(() => m_tpOverflowPresenterItemsPresenter.KeyDown -= OnOverflowContentKeyDown);
				}
			}

			// Set focus to a particular item if the overflow was opened by arrowing
			// up/down while focus was on the more button.
			if (m_overflowInitialFocusItem != OverflowInitialFocusItem.None)
			{
				SetFocusedElementInOverflow(m_overflowInitialFocusItem == OverflowInitialFocusItem.FirstItem, out _);
				m_overflowInitialFocusItem = OverflowInitialFocusItem.None;
			}
		}

		private void OnOverflowContentRootSizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (m_tpSecondaryItemsControlPart is CommandBarOverflowPresenter overflowPresenter)
			{
				var shouldUseFullWidth = GetShouldOverflowOpenInFullWidth();
				var shouldOpenUp = GetShouldOpenUp();

				overflowPresenter.SetDisplayModeState(shouldUseFullWidth, shouldOpenUp);
			}

			UpdateTemplateSettings();
		}

		private void TryDismissCommandBarOverflow()
		{
			var isSticky = IsSticky;

			if (!isSticky)
			{
				IsOpen = false;
			}

			//In either case (sticky v/s non-sticky) focus should go to the expand button
			RestoreFocusToExpandButton();
		}

		private void OnOverflowContentKeyDown(object sender, KeyRoutedEventArgs e)
		{
			var key = VirtualKey.None;
			var originalKey = VirtualKey.None;

			key = e.Key;
			originalKey = e.OriginalKey;

			bool wasHandledLocally = false;

			switch (originalKey)
			{
				case VirtualKey.GamepadB:
				case VirtualKey.Escape:
					TryDismissCommandBarOverflow();
					wasHandledLocally = true;
					break;
				case VirtualKey.GamepadDPadLeft:
				case VirtualKey.GamepadDPadRight:
				case VirtualKey.GamepadLeftThumbstickLeft:
				case VirtualKey.GamepadLeftThumbstickRight:
				case VirtualKey.Left:
				case VirtualKey.Right:
					//Mark these keys handled so that Auto-focus doesn't break the focus trap
					wasHandledLocally = true;
					break;

				case VirtualKey.GamepadDPadUp:
				case VirtualKey.GamepadLeftThumbstickUp:
				case VirtualKey.GamepadDPadDown:
				case VirtualKey.GamepadLeftThumbstickDown:
					ShiftFocusVerticallyInOverflow(
						key == VirtualKey.Down || key == VirtualKey.GamepadDPadDown || key == VirtualKey.GamepadLeftThumbstickDown,
						false /* allowFocusWrap */);
					wasHandledLocally = true;
					break;

				case VirtualKey.Up:
				case VirtualKey.Down:
					ShiftFocusVerticallyInOverflow(key == VirtualKey.Down);
					wasHandledLocally = true;
					break;
				case VirtualKey.Tab:
					GetKeyboardModifiers(out var modifierKeys);
					HandleTabKeyPressedInOverflow((modifierKeys & VirtualKeyModifiers.Shift) != 0, out wasHandledLocally);
					break;
			}

			if (wasHandledLocally)
			{
				e.Handled = true;
			}
		}

		internal static void OnCommandExecutionStatic(ICommandBarElement element)
		{
			CommandBar? parentCmdBar;
			FindParentCommandBarForElement(element, out parentCmdBar);

			if (parentCmdBar is { })
			{
				parentCmdBar.IsOpen = false;
			}
		}

		internal static void OnCommandBarElementVisibilityChanged(ICommandBarElement element)
		{
			CommandBar? parentCmdBar;
			FindParentCommandBarForElement(element, out parentCmdBar);

			if (parentCmdBar is { })
			{
				parentCmdBar.UpdateVisualState();
			}
		}

		protected override bool ContainsElement(DependencyObject pElement)
		{
			bool isAncestorOfElement = false;

			isAncestorOfElement = this.IsAncestorOf(pElement);

			if (!isAncestorOfElement && m_tpOverflowContentRoot is { })
			{
				isAncestorOfElement = m_tpOverflowContentRoot.IsAncestorOf(pElement);
			}

			return isAncestorOfElement;
		}

		private void RestoreFocusToExpandButton()
		{
			if (m_tpExpandButton is { })
			{
				var focusedElement = this.GetFocusedElement();

				if (focusedElement is { })
				{
					bool isOverflowPopupAncestorOfElement = false;
					if (m_tpOverflowContentRoot is { })
					{
						isOverflowPopupAncestorOfElement = m_tpOverflowContentRoot.IsAncestorOf(focusedElement as DependencyObject);
					}

					// Focus is in the overflow menu, so restore it to the expand button now that we're
					// closing.
					if (isOverflowPopupAncestorOfElement)
					{
						var focusState = GetFocusState(focusedElement as DependencyObject);
						this.SetFocusedElement(m_tpExpandButton, focusState, animateIfBringIntoView: false);
					}
				}
			}
		}

		protected override void RestoreSavedFocusImpl(DependencyObject? savedFocusedElement, FocusState savedFocusState)
		{
			// If we did save focus from a previous element when opening, then defer to the AppBar's
			// implemenation to restore it.  The CommandBar handles the case where there was no
			// saved element and restores focus to the expand button if it was previously in
			// the overflow menu.
			if (savedFocusedElement is { })
			{
				base.RestoreSavedFocusImpl(savedFocusedElement, savedFocusState);
			}
			else
			{
				RestoreFocusToExpandButton();
			}
		}

		//Returns true if the currently focused element
		//is a child of CommandBar OR lives in m_tpOverflowContentRoot's sub-tree
		//_Check_return_ HRESULT
		//CommandBar::HasFocus(_Out_ BOOLEAN * hasFocus)
		//{

		//	IFCPTR_RETURN(hasFocus);
		//	*hasFocus = FALSE;

		//	ctl::ComPtr<DependencyObject> focusedElement;
		//		IFC_RETURN(GetFocusedElement(&focusedElement));

		//	bool containsElement = false;
		//		IFC_RETURN(ContainsElement(focusedElement.Get(), &containsElement));

		//	*hasFocus = containsElement;

		//	return S_OK;
		//}

		protected override void UpdateTemplateSettings()
		{
			var templateSettings = CommandBarTemplateSettings;
			var appBarTemplateSettings = TemplateSettings;

			var isOpen = IsOpen;

			if (isOpen)
			{
				var contentHeight = ContentHeight;
				templateSettings.ContentHeight = contentHeight;

				var visibleBounds = new Rect();
				var availableBounds = new Rect();

				visibleBounds = Windows.UI.Xaml.Window.IReallyUseCurrentWindow?.Bounds ?? XamlRoot?.Bounds ?? default;

				bool windowed = false;
				//windowed = m_tpOverflowPopup && m_tpOverflowPopup.Cast<Popup>()->IsWindowed();
				if (windowed)
				{
					// UNO TODO: Windowed modes are not supported
					//ctl::ComPtr<xaml_media::IGeneralTransform> transform;
					//IFC_RETURN(TransformToVisual(nullptr, &transform));

					//wf::Point topLeftPoint = { 0, 0 };
					//IFC_RETURN(transform->TransformPoint(topLeftPoint, &topLeftPoint));

					//IFC_RETURN(DXamlCore::GetCurrent()->CalculateAvailableMonitorRect(this, topLeftPoint, &availableBounds));
				}
				else
				{
					availableBounds = visibleBounds;
				}

				bool shouldUseFullWidth = false;
				shouldUseFullWidth = GetShouldOverflowOpenInFullWidth();

				double overflowContentMaxWidth = m_overflowContentMaxWidth;
				double overflowContentMinWidth;

				if (shouldUseFullWidth)
				{
					overflowContentMinWidth = visibleBounds.Width;
					overflowContentMaxWidth = visibleBounds.Width;
				}
				//else if (m_inputDeviceTypeUsedToOpen == InputDeviceType::Touch ||
				//m_inputDeviceTypeUsedToOpen == DirectUI::InputDeviceType::GamepadOrRemote)
				//{
				//	overflowContentMinWidth = m_overflowContentTouchMinWidth;
				//}
				else
				{
					overflowContentMinWidth = m_overflowContentMinWidth;
				}

				templateSettings.OverflowContentMinWidth = overflowContentMinWidth;
				templateSettings.OverflowContentMaxWidth = overflowContentMaxWidth;

				double overflowContentMaxHeight = availableBounds.Height * 0.5;

				// When we're using a windowed popup, we add an extra margin to provide
				// enough space to do translate transforms that our animations need.
				// We'll add its height to the max height.
				if (m_tpWindowedPopupPadding is { })
				{
					double windowedPopupPaddingHeight;
					windowedPopupPaddingHeight = m_tpWindowedPopupPadding.ActualHeight;
					overflowContentMaxHeight += windowedPopupPaddingHeight;
				}

				templateSettings.OverflowContentMaxHeight = overflowContentMaxHeight;

				Size overflowContentSize = GetOverflowContentSize();
				templateSettings.OverflowContentClipRect = new Rect(0, 0, overflowContentSize.Width, overflowContentSize.Height - (m_tpWindowedPopupPadding is { } ? contentHeight : 0));

				var compactVerticalDelta = appBarTemplateSettings.CompactVerticalDelta;
				var minimalVerticalDelta = appBarTemplateSettings.MinimalVerticalDelta;
				var hiddenVerticalDelta = appBarTemplateSettings.HiddenVerticalDelta;

				templateSettings.OverflowContentCompactYTranslation = -overflowContentSize.Height + compactVerticalDelta;
				templateSettings.OverflowContentMinimalYTranslation = -overflowContentSize.Height + minimalVerticalDelta;
				templateSettings.OverflowContentHiddenYTranslation = -overflowContentSize.Height + hiddenVerticalDelta;

				double contentHeightForAnimation = overflowContentSize.Height;


				// If the overflow popup is windowed, we'll have already accounted for the size of the
				// main content in terms of transformation, so we'll only animate the remainder.

				// UNO TODO: Windowed modes are not supported
				//if (m_tpOverflowPopup is { })
				//{
				//	var closedDisplayMode = AppBarClosedDisplayMode.Hidden;
				//	closedDisplayMode = ClosedDisplayMode;

				//	switch (closedDisplayMode)
				//	{
				//		case AppBarClosedDisplayMode.Compact:
				//			contentHeightForAnimation -= m_compactHeight;
				//			break;

				//		case AppBarClosedDisplayMode.Minimal:
				//			contentHeightForAnimation -= m_minimalHeight;
				//			break;

				//		case AppBarClosedDisplayMode.Hidden:
				//		default:
				//			// The hidden height is zero, so nothing to do here.
				//			break;
				//	}
				//}

				templateSettings.OverflowContentHeight = contentHeightForAnimation;
				templateSettings.NegativeOverflowContentHeight = -contentHeightForAnimation;

				double overflowContentHorizontalOffset = 0.0;
				overflowContentHorizontalOffset = CalculateOverflowContentHorizontalOffset(overflowContentSize, availableBounds);
				templateSettings.OverflowContentHorizontalOffset = overflowContentHorizontalOffset;
			}

			base.UpdateTemplateSettings();

			var overflowButtonVisibility = OverflowButtonVisibility;

			bool shouldShowOverflowButton = false;

			if (overflowButtonVisibility == CommandBarOverflowButtonVisibility.Visible)
			{
				shouldShowOverflowButton = true;
			}
			else if (overflowButtonVisibility == CommandBarOverflowButtonVisibility.Auto)
			{
				// In the auto case, we should show the overflow button in one of three circumstances:
				// when we have at least one element in the secondary items collection, or when there is
				// a delta between the compact height and the height of the CommandBar, or when our
				// closed display mode is something other than compact.
				int secondaryItemsCount = 0;

				secondaryItemsCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

				if (secondaryItemsCount > 0)
				{
					shouldShowOverflowButton = true;
				}
				else
				{
					var closedDisplayMode = ClosedDisplayMode;

					if (closedDisplayMode != AppBarClosedDisplayMode.Compact)
					{
						shouldShowOverflowButton = true;
					}
					else
					{
						var compactVerticalDelta = appBarTemplateSettings.CompactVerticalDelta;

						shouldShowOverflowButton = !compactVerticalDelta.IsZero();
					}
				}
			}

			templateSettings.EffectiveOverflowButtonVisibility = shouldShowOverflowButton ? Visibility.Visible : Visibility.Collapsed;

			double maxAppBarKeyboardAcceleratorTextWidth = 0;

			var itemCount = m_tpDynamicSecondaryCommands?.Count ?? 0;
			for (int i = 0; i < itemCount; ++i)
			{
				var element = m_tpDynamicSecondaryCommands?[i];
				if (element is { })
				{
					var desiredSize = new Size();
					double desiredWidth = 0;

					if (element is AppBarButton elementAsAppBarButton)
					{
						desiredSize = elementAsAppBarButton.GetKeyboardAcceleratorTextDesiredSize();
					}
					else if (element is AppBarToggleButton elementAsAppBarToggleButton)
					{
						desiredSize = elementAsAppBarToggleButton.GetKeyboardAcceleratorTextDesiredSize();
					}

					desiredWidth = desiredSize.Width;

					if (desiredWidth > maxAppBarKeyboardAcceleratorTextWidth)
					{
						maxAppBarKeyboardAcceleratorTextWidth = desiredWidth;
					}
				}
			}

			for (int i = 0; i < itemCount; ++i)
			{
				var element = m_tpDynamicSecondaryCommands?[i];
				if (element is { })
				{
					if (element is AppBarButton elementAsAppBarButton)
					{
						elementAsAppBarButton.UpdateTemplateSettings(maxAppBarKeyboardAcceleratorTextWidth);
					}
					else if (element is AppBarToggleButton elementAsAppBarToggleButton)
					{
						elementAsAppBarToggleButton.UpdateTemplateSettings(maxAppBarKeyboardAcceleratorTextWidth);
					}
				}
			}
		}

		private bool GetShouldOverflowOpenInFullWidth()
		{
			var visibleBounds = Windows.UI.Xaml.Window.IReallyUseCurrentWindow?.Bounds ?? XamlRoot?.Bounds ?? default;

			// IFC_RETURN(DXamlCore::GetCurrent()->GetVisibleContentBoundsForElement(GetHandle(), &visibleBounds));

			return visibleBounds.Width <= m_overflowContentMaxWidth;
		}

		protected override void GetVerticalOffsetNeededToOpenUp(out double neededOffset, out bool opensWindowed)
		{
			base.GetVerticalOffsetNeededToOpenUp(out neededOffset, out opensWindowed);

			var templateSettings = CommandBarTemplateSettings;

			var overflowContentHeight = templateSettings.OverflowContentHeight;

			// Add the height of the overflow content.
			neededOffset += overflowContentHeight;

			// We open windowed if our popup is windowed.
			// opensWindowed = m_tpOverflowPopup && !!m_tpOverflowPopup.Cast<Popup>()->IsWindowed();
		}

		private Size GetOverflowContentSize()
		{
			var overfowContentSize = new Size(0, 0);


			if (m_tpOverflowContentRoot is { })
			{
				var hasVisibleSecondaryElements = HasVisibleElements(m_tpDynamicSecondaryCommands);

				// Only measure the overflow content control if it should be visible, otherwise we will be
				// unnecessarily expanding the content control template underneath it during the measure pass.
				if (hasVisibleSecondaryElements)
				{
					m_tpOverflowContentRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					overfowContentSize = m_tpOverflowContentRoot.DesiredSize;
				}
			}

			return overfowContentSize;
		}

		// Calculate OverflowContentHorizontalOffset.
		// By default align the menu with the right edge of the CommandBar.
		// If there is not enough space to flow to the right, align it with the left edge.
		// If it still can't fit by aligning left, then align with the edge of the screen.
		private double CalculateOverflowContentHorizontalOffset(Size overflowContentSize, Rect visibleBounds)
		{
			var offset = 0d;

			var transform = TransformToVisual(null);

			var offsetFromRoot = transform.TransformPoint(new Point(0, 0));

			var actualWidth = ActualWidth;

			// Try to align with the right edge by default.
			offset = actualWidth - overflowContentSize.Width;
			if (offset < 0)
			{
				// If the offset is negative, then the overflow menu is bigger than the CommandBar
				// so we have to make sure it doesn't flow off the edge of the window.
				var flowDirection = FlowDirection;

				// For LTR, if the sum of the offsets is negative, then the menu is flowing over
				// the edge of the window.
				// For RTL, test the difference of the offsets to see if it's greater than
				// the window width.
				if (((flowDirection == FlowDirection.LeftToRight) && ((offsetFromRoot.X + offset) < 0))
					|| ((flowDirection == FlowDirection.RightToLeft) && ((offsetFromRoot.X - offset) > visibleBounds.Width)))
				{
					// If we can align it with the left of the bar, then do that.
					// Otherwise, align it with the left edge of the window.
					if (((flowDirection == FlowDirection.LeftToRight) && (offsetFromRoot.X + overflowContentSize.Width <= visibleBounds.Width))
						|| ((flowDirection == FlowDirection.RightToLeft) && (offsetFromRoot.X - overflowContentSize.Width >= 0)))
					{
						// We can fit it by aligning left, so do that by setting the offset to 0.
						offset = 0;
					}
					else
					{
						// Align to the window edge.
						offset = flowDirection == FlowDirection.LeftToRight ? -offsetFromRoot.X : offsetFromRoot.X - visibleBounds.Width;
					}
				}
			}

			return offset;
		}


		/*
		static _Check_return_ HRESULT DoCollectionOperation(
    _In_ CommandBarElementCollection* collection,
    _In_ DeferredElementStateChange state,
    _In_ UINT32 collectionIndex,
    _In_ CDependencyObject* realizedElement)
{
    ctl::ComPtr<xaml_controls::ICommandBarElement> realizedElementAsICBE;
    ctl::ComPtr<DependencyObject> realizedElementDO;
    IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(realizedElement, &realizedElementDO));
    IFC_RETURN(realizedElementDO.As(&realizedElementAsICBE));

    switch (state)
    {
        case DeferredElementStateChange::Realized:
            IFC_RETURN(collection->InsertAt(collectionIndex, realizedElementAsICBE.Get()));
            break;

        case DeferredElementStateChange::Deferred:
            {
                UINT index = 0;
                BOOLEAN found = FALSE;

                IFC_RETURN(collection->IndexOf(
                    realizedElementAsICBE.Get(),
                    &index,
                    &found));

                if (found)
                {
                    IFC_RETURN(collection->RemoveAt(index));
                }
                // if not found, it's ok.  It might have been removed programatically.
            }
            break;

        default:
            ASSERT(false);
    }

    return S_OK;
}

_Check_return_ HRESULT CommandBar::NotifyDeferredElementStateChanged(
    _In_ KnownPropertyIndex propertyIndex,
    _In_ DeferredElementStateChange state,
    _In_ UINT32 collectionIndex,
    _In_ CDependencyObject* realizedElement)
{
    switch (propertyIndex)
    {
        case KnownPropertyIndex::CommandBar_PrimaryCommands:
            IFC_RETURN(DoCollectionOperation(
                m_tpPrimaryCommands.Get(),
                state,
                collectionIndex,
                realizedElement));
            break;

        case KnownPropertyIndex::CommandBar_SecondaryCommands:
            IFC_RETURN(DoCollectionOperation(
                m_tpSecondaryCommands.Get(),
                state,
                collectionIndex,
                realizedElement));
            break;

        default:
            // Should not be calling framework for any other properties
            ASSERT(false);
            break;
    }

    return S_OK;
}
		 */

		private static bool HasVisibleElements(ObservableCollection<ICommandBarElement>? collection)
		{
			bool hasVisibleElements = false;


			int size = collection?.Count ?? 0;
			for (int i = 0; i < size; ++i)
			{
				var element = collection?[i];
				if (element is { })
				{
					if (element is UIElement elementAsUIE)
					{
						var visibility = elementAsUIE.Visibility;
						if (visibility == Visibility.Visible)
						{
							hasVisibleElements = true;
							break;
						}
					}
				}
			}

			return hasVisibleElements;
		}


		public static void FindParentCommandBarForElement(ICommandBarElement element, out CommandBar? parentCmdBar)
		{
			parentCmdBar = null;
			var elementDO = element as DependencyObject;

			CommandBar? parentCommandBar = null;
			var itemsControl = ItemsControl.ItemsControlFromItemContainer(elementDO);
			if (itemsControl is { })
			{
				var templatedParent = itemsControl.TemplatedParent;
				parentCommandBar = templatedParent as CommandBar;
			}

			// If an element is collapsed initially, it isn't placed in the visual tree of its ItemsControl,
			// meaning that its parent will instead be its logical parent.  To account for that circumstance,
			// we'll explicitly walk the tree to find the parent command bar if we haven't found it yet.
			if (parentCommandBar == null)
			{
				var currentElement = elementDO as DependencyObject;
				while (currentElement is { })
				{
					if (currentElement is CommandBar cmdBar)
					{
						parentCommandBar = cmdBar;
					}

					currentElement = currentElement.GetParent() as DependencyObject;
				}
			}

			parentCmdBar = parentCommandBar;
		}

		private void FindMovablePrimaryCommands(double availablePrimaryCommandsWidth, double primaryItemsControlDesiredWidth, out int primaryCommandsCountInTransition)
		{
			bool canProcessDynamicOverflowOrder = false;

			// Find the movable primary commands by looking the DynamicOverflowOrder property.
			FindMovablePrimaryCommandsFromOrderSet(
				availablePrimaryCommandsWidth,
				primaryItemsControlDesiredWidth,
				out primaryCommandsCountInTransition,
				ref canProcessDynamicOverflowOrder);

			// Find the movable primary commands as the default behavior that move the right most command from the near the see more button
			if (!canProcessDynamicOverflowOrder)
			{
				// Get the movable primary commands transition candidates from the primary commands collection to
				// the overflow collection. The dynamic overflow moving order is based on the right most commands
				// in the near the "See More" button.
				int dynamicPrimaryCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

				MUX_ASSERT(dynamicPrimaryCount > 0);

				int moveStartIndex = dynamicPrimaryCount - 1;

				primaryCommandsCountInTransition = 0;

				// Find out the move starting index from the right most primary command element
				for (int i = moveStartIndex; i > 0; i--)
				{
					var element = m_tpDynamicPrimaryCommands?[i];
					Size elementDesiredSize = new Size();

					if (element is UIElement elementAsUiE)
					{
						elementDesiredSize = elementAsUiE.DesiredSize;
					}

					primaryItemsControlDesiredWidth -= elementDesiredSize.Width;

					if (primaryItemsControlDesiredWidth < availablePrimaryCommandsWidth)
					{
						break;
					}

					moveStartIndex--;
				}

				// Insert the movable primary commands candidate into the transition commands collection
				m_tpPrimaryCommandsInTransition?.Clear();
				for (int i = moveStartIndex; i < dynamicPrimaryCount; i++)
				{
					var element = m_tpDynamicPrimaryCommands?[i];
					if (element is { })
					{
						m_tpPrimaryCommandsInTransition?.Insert(primaryCommandsCountInTransition++, element);
					}
				}
			}
		}

		private void FindMovablePrimaryCommandsFromOrderSet(
			double availablePrimaryCommandsWidth,
			double primaryItemsControlDesiredWidth,
			out int primaryCommandsCountInTransition,
			ref bool canProcessDynamicOverflowOrder)
		{
			bool shouldFindMovableOrderSet = false;
			int dynamicPrimaryCount = 0;
			int dynamicOverflowOrder = 0;
			int firstMovableOrder = 0;
			int previousFirstMovableOrder = 0;
			primaryCommandsCountInTransition = 0;

			canProcessDynamicOverflowOrder = false;

			// Find the movable primary command by looking DynamicOverflowOrder property.
			//
			// If the DynamicOverflowOrder property is set,
			//      1. Find the first movable order command that isn't zero value set.
			//      2. Find the all movable primary commands that has the same order value.
			//      3. Find the movable separators if it needs to move with moving primary command.
			//      4. Keep look for the next movable order set in primary commands
			//      5. Look the default movable commands if it still need to move more primary commands to overflow.
			do
			{
				bool isSetMovableCandidateOrder = false;

				shouldFindMovableOrderSet = false;

				dynamicPrimaryCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

				// Find the first movable order set on the dynamic primary commands
				for (int i = 0; i < dynamicPrimaryCount; i++)
				{
					var element = m_tpDynamicPrimaryCommands?[i];

					if (element is ICommandBarElement2 element2)
					{
						dynamicOverflowOrder = element2.DynamicOverflowOrder;
						if (dynamicOverflowOrder > 0 && dynamicOverflowOrder > previousFirstMovableOrder)
						{
							if (!isSetMovableCandidateOrder)
							{
								firstMovableOrder = dynamicOverflowOrder;
								isSetMovableCandidateOrder = true;
							}
							else
							{
								firstMovableOrder = Math.Min(dynamicOverflowOrder, firstMovableOrder);
							}
							shouldFindMovableOrderSet = true;
						}
					}
				}

				// Find the first movable commands that has the same order value
				if (shouldFindMovableOrderSet && firstMovableOrder > previousFirstMovableOrder)
				{
					if (!canProcessDynamicOverflowOrder)
					{
						canProcessDynamicOverflowOrder = true;
						m_tpPrimaryCommandsInTransition?.Clear();
					}

					for (int i = 0; i < dynamicPrimaryCount; i++)
					{
						var element = m_tpDynamicPrimaryCommands?[i];

						if (element is ICommandBarElement2 element2)
						{
							dynamicOverflowOrder = element2.DynamicOverflowOrder;

							if (dynamicOverflowOrder > 0 && dynamicOverflowOrder == firstMovableOrder)
							{
								InsertPrimaryCommandToPrimaryCommandsInTransition(i, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);

								// Find the movable separators in backward direction by moving the primary command together
								if (i > 0)
								{
									FindMovableSeparatorsInBackwardDirection(i, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
								}

								// Find the movable separators in forward direction by moving the primary command together
								if (i < dynamicPrimaryCount - 1)
								{
									FindMovableSeparatorsInForwardDirection(i, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
								}

								if (primaryItemsControlDesiredWidth < availablePrimaryCommandsWidth)
								{
									shouldFindMovableOrderSet = false;
								}
							}
						}
					}
				}

				previousFirstMovableOrder = firstMovableOrder;

				// Keep looking for the next movable order set primary commands
			} while (shouldFindMovableOrderSet != false);

			if (canProcessDynamicOverflowOrder && primaryItemsControlDesiredWidth > availablePrimaryCommandsWidth)
			{
				// Keep looking for the movable primary commands that doesn't set the order
				for (int i = dynamicPrimaryCount; i > 0; i--)
				{
					var element = m_tpDynamicPrimaryCommands?[i - 1];

					if (element is ICommandBarElement2 element2)
					{
						dynamicOverflowOrder = element2.DynamicOverflowOrder;

						if (dynamicOverflowOrder == 0)
						{
							InsertPrimaryCommandToPrimaryCommandsInTransition(i - 1, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);

							if (primaryItemsControlDesiredWidth < availablePrimaryCommandsWidth)
							{
								break;
							}
						}
					}
				}
			}
		}

		private void InsertPrimaryCommandToPrimaryCommandsInTransition(
			int indexMovingPrimaryCommand,
			ref int primaryCommandsCountInTransition,
			ref double primaryItemsControlDesiredWidth)
		{
			var element = m_tpDynamicPrimaryCommands?[indexMovingPrimaryCommand];

			if (element is { })
			{
				m_tpPrimaryCommandsInTransition?.Insert(primaryCommandsCountInTransition++, element);

				if (element is UIElement elementAsUiE)
				{
					primaryItemsControlDesiredWidth -= elementAsUiE.DesiredSize.Width;
				}
			}
		}

		private void UpdatePrimaryCommandElementMinWidthInOverflow()
		{
			var primaryCommandDesiredSize = new Size();
			var primaryCommandsCountInTransition = 0;

			// Update the primary command min width that will be used for finding the restorable
			// primary commands from the overflow into the primary commands collection whenever
			// the CommandBar has more available width.

			primaryCommandsCountInTransition = m_tpPrimaryCommandsInTransition?.Count ?? 0;

			MUX_ASSERT(primaryCommandsCountInTransition > 0);

			m_restorablePrimaryCommandMinWidth = -1;

			for (int i = 0; i < primaryCommandsCountInTransition; i++)
			{
				var transitionPrimaryElement = m_tpPrimaryCommandsInTransition?[i];

				if (transitionPrimaryElement is { })
				{
					var elementAsSeparator = transitionPrimaryElement as AppBarSeparator;
					if (elementAsSeparator == null)
					{
						var elementAsUiE = transitionPrimaryElement as UIElement;
						primaryCommandDesiredSize = elementAsUiE?.DesiredSize ?? new Size();

						if (m_restorablePrimaryCommandMinWidth == -1)
						{
							m_restorablePrimaryCommandMinWidth = primaryCommandDesiredSize.Width;
						}
						else
						{
							m_restorablePrimaryCommandMinWidth = Math.Min(m_restorablePrimaryCommandMinWidth, primaryCommandDesiredSize.Width);
						}
					}
				}
			}
		}

		private void MoveTransitionPrimaryCommandsIntoOverflow(int primaryCommandsCountInTransition)
		{
			bool isFound = false;
			int primaryIndexForTransitionCommand = 0;

			MUX_ASSERT(primaryCommandsCountInTransition > 0);

			if (m_SecondaryCommandStartIndex == 0)
			{
				bool hasVisibleSecondaryElements = false;

				hasVisibleSecondaryElements = HasVisibleElements(m_tpDynamicSecondaryCommands);

				if (hasVisibleSecondaryElements)
				{
					if (m_tpAppBarSeparatorInOverflow is { })
					{
						// Add the AppBarSeparator between the transited primary command and existing secondary command in the overflow
						SetOverflowStyleUsage(m_tpAppBarSeparatorInOverflow, true /*isItemInOverflow*/);
						m_tpDynamicSecondaryCommands?.Insert(m_SecondaryCommandStartIndex, m_tpAppBarSeparatorInOverflow);
						/*SetInputModeOnSecondaryCommand(*/
						m_SecondaryCommandStartIndex++/*, m_inputDeviceTypeUsedToOpen)*/;
						m_hasAppBarSeparatorInOverflow = true;
					}
				}
			}

			// Rearrange the transition primary commands between the coming transition and the existing primary commands in overflow
			for (int i = 0; i < primaryCommandsCountInTransition; i++)
			{
				var transitionPrimaryElement = m_tpPrimaryCommandsInTransition?[0];

				m_tpPrimaryCommandsInTransition?.RemoveAt(0);

				primaryIndexForTransitionCommand = transitionPrimaryElement != null
					? m_tpDynamicPrimaryCommands?.IndexOf(transitionPrimaryElement) ?? -1
					: -1;

				isFound = primaryIndexForTransitionCommand != -1;

				if (isFound)
				{
					m_tpDynamicPrimaryCommands?.RemoveAt(primaryIndexForTransitionCommand);
					InsertTransitionPrimaryCommandIntoOverflow(transitionPrimaryElement);
				}
			}
		}

		private void InsertTransitionPrimaryCommandIntoOverflow(ICommandBarElement? transitionPrimaryElement)
		{
			int primaryIndexForTransitionCommand = 0;
			int primaryIndexForExistPrimaryCommand = 0;
			int indexForMovedPrimaryCommand = 0;
			bool isFound = false;
			bool isInserted = false;

			MUX_ASSERT(m_isDynamicOverflowEnabled);

			if (transitionPrimaryElement == null)
			{
				return;
			}

			if (m_hasAppBarSeparatorInOverflow)
			{
				indexForMovedPrimaryCommand = m_SecondaryCommandStartIndex - 1;
			}
			else
			{
				indexForMovedPrimaryCommand = m_SecondaryCommandStartIndex;
			}

			// Insert the transition primary command into the overflow collection in order of the original primary index
			primaryIndexForTransitionCommand = m_tpPrimaryCommands?.IndexOf(transitionPrimaryElement) ?? -1;

			isFound = primaryIndexForTransitionCommand != -1;
			MUX_ASSERT(isFound);

			// Note that the UseOverflowStyle property is set to True before the ICommandBarOverflowElement is inserted into
			// the SecondaryCommands ItemsControl. This is to guarantee that ItemsControl::PrepareItemContainer gets called after
			// the style was reset in AppBarButton::UpdateInternalStyles. Otherwise the ItemsControl::ApplyItemContainerStyle call
			// is ineffective.

			for (int i = 0; i < indexForMovedPrimaryCommand; i++)
			{
				var existPrimaryElement = m_tpDynamicSecondaryCommands?[i];
				if (existPrimaryElement is { })
				{
					primaryIndexForExistPrimaryCommand = m_tpPrimaryCommands?.IndexOf(existPrimaryElement) ?? -1;
					isFound = primaryIndexForExistPrimaryCommand != -1;
				}
				MUX_ASSERT(isFound);

				if (primaryIndexForTransitionCommand < primaryIndexForExistPrimaryCommand)
				{
					SetOverflowStyleUsage(transitionPrimaryElement, true /*isItemInOverflow*/);
					m_tpDynamicSecondaryCommands?.Insert(i, transitionPrimaryElement);
					//SetInputModeOnSecondaryCommand(i, m_inputDeviceTypeUsedToOpen));

					m_SecondaryCommandStartIndex++;
					isInserted = true;
					break;
				}
			}

			if (!isInserted)
			{
				SetOverflowStyleUsage(transitionPrimaryElement, true /*isItemInOverflow*/);
				m_tpDynamicSecondaryCommands?.Insert(indexForMovedPrimaryCommand, transitionPrimaryElement);
				//	IFC_RETURN(SetInputModeOnSecondaryCommand(indexForMovedPrimaryCommand, m_inputDeviceTypeUsedToOpen));

				m_SecondaryCommandStartIndex++;
			}
		}

		private void ResetDynamicCommands()
		{
			int primaryItemsCount = 0;
			int secondaryItemsCount = 0;

			StoreFocusedCommandBarElement();

			primaryItemsCount = m_tpPrimaryCommands?.Count ?? 0;
			secondaryItemsCount = m_tpSecondaryCommands?.Count ?? 0;

			// Reset any primary commands currently in the overflow back to the non-overflow style.
			for (int i = 0; i < m_SecondaryCommandStartIndex; ++i)
			{
				SetOverflowStyleAndInputModeOnSecondaryCommand(i, false);
			}

			// Remove the dynamic primary commands from the overflow collection and insert back to
			// the dynamic primary commands collection.
			for (int i = 0; i < m_SecondaryCommandStartIndex; ++i)
			{
				var primaryElement = m_tpDynamicSecondaryCommands?[0];

				if (primaryElement is { })
				{
					// Remove the moved primary command into the overflow immediately and
					// insert back to the primary commands.
					m_tpDynamicSecondaryCommands?.RemoveAt(0);
					m_tpDynamicPrimaryCommands?.Insert(0, primaryElement);
				}
			}

			// Reset the secondary command start index
			m_SecondaryCommandStartIndex = 0;

			m_hasAppBarSeparatorInOverflow = false;

			// Populate the dynamic primary collection with our primary items by default.
			m_tpDynamicPrimaryCommands?.Clear();
			for (int i = 0; i < primaryItemsCount; ++i)
			{
				var primaryElement = m_tpPrimaryCommands?[i];
				if (primaryElement is { })
				{
					m_tpDynamicPrimaryCommands?.Insert(i, primaryElement);
				}
			}

			// Populate the dynamic secondary collection with our secondary items by default.
			m_tpDynamicSecondaryCommands?.Clear();
			for (int i = 0; i < secondaryItemsCount; ++i)
			{
				var secondaryElement = m_tpSecondaryCommands?[i];

				if (secondaryElement is { })
				{
					SetOverflowStyleUsage(secondaryElement, true /*isItemInOverflow*/);
					m_tpDynamicSecondaryCommands?.Insert(i, secondaryElement);
					//SetInputModeOnSecondaryCommand(i, m_inputDeviceTypeUsedToOpen);
				}
			}

			UpdateTemplateSettings();
		}

		private void SaveMovedPrimaryCommandsIntoPreviousTransitionCollection()
		{
			int dynamicSecondaryItemsCount = 0;
			int secondaryItemsCount = 0;
			int primaryCountInOverflow = 0;

			dynamicSecondaryItemsCount = m_tpDynamicSecondaryCommands?.Count ?? 0;
			secondaryItemsCount = m_tpSecondaryCommands?.Count ?? 0;

			MUX_ASSERT(dynamicSecondaryItemsCount >= secondaryItemsCount);
			primaryCountInOverflow = dynamicSecondaryItemsCount - secondaryItemsCount;

			m_tpPrimaryCommandsInPreviousTransition?.Clear();


			for (int i = 0; i < primaryCountInOverflow; i++)
			{
				var primaryElement = m_tpDynamicSecondaryCommands?[i];
				if (primaryElement is { })
				{
					m_tpPrimaryCommandsInPreviousTransition?.Insert(i, primaryElement);
				}
			}

		}

		private void FireDynamicOverflowItemsChangingEvent(bool isForceToRestore)
		{
			var args = new DynamicOverflowItemsChangingEventArgs();

			bool isAdding = false;
			int previousTransitionCount = 0;
			int currentTransitionCount = 0;
			int samePrimaryCount = 0;

			if (!isForceToRestore)
			{
				previousTransitionCount = m_tpPrimaryCommandsInPreviousTransition?.Count ?? 0;
				currentTransitionCount = m_tpPrimaryCommandsInTransition?.Count ?? 0;

				for (int i = 0; i < previousTransitionCount; i++)
				{
					var primaryElementInPreviousTransition = m_tpPrimaryCommandsInPreviousTransition?[i];

					for (int j = 0; j < currentTransitionCount; j++)
					{
						var primaryElementInTransition = m_tpPrimaryCommandsInTransition?[j];

						if (primaryElementInTransition is { } && primaryElementInTransition == primaryElementInPreviousTransition)
						{
							samePrimaryCount++;
						}
					}
				}

				isAdding = (currentTransitionCount - samePrimaryCount) > 0 ? true : false;
			}

			args.Action = isAdding ?
				CommandBarDynamicOverflowAction.AddingToOverflow :
				CommandBarDynamicOverflowAction.RemovingFromOverflow;


			DynamicOverflowItemsChanging?.Invoke(this, args);
			// Fire the dynamic overflow items changing event
		}

		internal static bool IsCommandBarElementInOverflow(ICommandBarElement element)
		{
			FindParentCommandBarForElement(element, out var parentCmdBar);
			bool isInOverflow = false;

			if (parentCmdBar is { })
			{
				isInOverflow = parentCmdBar.IsElementInOverflow(element);
			}

			return isInOverflow;
		}

		private bool IsElementInOverflow(ICommandBarElement element)
		{
			int itemsCount = 0;
			bool isInOverflow = false;

			itemsCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

			for (int i = 0; i < itemsCount; ++i)
			{
				var elementInOverflow = m_tpDynamicSecondaryCommands?[i];

				if (elementInOverflow is { } && elementInOverflow == element)
				{
					isInOverflow = true;
				}
			}

			return isInOverflow;
		}

		internal static int GetPositionInSetStatic(ICommandBarElement element)
		{
			int positionInSet = -1;

			FindParentCommandBarForElement(element, out var parentCommandBar);

			if (parentCommandBar is { })
			{
				positionInSet = parentCommandBar.GetPositionInSet(element);
			}

			return positionInSet;
		}

		private int GetPositionInSet(ICommandBarElement element)
		{
			int positionInSet = -1;

			// The UIA position in set for a CommandBar element depends on two things:
			// which set the element belongs to (primary or secondary), and how many
			// interactable elements there are in that set.  We'll ignore separators
			// and collapsed elements for the purposes of this count since those are not UIA stops.
			// To accomplish this, we'll go through first the primary and then secondary
			// commands, and if we find the element we're looking for,
			// we'll return the number of interactable elements we've found
			// prior to and including it, which is its UIA position in set.
			var itemsCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

			int interactableElementCount = 0;

			for (int i = 0; i < itemsCount; ++i)
			{
				var currentElement = m_tpDynamicPrimaryCommands?[i];

				var itemAsUIE = currentElement as UIElement;
				var visibility = Visibility.Collapsed;

				if (itemAsUIE is { })
				{
					visibility = itemAsUIE.Visibility;
				}

				if (visibility != Visibility.Visible)
				{
					continue;
				}

				var itemAsButton = currentElement as AppBarButton;
				var itemAsToggleButton = currentElement as AppBarToggleButton;

				if (itemAsButton is { } || itemAsToggleButton is { })
				{
					interactableElementCount++;
				}

				if (currentElement == element)
				{
					positionInSet = interactableElementCount;
				}
			}

			itemsCount = 0;
			itemsCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

			interactableElementCount = 0;

			for (int i = 0; i < itemsCount; ++i)
			{
				var currentElement = m_tpDynamicSecondaryCommands?[i];

				var itemAsUIE = currentElement as UIElement;
				var visibility = Visibility.Collapsed;

				if (itemAsUIE is { })
				{
					visibility = itemAsUIE.Visibility;
				}

				if (visibility != Visibility.Visible)
				{
					continue;
				}

				var itemAsButton = currentElement as AppBarButton;
				var itemAsToggleButton = currentElement as AppBarToggleButton;

				if (itemAsButton is { } || itemAsToggleButton is { })
				{
					interactableElementCount++;
				}

				if (currentElement == element)
				{
					positionInSet = interactableElementCount;
				}
			}

			return positionInSet;
		}

		internal static int GetSizeOfSetStatic(ICommandBarElement element)
		{
			int sizeOfSet = -1;

			FindParentCommandBarForElement(element, out var parentCommandBar);

			if (parentCommandBar is { })
			{
				sizeOfSet = parentCommandBar.GetSizeOfSet(element);
			}

			return sizeOfSet;
		}

		private int GetSizeOfSet(ICommandBarElement element)
		{
			int sizeOfSet = -1;

			// The UIA size in set for a CommandBar element depends on two things:
			// which set the element belongs to (primary or secondary), and how many
			// interactable elements there are in that set.  We'll ignore separators
			// for the purposes of this count since those are not UIA stops.
			// To accomplish this, we'll go through first the primary and then secondary
			// commands, and if we find the element we're looking for,
			// we'll return the number of interactable elements that are in its set.
			var itemsCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

			bool itemFound = false;
			int interactableElementCount = 0;

			for (int i = 0; i < itemsCount; ++i)
			{
				var currentElement = m_tpDynamicPrimaryCommands?[i];

				var itemAsUIE = currentElement as UIElement;
				var visibility = Visibility.Collapsed;

				if (itemAsUIE is { })
				{
					visibility = itemAsUIE.Visibility;
				}

				if (visibility != Visibility.Visible)
				{
					continue;
				}

				var itemAsButton = currentElement as AppBarButton;
				var itemAsToggleButton = currentElement as AppBarToggleButton;

				if (itemAsButton is { } || itemAsToggleButton is { })
				{
					interactableElementCount++;
				}

				if (currentElement == element)
				{
					itemFound = true;
				}
			}

			if (itemFound)
			{
				sizeOfSet = interactableElementCount;
			}

			itemsCount = 0;
			itemsCount = m_tpDynamicSecondaryCommands?.Count ?? 0;

			interactableElementCount = 0;

			for (int i = 0; i < itemsCount; ++i)
			{
				var currentElement = m_tpDynamicSecondaryCommands?[i];

				var itemAsUIE = currentElement as UIElement;
				var visibility = Visibility.Collapsed;

				if (itemAsUIE is { })
				{
					visibility = itemAsUIE.Visibility;
				}

				if (visibility != Visibility.Visible)
				{
					continue;
				}

				var itemAsButton = currentElement as AppBarButton;
				var itemAsToggleButton = currentElement as AppBarToggleButton;

				if (itemAsButton is { } || itemAsToggleButton is { })
				{
					interactableElementCount++;
				}

				if (currentElement == element)
				{
					itemFound = true;
				}
			}

			if (itemFound)
			{
				sizeOfSet = interactableElementCount;
			}

			return sizeOfSet;
		}

		private void TrimPrimaryCommandSeparatorInOverflow(ref int primaryCommandsCountInTransition)
		{
			// Remove the primary AppBarSeparators that doesn't allow to move into overflow collection

			MUX_ASSERT(primaryCommandsCountInTransition > 0);

			for (int i = primaryCommandsCountInTransition; i > 0; i--)
			{
				var transitionPrimaryElement = m_tpPrimaryCommandsInTransition?[i - 1];

				var elementAsSeparator = transitionPrimaryElement as AppBarSeparator;
				if (elementAsSeparator is { })
				{
					int primaryIndexForTransitionCommand;
					bool isFound = false;

					primaryIndexForTransitionCommand = m_tpDynamicPrimaryCommands?.IndexOf(elementAsSeparator) ?? -1;
					isFound = primaryIndexForTransitionCommand != -1;

					if (isFound)
					{
						m_tpDynamicPrimaryCommands?.RemoveAt(primaryIndexForTransitionCommand);
					}

					m_tpPrimaryCommandsInTransition?.RemoveAt(i - 1);
					primaryCommandsCountInTransition--;
				}
			}
		}

		private bool IsAppBarSeparatorInDynamicPrimaryCommands(int index)
		{
			bool isAppBarSeparator = false;
			var primaryElement = m_tpDynamicPrimaryCommands?[index];

			if (primaryElement is { })
			{
				if (primaryElement is AppBarSeparator)
				{
					isAppBarSeparator = true;
				}
			}

			return isAppBarSeparator;
		}

		private void FindMovableSeparatorsInBackwardDirection(int movingPrimaryCommandIndex, ref int primaryCommandsCountInTransition, ref double primaryItemsControlDesiredWidth)
		{
			if (movingPrimaryCommandIndex > 0)
			{
				bool isAppBarSeparator = false;

				// Find the separators in backward direction that need to be away with moving the primary command element

				isAppBarSeparator = IsAppBarSeparatorInDynamicPrimaryCommands(movingPrimaryCommandIndex - 1);

				if (isAppBarSeparator)
				{
					bool hasNonSeparator = false;
					int indexNonSeparator = 0;
					int indexMovingBackward = movingPrimaryCommandIndex - 1;

					FindNonSeparatorInDynamicPrimaryCommands(
						false /* isForward */,
						indexMovingBackward,
						out hasNonSeparator,
						out indexNonSeparator);

					if (!hasNonSeparator)
					{
						while (indexMovingBackward >= 0)
						{
							InsertSeparatorToPrimaryCommandsInTransition(indexMovingBackward, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
							indexMovingBackward--;
						}
					}
					else
					{
						while (indexMovingBackward > indexNonSeparator && (indexMovingBackward - indexNonSeparator) > 1)
						{
							InsertSeparatorToPrimaryCommandsInTransition(indexMovingBackward, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
							indexMovingBackward--;
						}
					}
				}
			}
		}

		private void FindMovableSeparatorsInForwardDirection(
			int movingPrimaryCommandIndex,
			ref int primaryCommandsCountInTransition,
			ref double primaryItemsControlDesiredWidth)
		{
			bool isAppBarSeparator = false;

			// Find the separators in forward direction that need to be away with moving the primary command element

			isAppBarSeparator = IsAppBarSeparatorInDynamicPrimaryCommands(movingPrimaryCommandIndex + 1);

			if (isAppBarSeparator)
			{
				bool hasNonSeparator = false;
				int dynamicPrimaryCount = 0;
				int indexNonSeparator = 0;
				int indexMovingForward = movingPrimaryCommandIndex + 1;

				FindNonSeparatorInDynamicPrimaryCommands(
					true /* isForward */,
					indexMovingForward,
					out hasNonSeparator,
					out indexNonSeparator);

				dynamicPrimaryCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

				if (!hasNonSeparator)
				{
					while (indexMovingForward < dynamicPrimaryCount)
					{
						InsertSeparatorToPrimaryCommandsInTransition(indexMovingForward, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
						indexMovingForward++;
					}
				}
				else
				{
					while (indexMovingForward < indexNonSeparator && (indexNonSeparator - indexMovingForward) > 1)
					{
						InsertSeparatorToPrimaryCommandsInTransition(indexMovingForward, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
						indexMovingForward++;
					}
				}

				// Move the separator at the next index of moving primary command at 0 index
				if (movingPrimaryCommandIndex == 0)
				{
					InsertSeparatorToPrimaryCommandsInTransition(movingPrimaryCommandIndex + 1, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
				}

				// Move the separator at the next index with moving primary command
				if (movingPrimaryCommandIndex > 0)
				{
					isAppBarSeparator = IsAppBarSeparatorInDynamicPrimaryCommands(movingPrimaryCommandIndex - 1);
					if (isAppBarSeparator)
					{
						InsertSeparatorToPrimaryCommandsInTransition(movingPrimaryCommandIndex + 1, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);

						if (!hasNonSeparator)
						{
							InsertSeparatorToPrimaryCommandsInTransition(movingPrimaryCommandIndex - 1, ref primaryCommandsCountInTransition, ref primaryItemsControlDesiredWidth);
						}
					}
				}
			}
		}

		private void FindNonSeparatorInDynamicPrimaryCommands(bool isForward, int indexMoving, out bool hasNonSeparator, out int indexNonSeparator)
		{
			int dynamicPrimaryCount = 0;

			hasNonSeparator = false;
			indexNonSeparator = 0;

			dynamicPrimaryCount = m_tpDynamicPrimaryCommands?.Count ?? 0;

			// Find the non-separator command element index in proper direction
			while (isForward ? (indexMoving < dynamicPrimaryCount) : (indexMoving >= 0))
			{
				var primaryElement = m_tpDynamicPrimaryCommands?[indexMoving];

				var elementAsSeparator = primaryElement as AppBarSeparator;

				if (elementAsSeparator == null)
				{
					hasNonSeparator = true;
					indexNonSeparator = indexMoving;
					break;
				}

				if (isForward)
				{
					indexMoving++;
				}
				else
				{
					indexMoving--;
				}
			}
		}

		private void InsertSeparatorToPrimaryCommandsInTransition(int indexMovingSeparator, ref int primaryCommandsCountInTransition, ref double primaryItemsControlDesiredWidth)
		{
			var element = m_tpDynamicPrimaryCommands?[indexMovingSeparator];

			var elementAsSeparator = element as AppBarSeparator;
			if (elementAsSeparator is { })
			{
				bool isFound = false;
				int separatorIndexInTransition = 0;

				separatorIndexInTransition = m_tpPrimaryCommandsInTransition?.IndexOf(elementAsSeparator) ?? -1;
				isFound = separatorIndexInTransition != -1;

				if (!isFound)
				{
					UIElement? elementAsUiE = null;

					m_tpPrimaryCommandsInTransition?.Insert(primaryCommandsCountInTransition++, elementAsSeparator);

					elementAsUiE = element as UIElement;
					var elementDesiredSize = elementAsUiE?.DesiredSize ?? new Size();

					primaryItemsControlDesiredWidth -= elementDesiredSize.Width;
				}
			}
		}

		private int GetRestorablePrimaryCommandsMinimumCount()
		{
			int dynamicOverflowOrder = 0;
			int firstRestorableOrder = 0;

			int restorableMinCount = 0;

			if (m_SecondaryCommandStartIndex > 1)
			{
				for (int i = 0; i < m_SecondaryCommandStartIndex - 1; ++i)
				{
					var primaryElement = m_tpDynamicSecondaryCommands?[i];

					if (primaryElement is ICommandBarElement2 primaryElement2)
					{
						dynamicOverflowOrder = primaryElement2.DynamicOverflowOrder;
						if (dynamicOverflowOrder > 0)
						{
							firstRestorableOrder = Math.Max(dynamicOverflowOrder, firstRestorableOrder);
						}
					}
				}

				if (firstRestorableOrder > 0)
				{
					for (int i = 0; i < m_SecondaryCommandStartIndex - 1; ++i)
					{
						var primaryElement = m_tpDynamicSecondaryCommands?[i];

						if (primaryElement is ICommandBarElement2 primaryElement2)
						{
							dynamicOverflowOrder = primaryElement2.DynamicOverflowOrder;
							if (dynamicOverflowOrder > 0 && dynamicOverflowOrder == firstRestorableOrder)
							{
								// Retrieve the restorable primary commands that has the same dynamic overflow order
								restorableMinCount++;
							}
						}
					}
				}
				else
				{
					// Restore the primary command one by one from the overflow to the primary commands
					restorableMinCount = 1;
				}
			}

			return restorableMinCount;
		}

		private void StoreFocusedCommandBarElement()
		{
			var focusedElement = this.GetFocusedElement();

			if (focusedElement is { })
			{
				var itemsControl = ItemsControl.ItemsControlFromItemContainer(focusedElement);

				if (itemsControl is { }
					&& (itemsControl == m_tpPrimaryItemsControlPart
						|| itemsControl == (m_tpSecondaryItemsControlPart /*as CommandBarOverflowPresenter */)))
				{
					var item = itemsControl.ItemFromContainer(focusedElement);

					var element = item as ICommandBarElement;
					m_focusedElementPriorToCollectionOrSizeChange = element;
					m_focusStatePriorToCollectionOrSizeChange = GetFocusState(focusedElement);
				}
			}
		}

		private void RestoreCommandBarElementFocus()
		{
			var element = m_focusedElementPriorToCollectionOrSizeChange;

			if (element is { })
			{
				ItemsControl? itemsControl = null;
				int elementIndex;
				bool found = false;

				elementIndex = m_tpDynamicPrimaryCommands?.IndexOf(element) ?? -1;
				found = elementIndex != -1;

				if (found)
				{
					itemsControl = m_tpPrimaryItemsControlPart;
				}
				else
				{
					elementIndex = m_tpDynamicSecondaryCommands?.IndexOf(element) ?? -1;
					found = elementIndex != -1;
					if (found)
					{
						itemsControl = m_tpSecondaryItemsControlPart;
					}
				}

				if (itemsControl is { })
				{
					var container = itemsControl.ContainerFromItem(element);

					if (container is { })
					{
						var containerAsUIE = container as UIElement;
						containerAsUIE?.Focus(m_focusStatePriorToCollectionOrSizeChange);
					}
				}

				ResetCommandBarElementFocus();
			}
		}

		private void OnAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (m_tpOverflowPopup is { })
			{
				m_tpOverflowPopup.Opened += OnOverflowPopupOpened;
				m_overflowPopupOpenedEventHandler.Disposable = Disposable.Create(() => m_tpOverflowPopup.Opened -= OnOverflowPopupOpened);
			}
		}

		private void OnOverflowPopupOpened(object? sender, object e)
		{
			try
			{
				if (m_tpOverflowPopup is { })
				{
					if (m_tpSecondaryItemsControlPart is { })
					{
						var items = m_tpSecondaryItemsControlPart.Items;
						var firstItemIterator = items.GetEnumerator();

						if (firstItemIterator is { })
						{
							bool succeeded = false;
							bool hasCurrent = false;

							hasCurrent = firstItemIterator.Current is { };

							while (hasCurrent && !succeeded)
							{
								var firstItem = firstItemIterator.Current;
								var itemAsControl = firstItem as Control;
								if (itemAsControl is { })
								{
									succeeded = itemAsControl.Focus(FocusState.Keyboard);
									if (succeeded)
									{
										//auto contentRoot = VisualTree::GetContentRootForElement(GetHandle());
										//IFC_RETURN(contentRoot->GetAKExport().UpdateScope());
									}
								}

								hasCurrent = firstItemIterator.MoveNext();
							}
						}
					}
				}
			}
			finally
			{
				m_overflowPopupOpenedEventHandler.Disposable = null;
			}
		}

		private void ResetCommandBarElementFocus()
		{
			m_focusedElementPriorToCollectionOrSizeChange = null;
			m_focusStatePriorToCollectionOrSizeChange = FocusState.Unfocused;
		}

		private FocusState GetFocusState(DependencyObject focusedElement)
		{
			var focusState = FocusState.Programmatic;

			var focusedControl = focusedElement as Control;
			if (focusedControl is { })
			{
				focusState = focusedControl.FocusState;
			}

			// Although it shouldn't be possible, we are seeing scenarios where the FocusState of the focused element is
			// Unfocused. Workaround this issue by using the real FocusState off the FocusManager.
			if (focusedControl == null || focusState == FocusState.Unfocused)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				if (focusManager is { })
				{
					focusState = focusManager.GetRealFocusStateForFocusedElement();
				}
			}

			return focusState;
		}

		internal void CloseSubMenus(ISubMenuOwner? pMenuToLeaveOpen, bool closeOnDelay)
		{
			var primaryCommands = PrimaryCommands;
			var secondaryCommands = SecondaryCommands;

			int primaryCommandCount = 0;
			int secondaryCommandCount = 0;

			primaryCommandCount = primaryCommands.Count;
			secondaryCommandCount = secondaryCommands.Count;

			for (int i = 0; i < primaryCommandCount; i++)
			{
				ICommandBarElement? element;
				ISubMenuOwner? elementAsSubMenuOwner;

				element = primaryCommands[i];
				elementAsSubMenuOwner = element as ISubMenuOwner;

				if (elementAsSubMenuOwner is { } && (pMenuToLeaveOpen == null || (pMenuToLeaveOpen != elementAsSubMenuOwner)))
				{
					if (closeOnDelay)
					{
						elementAsSubMenuOwner.DelayCloseSubMenu();
					}
					else
					{
						elementAsSubMenuOwner.CloseSubMenuTree();
					}
				}
			}


			for (int i = 0; i < secondaryCommandCount; i++)
			{
				ICommandBarElement? element;
				ISubMenuOwner? elementAsSubMenuOwner;

				element = secondaryCommands[i];
				elementAsSubMenuOwner = element as ISubMenuOwner;

				if (elementAsSubMenuOwner is { } && (pMenuToLeaveOpen == null || (pMenuToLeaveOpen != elementAsSubMenuOwner)))
				{
					if (closeOnDelay)
					{
						elementAsSubMenuOwner.DelayCloseSubMenu();
					}
					else
					{
						elementAsSubMenuOwner.CloseSubMenuTree();
					}
				}
			}
		}

		private void SetCompactMode(bool isCompact)
		{
			if (m_tpDynamicPrimaryCommands == null
				|| m_tpDynamicSecondaryCommands == null)
			{
				return;
			}

			var primaryItemsCount = m_tpDynamicPrimaryCommands.Count;

			for (int i = 0; i < primaryItemsCount; ++i)
			{
				var element = m_tpDynamicPrimaryCommands[i];
				element.IsCompact = isCompact;
			}
		}

		internal void NotifyElementVectorChanging(CommandBarElementCollection pElementCollection, CollectionChange change, int changeIndex)
		{
			// Assume that we get this notification only for secondary commands collection.
			//MUX_ASSERT(pElementCollection == m_tpSecondaryCommands);

			SetOverflowStyleParams();

			if (change == CollectionChange.ItemRemoved
				|| change == CollectionChange.ItemChanged)
			{
				SetOverflowStyleAndInputModeOnSecondaryCommand(changeIndex, false);
			}
			else if (change == CollectionChange.Reset)
			{
				int itemCount = 0;
				itemCount = m_tpSecondaryCommands?.Count ?? 0;
				for (int i = 0; i < itemCount; ++i)
				{
					SetOverflowStyleAndInputModeOnSecondaryCommand(i, false);
				}
			}
		}

		void IMenu.Close() => IsOpen = false;
	}
}
