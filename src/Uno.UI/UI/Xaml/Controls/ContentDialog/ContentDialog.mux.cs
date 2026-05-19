// MUX Reference ContentDialog_Partial.cpp, tag winui3/release/1.6-stable

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Helpers;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentDialog
{
#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
	//
	// Original destructor logic:
	// VERIFYHR(DetachEventHandlers());
	// if (auto popup = m_tpPopup.GetSafeReference())
	// {
	//     if (m_popupOpenedHandler)
	//     {
	//         VERIFYHR(m_popupOpenedHandler.DetachEventHandler(popup.Get()));
	//     }
	// }
#endif

	public ContentDialog()
	{
		DefaultStyleKey = typeof(ContentDialog);

#if HAS_UNO
		// Uno specific: Create popup eagerly since ContentDialogPopupPanel needs it in constructor.
		m_tpPopup = new Popup()
		{
			LightDismissOverlayMode = LightDismissOverlayMode.On,
		};

		ResourceResolver.ApplyResource(m_tpPopup, Popup.LightDismissOverlayBackgroundProperty, "ContentDialogLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: true);

		m_tpPopup.PopupPanel = new ContentDialogPopupPanel(this);

		m_tpPopup.Opened += OnPopupOpenedUno;
		m_tpPopup.Closed += OnPopupClosedUno;

		Loaded += (s, e) => OnLoadedUno();
		Unloaded += (s, e) => OnUnloadedUno();
#endif
	}

#if HAS_UNO
	// Uno specific: Provide backward-compatible access to the popup for ContentDialogPopupPanel and MessageDialogContentDialog.
	internal Popup _popup => m_tpPopup;

	private void OnPopupOpenedUno(object sender, object e)
	{
		SetInitialFocusElement();
		Opened?.Invoke(this, new ContentDialogOpenedEventArgs());

		m_skipClosingEventOnHide = false;

		UpdateVisualState();
	}

	private void OnPopupClosedUno(object sender, object e)
	{
		// If the popup was closed externally (not via Hide), handle it.
		if (m_isShowing && !m_hideInProgress)
		{
			m_skipClosingEventOnHide = true;
			HideInternal(ContentDialogResult.None);
		}
	}

	private void OnLoadedUno()
	{
		var d = new CompositeDisposable();

		var xamlRoot = XamlRoot;
		if (xamlRoot != null)
		{
			xamlRoot.Changed += OnXamlRootChangedUno;
			d.Add(() => xamlRoot.Changed -= OnXamlRootChangedUno);
		}

		// Back button integration
		if (SystemNavigationManager.GetForCurrentView() is { } navManager)
		{
			navManager.BackRequested += OnBackRequested;
			d.Add(() => navManager.BackRequested -= OnBackRequested);
		}

		if (m_tpPopup?.PopupPanel is { } popupPanel)
		{
			d.Add(popupPanel.RegisterDisposablePropertyChangedCallback(VisibilityProperty, OnPopupPanelVisibilityChanged));
		}

		m_loadedSubscriptions.Disposable = d;
	}

	private void OnUnloadedUno()
	{
		m_loadedSubscriptions.Disposable = null;
	}

	private readonly SerialDisposable m_loadedSubscriptions = new SerialDisposable();

	private void OnXamlRootChangedUno(object sender, XamlRootChangedEventArgs e)
	{
		if (!m_isTemplateApplied)
		{
			return;
		}

		UpdateVisualState();

		if (m_placementMode != PlacementMode.InPlace)
		{
			SizeAndPositionContentInPopup();
		}
	}

	private void OnPopupPanelVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (m_tpPopup?.PopupPanel?.Visibility == Visibility.Visible)
		{
			SetInitialFocusElement();
		}
	}

	private void OnBackRequested(object sender, BackRequestedEventArgs e)
	{
		ExecuteCloseAction();
		e.Handled = true;
	}
#endif

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// We only react to property changes if ContentDialog is currently visible
		if (m_tpCurrentAsyncOperation == null ||
			!m_hasPreparedContent ||
			m_hideInProgress)
		{
			return;
		}

		var property = args.Property;

		if (property == FullSizeDesiredProperty ||
			property == PrimaryButtonTextProperty ||
			property == SecondaryButtonTextProperty ||
			property == CloseButtonTextProperty ||
			property == DefaultButtonProperty)
		{
			UpdateVisualState();
		}
		else if (property == IsPrimaryButtonEnabledProperty ||
			property == IsSecondaryButtonEnabledProperty)
		{
			// No action needed
		}
		else if (property == PrimaryButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Primary, oldCommand);
		}
		else if (property == SecondaryButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Secondary, oldCommand);
		}
		else if (property == CloseButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Close, oldCommand);
		}
		else if (property == TitleProperty ||
			property == TitleTemplateProperty)
		{
			UpdateTitleSpaceVisibility();
			SetPopupAutomationProperties();
		}
		else if (property == FlowDirectionProperty)
		{
			if (m_placementMode != PlacementMode.InPlace)
			{
				SizeAndPositionContentInPopup();
			}
		}
	}

	protected override void OnApplyTemplate()
	{
		if (m_isSmokeLayerATemplatePart)
		{
			m_isSmokeLayerATemplatePart = false;
			m_tpSmokeLayer = null;
		}

		DetachEventHandlers();
		m_tpDialogShowingStates = null;

		base.OnApplyTemplate();

		m_isLayoutRootTransplanted = false;
		m_isTemplateApplied = true;

		// Acquire template parts
		m_tpBackgroundElementPart = GetTemplateChild("BackgroundElement") as Border;
		m_tpButton1HostPart = GetTemplateChild("Button1Host") as Border;
		m_tpButton2HostPart = GetTemplateChild("Button2Host") as Border;
		m_tpCloseButtonPart = GetTemplateChild("CloseButton") as ButtonBase;
		m_tpCommandSpacePart = GetTemplateChild("CommandSpace") as Grid;
		m_tpContainerPart = GetTemplateChild("Container") as Border;
		m_tpContentPanelPart = GetTemplateChild("ContentPanel") as Grid;
		m_tpContentPart = GetTemplateChild("Content") as ContentPresenter;
		m_tpContentScrollViewerPart = GetTemplateChild("ContentScrollViewer") as ScrollViewer;
		m_tpDialogSpacePart = GetTemplateChild("DialogSpace") as Grid;
		m_tpLayoutRootPart = GetTemplateChild("LayoutRoot") as Grid;
		m_tpPrimaryButtonPart = GetTemplateChild("PrimaryButton") as ButtonBase;
		m_tpScaleTransformPart = GetTemplateChild("ScaleTransform") as ScaleTransform;
		m_tpSecondaryButtonPart = GetTemplateChild("SecondaryButton") as ButtonBase;
		m_tpTitlePart = GetTemplateChild("Title") as ContentControl;

#if HAS_UNO
		m_dialogMinHeight = ResourceResolver.ResolveTopLevelResourceDouble("ContentDialogMinHeight");
#endif

		// For dialogs that were shown when not in the visual tree, since we couldn't prepare
		// their content during the ShowAsync() call, do it now that it's loaded.
		if (m_placementMode == PlacementMode.EntireControlInPopup)
		{
			PrepareContent();
		}

		// On non-PhoneBlue templates, it is possible to resize the app, in which case we would like to reposition the
		// ContentDialog. FullSize behavior also needs to be checked as the window height might become smaller than
		// the MaxHeight, in which case positioning behavior changes.
		var xamlRoot = XamlRoot;
		if (xamlRoot != null)
		{
			var weakThis = new WeakReference<ContentDialog>(this);
			void handler(XamlRoot sender, XamlRootChangedEventArgs args)
			{
				if (weakThis.TryGetTarget(out var instance))
				{
					instance.OnXamlRootChanged(sender, args);
				}
			}

			xamlRoot.Changed += handler;
			m_xamlRootChangedEventHandler.Disposable =
				Disposable.Create(() => xamlRoot.Changed -= handler);
		}

		if (m_tpLayoutRootPart != null)
		{
			m_tpLayoutRootPart.Loaded += OnLayoutRootLoaded;
			m_epLayoutRootLoadedHandler.Disposable =
				Disposable.Create(() => m_tpLayoutRootPart.Loaded -= OnLayoutRootLoaded);

			m_tpLayoutRootPart.KeyDown += OnLayoutRootKeyDown;
			m_epLayoutRootKeyDownHandler.Disposable =
				Disposable.Create(() => m_tpLayoutRootPart.KeyDown -= OnLayoutRootKeyDown);

			m_tpLayoutRootPart.KeyUp += OnLayoutRootKeyUp;
			m_epLayoutRootKeyUpHandler.Disposable =
				Disposable.Create(() => m_tpLayoutRootPart.KeyUp -= OnLayoutRootKeyUp);

			m_tpLayoutRootPart.GotFocus += OnLayoutRootGotFocus;
			m_epLayoutRootGotFocusHandler.Disposable =
				Disposable.Create(() => m_tpLayoutRootPart.GotFocus -= OnLayoutRootGotFocus);

			m_tpLayoutRootPart.ProcessKeyboardAccelerators += OnLayoutRootProcessKeyboardAccelerators;
			m_epLayoutRootProcessKeyboardAcceleratorsHandler.Disposable =
				Disposable.Create(() => m_tpLayoutRootPart.ProcessKeyboardAccelerators -= OnLayoutRootProcessKeyboardAccelerators);
		}

		if (m_tpBackgroundElementPart != null)
		{
			m_tpBackgroundElementPart.SizeChanged += OnDialogSizeChanged;
			m_dialogSizeChangedHandler.Disposable =
				Disposable.Create(() => m_tpBackgroundElementPart.SizeChanged -= OnDialogSizeChanged);
		}

		AttachButtonEvents();

		// In case the commands were set before the template was applied, we won't have responded to the property-change event.
		// We should set the button properties from the commands at this point to ensure they're set properly.
		SetButtonPropertiesFromCommand(ContentDialogButton.Primary);
		SetButtonPropertiesFromCommand(ContentDialogButton.Secondary);
		SetButtonPropertiesFromCommand(ContentDialogButton.Close);

		// If the template changes while the dialog is showing, we need to re-attached to the
		// dialog showing states changed event so that we can fire the hiding event at the
		// right time.
		if (m_placementMode == PlacementMode.InPlace && m_isShowing)
		{
			if (GetTemplateChild("DialogShowingStates") is VisualStateGroup dialogShowingStates)
			{
				dialogShowingStates.CurrentStateChanged += OnDialogShowingStateChanged;
				m_dialogShowingStateChangedEventHandler.Disposable =
					Disposable.Create(() => dialogShowingStates.CurrentStateChanged -= OnDialogShowingStateChanged);
				m_tpDialogShowingStates = dialogShowingStates;
			}
		}

		// Attempt to set the m_tpSmokeLayer field as a FrameworkElement template part, in Popup placement.
		if (m_tpLayoutRootPart != null)
		{
			var smokeLayerPartAsFE = GetTemplateChild("SmokeLayerBackground") as FrameworkElement;

			if (smokeLayerPartAsFE != null)
			{
				// Unparent smokeLayerPartAsFE in both Popup and InPlace placements. It can be reparented later in PrepareSmokeLayer() in Popup placement.
				var layoutRootChildren = m_tpLayoutRootPart.Children;

				var indexOfSmokeLayerPart = layoutRootChildren.IndexOf(smokeLayerPartAsFE as UIElement);

				if (indexOfSmokeLayerPart >= 0)
				{
					layoutRootChildren.RemoveAt(indexOfSmokeLayerPart);

					if (m_placementMode == PlacementMode.TransplantedRootInPopup || m_placementMode == PlacementMode.EntireControlInPopup)
					{
						// When m_tpSmokeLayer was previously set by a PrepareSmokeLayer call while m_tpLayoutRootPart was still unknown,
						// HostDialogWithinPopup needs to be called again to set up m_tpSmokeLayer, m_tpSmokeLayerPopup and m_tpPopup.
						m_prepareSmokeLayerAndPopup = m_tpSmokeLayer != null;
						m_tpSmokeLayer = smokeLayerPartAsFE;

						m_isSmokeLayerATemplatePart = true;
					}
				}
			}
		}

		UpdateVisualState();
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		if (m_isShowing)
		{
			return base.ArrangeOverride(arrangeSize);
		}
		else
		{
			return new Size(0, 0);
		}
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);

		if (!m_isTemplateApplied)
		{
			return;
		}

		bool fullSizeDesired = FullSizeDesired;

		// ButtonsVisibilityStates
		{
			var primaryText = PrimaryButtonText;
			var secondaryText = SecondaryButtonText;
			var closeText = CloseButtonText;

			bool hasPrimary = !string.IsNullOrEmpty(primaryText);
			bool hasSecondary = !string.IsNullOrEmpty(secondaryText);
			bool hasClose = !string.IsNullOrEmpty(closeText);

			var buttonVisibilityState = "NoneVisible";
			if (hasPrimary && hasSecondary && hasClose)
			{
				buttonVisibilityState = "AllVisible";
			}
			else if (hasPrimary && hasSecondary)
			{
				buttonVisibilityState = "PrimaryAndSecondaryVisible";
			}
			else if (hasPrimary && hasClose)
			{
				buttonVisibilityState = "PrimaryAndCloseVisible";
			}
			else if (hasSecondary && hasClose)
			{
				buttonVisibilityState = "SecondaryAndCloseVisible";
			}
			else if (hasPrimary)
			{
				buttonVisibilityState = "PrimaryVisible";
			}
			else if (hasSecondary)
			{
				buttonVisibilityState = "SecondaryVisible";
			}
			else if (hasClose)
			{
				buttonVisibilityState = "CloseVisible";
			}

			GoToState(useTransitions, buttonVisibilityState);
		}

		// DefaultButtonStates
		{
			var defaultButtonState = "NoDefaultButton";

			var defaultButton = DefaultButton;

			if (defaultButton != ContentDialogButton.None)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				var focusedElement = focusManager?.FocusedElement as DependencyObject;

				bool isFocusInCommandArea = false;
				if (m_tpCommandSpacePart != null && focusedElement != null)
				{
					isFocusInCommandArea = m_tpCommandSpacePart.IsAncestorOf(focusedElement);
				}

				// If focus is not in the command area, set the default button visualization just based on the property value.
				// If focus is in the command area, set the default button visualization only if it has focus.
				if (defaultButton == ContentDialogButton.Primary)
				{
					if (!isFocusInCommandArea || ReferenceEquals(m_tpPrimaryButtonPart, focusedElement))
					{
						defaultButtonState = "PrimaryAsDefaultButton";
					}
				}
				else if (defaultButton == ContentDialogButton.Secondary)
				{
					if (!isFocusInCommandArea || ReferenceEquals(m_tpSecondaryButtonPart, focusedElement))
					{
						defaultButtonState = "SecondaryAsDefaultButton";
					}
				}
				else if (defaultButton == ContentDialogButton.Close)
				{
					if (!isFocusInCommandArea || ReferenceEquals(m_tpCloseButtonPart, focusedElement))
					{
						defaultButtonState = "CloseAsDefaultButton";
					}
				}
			}

			GoToState(useTransitions, defaultButtonState);
		}

		// DialogShowingStates
		if (m_placementMode == PlacementMode.InPlace)
		{
			if (m_tpDialogShowingStates != null)
			{
				GoToState(true, m_isShowing && !m_hideInProgress ? "DialogShowing" : "DialogHidden");
			}
			else if (m_tpLayoutRootPart != null)
			{
				// We don't have a state transition defined so brute force it.
				m_tpLayoutRootPart.Visibility = m_isShowing && !m_hideInProgress ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		else if (m_placementMode != PlacementMode.Undetermined)
		{
			// For ContentDialog's shown in the popup, set the state to always showing since the opened
			// state of the popup effectively controls whether its showing it not.
			GoToState(false, "DialogShowingWithoutSmokeLayer");
		}

		// DialogSizingStates
		{
			GoToState(useTransitions, fullSizeDesired ? "FullDialogSizing" : "DefaultDialogSizing");
		}

		// DialogBorderStates
		{
			GoToState(useTransitions, "NoBorder");
		}

#if HAS_UNO
		AdjustVisualStateForInputPane();
#endif
	}

	/// <summary>
	/// Hides the dialog.
	/// </summary>
	public void Hide()
	{
		if (m_isShowing)
		{
			HideInternal(ContentDialogResult.None);
		}
	}

	internal bool Hide(ContentDialogResult result)
	{
		return HideInternal(result);
	}

	private bool HideInternal(ContentDialogResult result)
	{
		if (m_tpCurrentAsyncOperation == null || m_hideInProgress)
		{
#if HAS_UNO
			// Uno specific: Preserve legacy behavior for synchronous hide
			if (m_isShowing && !m_hideInProgress)
			{
				return HideInternalUno(result);
			}
#endif
			return false;
		}

		EnsureDeferralManagers();

		m_hideInProgress = true;

		bool doFireClosing = ShouldFireClosing();

		ContentDialogClosingEventArgs args = null;
		if (doFireClosing)
		{
			args = new ContentDialogClosingEventArgs(OnClosingDeferralComplete, result);
			Closing?.Invoke(this, args);
		}

		// Continue after deferral
		bool completedSynchronously = args?.DeferralManager.EventRaiseCompleted() ?? true;

		if (!doFireClosing || completedSynchronously)
		{
			bool isCanceled = args?.Cancel ?? false;
			HideAfterDeferralWorker(isCanceled, result);
		}

		return completedSynchronously && !(args?.Cancel ?? false);
	}

#if HAS_UNO
	private bool HideInternalUno(ContentDialogResult result)
	{
		// Uno specific: Legacy synchronous hide path for backward compatibility
		m_hideInProgress = true;

		void Complete(ContentDialogClosingEventArgs closingArgs)
		{
			if (!closingArgs.Cancel)
			{
				m_isShowing = false;
				m_tpPopup.IsOpen = false;
				m_tpPopup.Child = null;
				UpdateVisualState();
				Closed?.Invoke(this, new ContentDialogClosedEventArgs(result));

				(var tcs, m_tcs) = (m_tcs, null);
				DispatcherQueue.TryEnqueue(() => tcs?.TrySetResult(result));
			}
			m_hideInProgress = false;
		}

		var args = new ContentDialogClosingEventArgs(Complete, result);
		Closing?.Invoke(this, args);

		var completedSynchronously = args.DeferralManager.EventRaiseCompleted();
		return completedSynchronously && !args.Cancel;
	}
#endif

	private void OnClosingDeferralComplete(ContentDialogClosingEventArgs args)
	{
		HideAfterDeferralWorker(args.Cancel, args.Result);
	}

	private void HideAfterDeferralWorker(bool isCanceled, ContentDialogResult result)
	{
		bool isPopupOpen = false;
		if (m_placementMode != PlacementMode.InPlace && m_tpPopup != null)
		{
			isPopupOpen = m_tpPopup.IsOpen;
		}

		// For the popup hosted scenarios, cancel hiding the dialog only if the popup is still open.
		// For the inline scenarios, since there's no popup, always respect the cancel flag.
		if (isCanceled && (isPopupOpen || m_placementMode == PlacementMode.InPlace))
		{
			m_hideInProgress = false;
		}
		else
		{
			// Try to restore focus back to the original focused element.
			if (m_spFocusedElementBeforeContentDialogShows?.Target is UIElement previousFocusedElement)
			{
				previousFocusedElement.Focus(FocusState.Programmatic);
			}

			if (m_placementMode == PlacementMode.InPlace)
			{
				UpdateVisualState();
			}
			else
			{
				if (m_tpPopup != null)
				{
					// Attach unloaded handler
					var popupChild = m_placementMode == PlacementMode.EntireControlInPopup ? this : m_tpLayoutRootPart as FrameworkElement;
					if (popupChild != null)
					{
						popupChild.Unloaded += OnPopupChildUnloaded;
						m_popupChildUnloadedEventHandler.Disposable =
							Disposable.Create(() => popupChild.Unloaded -= OnPopupChildUnloaded);
					}

					// Make these elements non-interactable while the closing transition plays.
					if (m_tpSmokeLayer is UIElement smokeLayerUE)
					{
						smokeLayerUE.IsHitTestVisible = false;
					}

					if (m_tpLayoutRootPart != null)
					{
						m_tpLayoutRootPart.IsHitTestVisible = false;
					}

					m_tpPopup.IsOpen = false;
				}
			}

#if HAS_UNO
			// TODO Uno: BackButtonIntegration_UnregisterListener equivalent
			// IFC_RETURN(BackButtonIntegration_UnregisterListener(this));

			// TODO Uno: ElementSoundPlayerService equivalent
			// IFC_RETURN(DirectUI::ElementSoundPlayerService::RequestInteractionSoundForElementStatic(xaml::ElementSoundKind_Hide, this));
#endif
		}

		// For the popup hosted scenario, the OnFinishedClosing() method is usually called in the
		// popup's closed event handler, but only if the popup was closed via another means.
		if (m_placementMode != PlacementMode.InPlace && !isPopupOpen)
		{
			OnFinishedClosing();
		}
	}

	/// <summary>
	/// Begins an asynchronous operation to show the dialog.
	/// </summary>
	/// <returns>An asynchronous operation to show the dialog. When complete, returns a ContentDialogResult.</returns>
	public IAsyncOperation<ContentDialogResult> ShowAsync()
		=> ShowAsync(ContentDialogPlacement.Popup);

	/// <summary>
	/// Begins an asynchronous operation to show the dialog with the specified placement.
	/// </summary>
	/// <param name="placement">A value that specifies how to display the dialog.</param>
	/// <returns>An asynchronous operation to show the dialog. When complete, returns a ContentDialogResult.</returns>
	public IAsyncOperation<ContentDialogResult> ShowAsync(ContentDialogPlacement placement)
		=> AsyncOperation.FromTask(async ct =>
		{
#if HAS_UNO
			if (m_tpPopup.IsOpen)
			{
				throw new InvalidOperationException("A ContentDialog is already opened.");
			}

			// TODO: support in-place and transplanted root modes
			m_placementMode = PlacementMode.EntireControlInPopup;

			// reset previous focused element
			m_spFocusedElementBeforeContentDialogShows = null;

			// Save the currently focused element
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			if (focusManager?.FocusedElement is DependencyObject previouslyFocused)
			{
				m_spFocusedElementBeforeContentDialogShows = new WeakReference(previouslyFocused);
			}

			m_isShowing = true;
			m_hideInProgress = false;
			m_skipClosingEventOnHide = true;
			m_hasPreparedContent = false;

			// Make sure default template is applied, so visual states etc can be set correctly
			EnsureTemplate();
			if (HasValidAppliedTemplate())
			{
				PrepareContent();
			}

			m_tpPopup.Child = this;
			m_tpPopup.IsOpen = true;
			m_tpPopup.IsLightDismissEnabled = false;

			m_tcs = new TaskCompletionSource<ContentDialogResult>();
			m_tpCurrentAsyncOperation = AsyncOperation.FromTask(_ => m_tcs.Task);

			using (ct.Register(() =>
			{
				m_tcs.TrySetCanceled();
				Hide();
			}
#if !__WASM__
				, useSynchronizationContext: true
#endif
				))
			{
				return await m_tcs.Task;
			}
#else
			// TODO Uno: Non-Uno path for ShowAsync
			throw new NotImplementedException();
#endif
		});

	private void OnCommandButtonClicked(
		TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler,
		ICommand command,
		object commandParameter,
		ContentDialogResult result)
	{
		if (m_tpCurrentAsyncOperation == null)
		{
			return;
		}

		if (!m_hideInProgress)
		{
			EnsureDeferralManagers();

			var args = new ContentDialogButtonClickEventArgs(OnButtonClickDeferralComplete(command, commandParameter, result));

			clickEventHandler?.Invoke(this, args);

			bool completedSynchronously = args.DeferralManager.EventRaiseCompleted();

			if (completedSynchronously)
			{
				if (!args.Cancel)
				{
					if (command != null)
					{
						command.Execute(commandParameter);
					}

					HideInternal(result);
				}
			}
		}
	}

	private Action<ContentDialogButtonClickEventArgs> OnButtonClickDeferralComplete(ICommand command, object commandParameter, ContentDialogResult result)
	{
		return (args) =>
		{
			if (!args.Cancel)
			{
				if (command != null)
				{
					command.Execute(commandParameter);
				}

				HideInternal(result);
			}
		};
	}

	private void EnsureDeferralManagers()
	{
		m_spClosingDeferralManager ??= new DeferralManager<ContentDialogClosingDeferral>(h => new ContentDialogClosingDeferral(h));

		m_spButtonClickDeferralManager ??= new DeferralManager<ContentDialogButtonClickDeferral>(h => new ContentDialogButtonClickDeferral(h));
	}

	private void ResetAndPrepareContent()
	{
		if (m_isTemplateApplied && m_tpCurrentAsyncOperation != null && m_hasPreparedContent)
		{
			m_hasPreparedContent = false;
			PrepareContent();
		}
	}

	private void PrepareContent()
	{
		if (!m_hasPreparedContent)
		{
			if (m_placementMode == PlacementMode.TransplantedRootInPopup)
			{
				// We set the width/height of the container to 0 to ensure that the
				// inverse transform we apply to Popup behaves correctly when in RTL mode.
				if (m_tpContainerPart != null)
				{
					m_tpContainerPart.Width = 0.0;
					m_tpContainerPart.Height = 0.0;
				}
			}
			else if (m_placementMode == PlacementMode.InPlace && m_isLayoutRootTransplanted)
			{
				// If a dialog that had previously been shown in a popup is now being shown in place,
				// restore the layout root that had been transplanted out of the dialog's tree.
				if (m_tpContainerPart != null && m_tpLayoutRootPart != null)
				{
					m_tpContainerPart.Child = m_tpLayoutRootPart;
					m_tpContainerPart.Width = double.NaN;
					m_tpContainerPart.Height = double.NaN;
				}

				m_isLayoutRootTransplanted = false;
			}

			UpdateVisualState();
			UpdateTitleSpaceVisibility();

			if (m_placementMode != PlacementMode.InPlace)
			{
				SizeAndPositionContentInPopup();
			}

			// TODO Uno: Cast a shadow (ApplyElevationEffect)
			// if (m_tpBackgroundElementPart != null)
			// {
			//     if (CThemeShadow.IsDropShadowMode())
			//     {
			//         ApplyElevationEffect(m_tpBackgroundElementPart, 0 /* depth */, 128 /* baseElevation */);
			//     }
			//     else
			//     {
			//         ApplyElevationEffect(m_tpBackgroundElementPart);
			//     }
			// }

			m_hasPreparedContent = true;
		}
	}

	private void SizeAndPositionContentInPopup()
	{
		if (m_tpPopup == null)
		{
			return;
		}

		// Eventually, I think we want to center the popup, rather than make the popup full window size and then
		// center the content. For now, to handle invalid templates, we just won't position if we don't have
		// the right parts.
		if (m_tpBackgroundElementPart == null || m_tpLayoutRootPart == null)
		{
			return;
		}

		double xOffset = 0;
		double yOffset = 0;

		if (XamlRoot == null)
		{
#if HAS_UNO
			if (this is not Uno.UI.WinRT.Extensions.UI.Popups.MessageDialogContentDialog)
			{
				throw new InvalidOperationException(
					"Trying to set position of the dialog before it is associated with a visual tree. " +
					"This can happen if the dialog's XamlRoot was not set.");
			}
			else
			{
				throw new InvalidOperationException(
					"Trying to set position of the dialog before it is associated with a visual tree. " +
					"Make sure to use InitializeWithWindow before calling ShowAsync.");
			}
#else
			return;
#endif
		}

#if HAS_UNO
		var xamlRootSize = XamlRoot.VisualTree.VisibleBounds.Size;
#else
		var xamlRootSize = XamlRoot.Size;
#endif

		var flowDirection = FlowDirection;

		// Uno note: we're using latest template version. For now we're not supporting legacy templates.

		if (m_placementMode == PlacementMode.EntireControlInPopup)
		{
			Height = xamlRootSize.Height;
			Width = xamlRootSize.Width;
		}
		else if (m_tpLayoutRootPart != null)
		{
			m_tpLayoutRootPart.Height = xamlRootSize.Height;
			m_tpLayoutRootPart.Width = xamlRootSize.Width;
		}

		// When the ContentDialog is in the visual tree, the popup offset has added
		// to it the top-left point of where layout measured and arranged it to.
		// Since we want ContentDialog to be an overlay, we need to subtract off that
		// point in order to ensure the ContentDialog is always being displayed in
		// window coordinates instead of local coordinates.
		if (m_placementMode == PlacementMode.TransplantedRootInPopup)
		{
			GeneralTransform transformToRoot = m_tpPopup.TransformToVisual(null);
			var offsetFromRoot = transformToRoot.TransformPoint(default);

			xOffset =
				flowDirection == FlowDirection.LeftToRight ?
				(xOffset - offsetFromRoot.X) :
				(xOffset - offsetFromRoot.X) * -1;

			yOffset -= offsetFromRoot.Y;
		}

		// Set the ContentDialog left and top position.
		m_tpPopup.HorizontalOffset = xOffset;
		m_tpPopup.VerticalOffset = yOffset;
	}

	private void UpdateTitleSpaceVisibility()
	{
		if (m_tpTitlePart != null)
		{
			var hasTitle = Title != null || TitleTemplate != null;
			m_tpTitlePart.Visibility = hasTitle ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private void OnLayoutRootLoaded(object sender, RoutedEventArgs args)
	{
		// This handler will get executed when the layout root is loaded within the popup and also (in
		// the cases of dialogs defined in markup) when it is put back into the ContentDialog's
		// tree when finishing closing, so make sure to only execute the following code when the dialog
		// is actually showing.
		if (m_isShowing && m_placementMode != PlacementMode.InPlace)
		{
			SetInitialFocusElement();
			SizeAndPositionContentInPopup();
		}
	}

	private void OnLayoutRootKeyDown(object sender, KeyRoutedEventArgs args)
	{
		ProcessLayoutRootKey(true, args);
	}

	private void OnLayoutRootKeyUp(object sender, KeyRoutedEventArgs args)
	{
		ProcessLayoutRootKey(false, args);
	}

	private void OnLayoutRootGotFocus(object sender, RoutedEventArgs args)
	{
		// Update which command button has the default button visualization.
		UpdateVisualState();
	}

	private void OnLayoutRootProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
	{
		// If we're already in the middle of processing accelerators, we don't need to do anything.
		if (m_isProcessingKeyboardAccelerators)
		{
			return;
		}

		var key = args.Key;

		// Even if TryInvokeKeyboardAccelerator fails, we want to always mark the args as handled for ContentDialog to ensure the dialog appears
		// as if it's modal.
		try
		{
			if (m_tpLayoutRootPart != null)
			{
				// This handler is being called from the layout root - so we shouldn't just call TryInvokeKeyboardAccelerators on the layout root itself,
				// as that would cause the event on the layout root to be raised again and we'd be in an infinite loop here.
				m_isProcessingKeyboardAccelerators = true;
				m_tpLayoutRootPart.TryInvokeKeyboardAccelerator(args);
			}
		}
		finally
		{
			m_isProcessingKeyboardAccelerators = false;

			if (key != VirtualKey.Escape && key != VirtualKey.Enter)
			{
				if (!args.Handled)
				{
					// As we are marking each key handled though it's not actually, we will set HandledShouldNotImpedeTextInput
					// property true.
					// TODO Uno: HandledShouldNotImpedeTextInput is not yet available
					// args.HandledShouldNotImpedeTextInput = true;
					args.Handled = true;
				}
			}
		}
	}

	private void ProcessLayoutRootKey(bool isKeyDown, KeyRoutedEventArgs args)
	{
		var key = args.Key;

		if (key == VirtualKey.Escape)
		{
			var originalKey = args.OriginalKey;

			if ((!isKeyDown && originalKey == VirtualKey.GamepadB) ||
				(isKeyDown && originalKey == VirtualKey.Escape))
			{
				ExecuteCloseAction();
				args.Handled = true;
			}
		}
		else if (key == VirtualKey.Enter)
		{
			if (isKeyDown)
			{
				var defaultButton = GetDefaultButtonHelper();

				if (defaultButton is ButtonBase buttonBase && buttonBase.IsEnabled)
				{
					// TODO Uno: ProgrammaticClick is internal on ButtonBase
					// buttonBase.ProgrammaticClick();
					var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(buttonBase);
					if (peer is Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer buttonPeer)
					{
						buttonPeer.Invoke();
					}
					args.Handled = true;
				}
			}
		}
	}

#if HAS_UNO
	// Uno specific: Override for MessageDialogContentDialog compatibility
	private protected virtual void OnPopupKeyDown(object sender, KeyRoutedEventArgs e)
	{
		// This is now handled by OnLayoutRootKeyDown/ProcessLayoutRootKey,
		// but kept for backward compatibility with MessageDialogContentDialog.
		ProcessLayoutRootKey(true, e);
	}
#endif

	private bool ShouldFireClosing()
	{
		if (m_skipClosingEventOnHide)
		{
			return false;
		}

		// TODO Uno: Check if async operation was canceled
		// In WinUI this checks the async operation status, which we don't have in the same way.

		return true;
	}

	private void DetachEventHandlers()
	{
		DetachButtonEvents();

		m_epLayoutRootLoadedHandler.Disposable = null;
		m_epLayoutRootKeyDownHandler.Disposable = null;
		m_epLayoutRootKeyUpHandler.Disposable = null;
		m_epLayoutRootGotFocusHandler.Disposable = null;
		m_epLayoutRootProcessKeyboardAcceleratorsHandler.Disposable = null;
		m_dialogSizeChangedHandler.Disposable = null;
		m_dialogShowingStateChangedEventHandler.Disposable = null;
		m_xamlRootChangedEventHandler.Disposable = null;
	}

	private void DetachButtonEvents()
	{
		m_epPrimaryButtonClickHandler.Disposable = null;
		m_epSecondaryButtonClickHandler.Disposable = null;
		m_epCloseButtonClickHandler.Disposable = null;
	}

	private void AttachButtonEvents()
	{
		if (m_tpPrimaryButtonPart != null && m_epPrimaryButtonClickHandler.Disposable == null)
		{
			m_tpPrimaryButtonPart.Click += OnPrimaryButtonClicked;
			m_epPrimaryButtonClickHandler.Disposable =
				Disposable.Create(() => m_tpPrimaryButtonPart.Click -= OnPrimaryButtonClicked);
		}

		if (m_tpSecondaryButtonPart != null && m_epSecondaryButtonClickHandler.Disposable == null)
		{
			m_tpSecondaryButtonPart.Click += OnSecondaryButtonClicked;
			m_epSecondaryButtonClickHandler.Disposable =
				Disposable.Create(() => m_tpSecondaryButtonPart.Click -= OnSecondaryButtonClicked);
		}

		if (m_tpCloseButtonPart != null && m_epCloseButtonClickHandler.Disposable == null)
		{
			m_tpCloseButtonPart.Click += OnCloseButtonClicked;
			m_epCloseButtonClickHandler.Disposable =
				Disposable.Create(() => m_tpCloseButtonPart.Click -= OnCloseButtonClicked);
		}
	}

	private void OnPrimaryButtonClicked(object sender, RoutedEventArgs e)
	{
		OnCommandButtonClicked(
			PrimaryButtonClick,
			PrimaryButtonCommand,
			PrimaryButtonCommandParameter,
			ContentDialogResult.Primary);
	}

	private void OnSecondaryButtonClicked(object sender, RoutedEventArgs e)
	{
		OnCommandButtonClicked(
			SecondaryButtonClick,
			SecondaryButtonCommand,
			SecondaryButtonCommandParameter,
			ContentDialogResult.Secondary);
	}

	private void OnCloseButtonClicked(object sender, RoutedEventArgs e)
	{
		OnCommandButtonClicked(
			CloseButtonClick,
			CloseButtonCommand,
			CloseButtonCommandParameter,
			ContentDialogResult.None);
	}

	private ButtonBase GetButtonHelper(ContentDialogButton buttonType)
	{
		return buttonType switch
		{
			ContentDialogButton.Primary => m_tpPrimaryButtonPart,
			ContentDialogButton.Secondary => m_tpSecondaryButtonPart,
			ContentDialogButton.Close => m_tpCloseButtonPart,
			_ => null,
		};
	}

	private ButtonBase GetDefaultButtonHelper()
	{
		return GetButtonHelper(DefaultButton);
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		if (m_tpSmokeLayer is FrameworkElement smokeLayerFE)
		{
			var currentXamlRootSize = XamlRoot?.Size ?? default;
			smokeLayerFE.Width = currentXamlRootSize.Width;
			smokeLayerFE.Height = currentXamlRootSize.Height;
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			SizeAndPositionContentInPopup();
		}
	}

	private void OnDialogSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (m_prepareSmokeLayerAndPopup)
		{
			// HostDialogWithinPopup needs to be called again to set up m_tpSmokeLayerPopup and m_tpPopup with the new m_tpSmokeLayer.
			m_prepareSmokeLayerAndPopup = false;
			// TODO Uno: HostDialogWithinPopup(true) for template part smoke layer
		}

		UpdateVisualState();

		ResetAndPrepareContent();

		if (m_placementMode != PlacementMode.InPlace)
		{
			SizeAndPositionContentInPopup();
		}
	}

	private void OnDialogShowingStateChanged(object sender, VisualStateChangedEventArgs args)
	{
		// Fire the opened or closed events after visual transitions have played.
		if (m_isShowing && !m_hideInProgress)
		{
			SetInitialFocusElement();
			RaiseOpenedEvent();
		}
		else
		{
			m_hideInProgress = false;
			OnFinishedClosing();
		}
	}

	private void OnPopupChildUnloaded(object sender, RoutedEventArgs args)
	{
		if (m_tpCurrentAsyncOperation == null)
		{
			// If m_tpCurrentAsyncOperation is null, then that means that we've already handled the popup closing.
			return;
		}

		if (m_hideInProgress)
		{
			m_hideInProgress = false;
			OnFinishedClosing();
		}
		else
		{
			// If the app directly closed the popup, go through the closing actions for ContentDialog.
			m_skipClosingEventOnHide = true;
			HideInternal(ContentDialogResult.None);
		}
	}

	private void OnFinishedClosing()
	{
		m_isShowing = false;

		UpdateVisualState();

		if (m_placementMode != PlacementMode.InPlace)
		{
			// No longer need the handler so detach it.
			m_popupChildUnloadedEventHandler.Disposable = null;

			if (m_tpSmokeLayerPopup != null)
			{
				m_tpSmokeLayerPopup.IsOpen = false;
				// TODO Uno: UpdateCanDragStatusWindowChrome(true) - re-enable dragging in custom titlebar
			}

			// Break circular reference with ContentDialog.
			if (m_tpPopup != null)
			{
				m_tpPopup.Child = null;
			}
		}

		var result = ContentDialogResult.None;
		// TODO Uno: Get the result from the async operation
		// In WinUI this reads the result from the async operation.

		RaiseClosedEvent(result);

		// We use a new deferral manager each time the ContentDialog is shown because the
		// dialog may be forcibly closed and then reshown while a button click deferral is pending.
		m_spButtonClickDeferralManager = null;

		if (m_dialogShowingStateChangedEventHandler.Disposable != null)
		{
			m_dialogShowingStateChangedEventHandler.Disposable = null;
			m_tpDialogShowingStates = null;
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			// Now that we've finished closing, make these interactable again.
			if (m_tpSmokeLayer is UIElement smokeLayerUE)
			{
				smokeLayerUE.IsHitTestVisible = true;
			}

			if (m_tpLayoutRootPart != null)
			{
				m_tpLayoutRootPart.IsHitTestVisible = true;
			}
		}

		// Reset this so that it can be re-evaluated the next time the dialog is opened.
		m_placementMode = PlacementMode.Undetermined;

		// We clear the tracker pointer here to allow for callers
		// to perform an additional ShowAsync from the completion handler.
		m_tpCurrentAsyncOperation = null;

#if HAS_UNO
		// Uno specific: Complete the TaskCompletionSource
		// This is already handled in HideInternalUno or by the cancellation token.
#endif
	}

	private void SetInitialFocusElement()
	{
		bool wasFocusSet = false;

		// Save the focused element in order to give focus back to that once the ContentDialog dismisses.
		var focusManager = VisualTree.GetFocusManagerForElement(this);
		if (focusManager?.FocusedElement is DependencyObject previouslyFocused)
		{
			m_spFocusedElementBeforeContentDialogShows = new WeakReference(previouslyFocused);
		}

		// Try to set focus to the first focusable element in the content area.
		if (m_tpContentPart != null)
		{
			// We need to make sure all the nested templates of the ScrollViewer are expanded
			if (m_tpContentScrollViewerPart != null)
			{
				m_tpContentScrollViewerPart.UpdateLayout();
			}

			if (focusManager?.GetFirstFocusableElement(m_tpContentPart) is UIElement firstFocusable)
			{
				// TODO Uno: InitialFocusSIPSuspender equivalent
				wasFocusSet = firstFocusable.Focus(FocusState.Programmatic);
			}
		}

		// If not set, try to focus the default button.
		if (!wasFocusSet)
		{
			var defaultButton = GetDefaultButtonHelper();
			if (defaultButton != null)
			{
				wasFocusSet = defaultButton.Focus(FocusState.Programmatic);
			}
		}

		// If not set, try to focus the first focusable command button.
		if (!wasFocusSet && m_tpCommandSpacePart != null)
		{
			if (focusManager?.GetFirstFocusableElement(m_tpCommandSpacePart) is UIElement firstFocusableCommand)
			{
				wasFocusSet = firstFocusableCommand.Focus(FocusState.Programmatic);
			}
		}
	}

	private void ExecuteCloseAction()
	{
		bool didInvokeClose = false;

		var closeButtonText = CloseButtonText;

		// If we have a clickable close button, then invoke it, otherwise just
		// return a result of None.
		if (!string.IsNullOrEmpty(closeButtonText))
		{
			if (m_tpCloseButtonPart is ButtonBase closeButton && closeButton.IsEnabled)
			{
				// TODO Uno: ProgrammaticClick is internal on ButtonBase
				var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(closeButton);
				if (peer is Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer buttonPeer)
				{
					buttonPeer.Invoke();
				}
				didInvokeClose = true;
			}
		}

		// If there was no close button to invoke, then just call hide.
		if (!didInvokeClose)
		{
			HideInternal(ContentDialogResult.None);
		}
	}

	private void RaiseOpenedEvent()
	{
		Opened?.Invoke(this, new ContentDialogOpenedEventArgs());
	}

	private void RaiseClosedEvent(ContentDialogResult result)
	{
		Closed?.Invoke(this, new ContentDialogClosedEventArgs(result));
	}

	private void SetPopupAutomationProperties()
	{
		if (m_tpPopup == null)
		{
			return;
		}

		// If a Title string exists, make it the default AutomationProperties.Name string for the Popup.
		var title = Title;
		if (title != null)
		{
			var titleString = title.ToString();
			AutomationProperties.SetName(m_tpPopup, titleString);
		}

		// Update the automation Id as well.
		var automationId = AutomationProperties.GetAutomationId(this);
		if (!string.IsNullOrEmpty(automationId))
		{
			AutomationProperties.SetAutomationId(m_tpPopup, automationId);
		}

		// Set IsDialog property to True for popup
		AutomationProperties.SetIsDialog(m_tpPopup, true);
	}

	private void SetButtonPropertiesFromCommand(ContentDialogButton buttonType, ICommand oldCommand = null)
	{
		if (buttonType == ContentDialogButton.None)
		{
			return;
		}

		var button = GetButtonHelper(buttonType);

		if (button != null)
		{
			DependencyProperty textProperty = buttonType switch
			{
				ContentDialogButton.Primary => PrimaryButtonTextProperty,
				ContentDialogButton.Secondary => SecondaryButtonTextProperty,
				ContentDialogButton.Close => CloseButtonTextProperty,
				_ => null,
			};

			ICommand newCommand = buttonType switch
			{
				ContentDialogButton.Primary => PrimaryButtonCommand,
				ContentDialogButton.Secondary => SecondaryButtonCommand,
				ContentDialogButton.Close => CloseButtonCommand,
				_ => null,
			};

			if (oldCommand != null)
			{
				if (textProperty != null)
				{
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, textProperty);
				}
				CommandingHelpers.ClearBindingIfSet(oldCommand, button, UIElement.KeyboardAcceleratorsProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommand, button, UIElement.AccessKeyProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommand, button, Automation.AutomationProperties.HelpTextProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommand, button, ToolTipService.ToolTipProperty);
			}

			if (newCommand != null)
			{
				if (textProperty != null)
				{
					CommandingHelpers.BindToLabelPropertyIfUnset(newCommand, this, textProperty);
				}
				if (newCommand is Input.XamlUICommand uiCommand)
				{
					CommandingHelpers.BindToKeyboardAcceleratorsIfUnset(uiCommand, button);
					CommandingHelpers.BindToAccessKeyIfUnset(uiCommand, button);
					CommandingHelpers.BindToDescriptionPropertiesIfUnset(uiCommand, button);
				}
			}
		}
	}

	private bool HasValidAppliedTemplate() => m_tpBackgroundElementPart != null;

#if HAS_UNO
	private void AdjustVisualStateForInputPane()
	{
#if __ANDROID__
		if (m_tpPopup?.UseNativePopup == true)
		{
			// Skip managed adjustment since the popup itself will adjust to the soft keyboard.
			return;
		}
#endif
		Rect inputPaneRect = InputPane.GetForCurrentView().OccludedRect;

		if (m_isShowing && inputPaneRect.Height > 0)
		{
			if (m_tpLayoutRootPart == null || m_tpBackgroundElementPart == null)
			{
				return;
			}

			Rect getElementBounds(FrameworkElement element)
			{
				GeneralTransform transform = element.TransformToVisual(null);
				var width = element.ActualWidth;
				var height = element.ActualHeight;
				return transform.TransformBounds(new Rect(0, 0, width, height));
			}

			Rect layoutRootBounds = getElementBounds(m_tpLayoutRootPart);
			Rect dialogBounds = getElementBounds(m_tpBackgroundElementPart);

			// If the input pane overlaps the dialog (including a 12px bottom margin), the dialog will get translated
			// up so that is not occluded, while also preserving a 12px margin between the bottom of the dialog
			// and the top of the input pane.
			if (inputPaneRect.Y < (dialogBounds.Y + dialogBounds.Height + ContentDialog_SIP_Bottom_Margin))
			{
				var contentVerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				bool setDialogVisibility = false;
				var dialogVerticalAlignment = VerticalAlignment.Center;

				var layoutRootPadding = new Thickness(0, 0, 0, layoutRootBounds.Height - Math.Max(inputPaneRect.Y - layoutRootBounds.Y, (float)(m_dialogMinHeight)) + ContentDialog_SIP_Bottom_Margin);

				bool fullSizeDesired = FullSizeDesired;
				if (!fullSizeDesired)
				{
					dialogVerticalAlignment = VerticalAlignment.Bottom;
					setDialogVisibility = true;
				}

				// Apply our layout adjustments using a storyboard so that we don't stomp over template or user
				// provided values. When we stop the storyboard, it will restore the previous values.
				var storyboard = CreateStoryboardForLayoutAdjustmentsForInputPane(layoutRootPadding, contentVerticalScrollBarVisibility, setDialogVisibility, dialogVerticalAlignment);

				storyboard.Begin();
				storyboard.SkipToFill();
				m_layoutAdjustmentsForInputPaneStoryboard = storyboard;
			}
		}
		else if (m_layoutAdjustmentsForInputPaneStoryboard != null)
		{
			m_layoutAdjustmentsForInputPaneStoryboard.Stop();
			m_layoutAdjustmentsForInputPaneStoryboard = null;
		}
	}

	private Storyboard CreateStoryboardForLayoutAdjustmentsForInputPane(
		Thickness layoutRootPadding,
		ScrollBarVisibility contentVerticalScrollBarVisibility,
		bool setDialogVerticalAlignment,
		VerticalAlignment dialogVerticalAlignment)
	{
		Storyboard storyboardLocal = new Storyboard();

		var storyboardChildren = storyboardLocal.Children;

		// LayoutRoot Padding
		{
			ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

			Storyboard.SetTarget(objectAnimation, m_tpBackgroundElementPart);
			Storyboard.SetTargetProperty(objectAnimation, "Margin");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = layoutRootPadding;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		// ContentScrollViewer VerticalScrollBarVisibility
		if (m_tpContentScrollViewerPart != null)
		{
			ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

			Storyboard.SetTarget(objectAnimation, m_tpContentScrollViewerPart);
			Storyboard.SetTargetProperty(objectAnimation, "VerticalScrollBarVisibility");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = contentVerticalScrollBarVisibility;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		// BackgroundElement VerticalAlignment
		if (setDialogVerticalAlignment)
		{
			ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();

			Storyboard.SetTarget(objectAnimation, m_tpBackgroundElementPart);
			Storyboard.SetTargetProperty(objectAnimation, "VerticalAlignment");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = dialogVerticalAlignment;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		return storyboardLocal;
	}
#endif

	// TODO Uno: HostDialogWithinPopup, PreparePopup, PrepareSmokeLayer, CreateSmokeLayerBrush, DeferredOpenPopup
	// These are used for the dual-popup architecture in WinUI (one popup for smoke layer, one for dialog).
	// In Uno, we use a single popup with ContentDialogPopupPanel and LightDismissOverlay instead.
	// The WinUI approach would need to be ported if we want full feature parity (InPlace mode,
	// TransplantedRootInPopup mode, opening/closing transitions).
	//
	// Original C++ methods:
	// HostDialogWithinPopup(bool wasSmokeLayerFoundAsTemplatePart)
	// PreparePopup(bool wasSmokeLayerFoundAsTemplatePart)
	// PrepareSmokeLayer()
	// CreateSmokeLayerBrush(IBrush** brush)
	// DeferredOpenPopup()
	// UpdateCanDragStatusWindowChrome(bool dragEnabled)

	// TODO Uno: DiscardPopup
	// Original C++:
	// if (auto popup = m_tpPopup.GetSafeReference())
	// {
	//     if (m_popupOpenedHandler)
	//     {
	//         m_popupOpenedHandler.DetachEventHandler(popup.Get());
	//     }
	// }
	// m_tpPopup.Clear();

	// TODO Uno: GetPlainText - used for automation
	// Original C++: Returns Title as string, or falls back to base GetPlainText.

	// TODO Uno: put_XamlRootImpl
	// Original C++: Associates the popup and smoke layer popup with the correct visual tree.

	// TODO Uno: GetFocusedElementPosition
	// Original C++: Gets the position of the focused element before the dialog was shown.
}
