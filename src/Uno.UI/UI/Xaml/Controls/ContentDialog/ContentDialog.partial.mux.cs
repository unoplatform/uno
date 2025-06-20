// MUX Reference ContentDialog_Partial.cpp, tag winui3/release/1.4.2

#nullable enable

using System;
using System.Linq;
using System.Windows.Input;
using DirectUI;
using Uno.Disposables;
using Uno.Helpers;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static DirectUI.Pinvokes;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Uno.UI.FeatureConfiguration;

namespace Windows.UI.Xaml.Controls;

partial class ContentDialog
{
	private const ulong z_ulUniqueAsyncActionId = 1;

	private const double ContentDialog_SIP_Bottom_Margin = 12.0;

	// TODO:MZ: This should happen on Unloaded (and un-happen in Loaded)
	//	ContentDialog.~ContentDialog()
	//	{
	//VERIFYHR(DetachEventHandlers());
	//VERIFYHR(DetachEventHandlersForOpenDialog());

	//auto xamlRoot = XamlRoot::GetForElementStatic(this);
	//   if (m_xamlRootChangedEventHandler && xamlRoot)
	//   {
	//       VERIFYHR(m_xamlRootChangedEventHandler.DetachEventHandler(xamlRoot.Get()));
	//   }

	//   if (auto popup = m_tpPopup.GetSafeReference())
	//   {
	//       if (m_popupOpenedHandler)
	//       {
	//           VERIFYHR(m_popupOpenedHandler.DetachEventHandler(popup.Get()));
	//       }
	//   }
	//	}


	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// We only react to property changes if we have a complete
		// control template and a ContentDialog is currently visible or in design mode
		// Else what's the point?
		if (m_templateVersion == TemplateVersion.Unsupported || // Template is not complete
			(m_tpCurrentAsyncOperation is null && !DesignerInterop.GetDesignerMode(DesignerMode.V2Only)) || //&& ) || // or no active ShowAsync operation and not under the designer
			!m_hasPreparedContent || // or content hasn't been prepared
			m_hideInProgress)
		{
			return;
		}

		if (args.Property == FullSizeDesiredProperty)
		{
			UpdateVisualState();


		}
		else if (args.Property == PrimaryButtonTextProperty)
		{
			if (m_templateVersion < TemplateVersion.Redstone2)
			{
				var newString = (string)args.NewValue;
				var oldString = (string)args.OldValue;

				if (oldString == null || newString == null)
				{
					ResetAndPrepareContent();
				}
				else
				{
					var primaryButton = GetButtonHelper(ContentDialogButton.Primary);
					MUX_ASSERT(primaryButton is not null);

					var spContentControl = primaryButton;
					spContentControl!.Content = args.NewValue;
				}
			}
			else
			{
				UpdateVisualState();
			}
		}
		else if (args.Property == SecondaryButtonTextProperty)
		{
			if (m_templateVersion < TemplateVersion.Redstone2)
			{
				var newString = (string)args.NewValue;
				var oldString = (string)args.OldValue;

				// Going to or from a null value (blank string) causes a change in the content
				// of the button so we rebuild.

				if (oldString == null || newString == null)
				{
					ResetAndPrepareContent();
				}
				else
				{
					var secondaryButton = GetButtonHelper(ContentDialogButton.Secondary);
					MUX_ASSERT(secondaryButton is not null);

					var spContentControl = secondaryButton;
					spContentControl!.Content = args.NewValue;
				}
			}
			else
			{
				UpdateVisualState();
			}
		}
		else if (
			args.Property == CloseButtonTextProperty ||
			args.Property == DefaultButtonProperty)
		{
			UpdateVisualState();
		}
		else if (
			args.Property == IsPrimaryButtonEnabledProperty ||
			args.Property == IsSecondaryButtonEnabledProperty)
		{
			if (m_templateVersion < TemplateVersion.Redstone2)
			{
				bool isPrimary = args.Property == IsPrimaryButtonEnabledProperty;

				var button = GetButtonHelper(isPrimary ? ContentDialogButton.Primary : ContentDialogButton.Secondary);
				if (button is not null)
				{
					button.IsEnabled = (bool)args.NewValue;
				}
			}
		}
		else if (args.Property == PrimaryButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Primary, oldCommand);
		}
		else if (args.Property == SecondaryButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Secondary, oldCommand);
		}
		else if (args.Property == CloseButtonCommandProperty)
		{
			var oldCommand = args.OldValue as ICommand;
			SetButtonPropertiesFromCommand(ContentDialogButton.Close, oldCommand);
		}
		else if (
			args.Property == TitleProperty ||
			args.Property == TitleTemplateProperty)
		{
			UpdateTitleSpaceVisibility();
			SetPopupAutomationProperties();
		}
		else if (args.Property == FlowDirectionProperty)
		{
			if (m_placementMode != PlacementMode.InPlace)
			{
				SizeAndPositionContentInPopup();
			}
		}
		else if (args.Property == AutomationProperties.AutomationIdProperty)
		{
			SetPopupAutomationProperties();
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

		DetermineTemplateVersion();

		if (m_templateVersion != TemplateVersion.Unsupported)
		{
			if (DesignerInterop.GetDesignerMode(DesignerMode.V2Only))
			{
				// In designer mode the dialog is displayed in-place.  Do nothing special
				// to display the popup, but only worry about the contents
				m_isShowing = true;
				m_placementMode = PlacementMode.InPlace;

				PrepareContent();
			}
			else if (m_templateVersion < TemplateVersion.Redstone3 &&

				m_placementMode == PlacementMode.Undetermined || m_placementMode == PlacementMode.TransplantedRootInPopup)
			{
				m_placementMode = PlacementMode.TransplantedRootInPopup;

				// HostDialogWithinPopup is called here to catch the case when
				// this is part of the visual tree, at which point we
				// will create the Popup and pull the contents out of
				// the template.
				HostDialogWithinPopup(false /*wasSmokeLayerFoundAsTemplatePart*/);

				// At this point either a popup will have been created because
				// the call above and ContentDialog is in the visual tree, or a ShowAsync
				// call has happened and we're in LayoutMode_EntireControl mode.
				MUX_ASSERT(m_tpPopup is not null);
			}

			// For dialogs that were shown when not in the visual tree, since we couldn't prepare
			// their content during the ShowAsync() call, do it now that it's loaded.
			if (m_placementMode == PlacementMode.EntireControlInPopup)
			{
				PrepareContent();
			}

			// On non-PhoneBlue templates, it is possible to resize the app, in which case we would like to reposition the
			// ContentDialog. FullSize behavior also needs to be checked as the window height might become smaller than
			// the MaxHeight, in which case positioning behavior changes.
			var xamlRoot = XamlRoot.GetForElement(this);
			if (m_templateVersion > TemplateVersion.PhoneBlue && xamlRoot is not null)
			{
				ManagedWeakReference weakInstance = WeakReferencePool.RentWeakReference(this, this);

				void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
				{
					if (weakInstance.IsAlive && weakInstance.Target is ContentDialog contentDialog)
					{
						contentDialog.OnXamlRootChanged(sender, args);
					}
				}

				xamlRoot.Changed += OnXamlRootChanged;
				m_xamlRootChangedEventHandler.Disposable = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
			}

			m_tpLayoutRootPart.Loaded += OnLayoutRootLoaded;
			m_epLayoutRootLoadedHandler.Disposable = Disposable.Create(() => m_tpLayoutRootPart.Loaded -= OnLayoutRootLoaded);

			m_tpLayoutRootPart.KeyDown += OnLayoutRootKeyDown;
			m_epLayoutRootKeyDownHandler.Disposable = Disposable.Create(() => m_tpLayoutRootPart.KeyDown -= OnLayoutRootKeyDown);

			m_tpLayoutRootPart.KeyUp += OnLayoutRootKeyUp;
			m_epLayoutRootKeyUpHandler.Disposable = Disposable.Create(() => m_tpLayoutRootPart.KeyUp -= OnLayoutRootKeyUp);


			void gotFocusHandler(object sender, RoutedEventArgs args)
			{
				// Update which command button has the default button visualization.
				UpdateVisualState();
			}
			m_tpLayoutRootPart.GotFocus += gotFocusHandler;
			m_epLayoutRootGotFocusHandler.Disposable = Disposable.Create(() => m_tpLayoutRootPart.GotFocus -= gotFocusHandler);

			m_tpLayoutRootPart.ProcessKeyboardAccelerators += OnLayoutRootProcessKeyboardAccelerators;
			m_epLayoutRootProcessKeyboardAcceleratorsHandler.Disposable = Disposable.Create(() => m_tpLayoutRootPart.ProcessKeyboardAccelerators -= OnLayoutRootProcessKeyboardAccelerators);

			m_tpBackgroundElementPart.SizeChanged += OnDialogSizeChanged;
			m_dialogSizeChangedHandler.Disposable = Disposable.Create(() => m_tpBackgroundElementPart.SizeChanged -= OnDialogSizeChanged);
		}

		if (m_templateVersion >= TemplateVersion.Redstone2)
		{
			AttachButtonEvents();

			// In case the commands were set before the template was applied, we won't have responded to the property-change event.
			// We should set the button properties from the commands at this point to ensure they're set properly.
			// If they've already been set before, this will be a no-op, since we check to make sure that the properties
			// are unset before we set them.
			SetButtonPropertiesFromCommand(ContentDialogButton.Primary);
			SetButtonPropertiesFromCommand(ContentDialogButton.Secondary);
			SetButtonPropertiesFromCommand(ContentDialogButton.Close);
		}

		// If the template changes while the dialog is showing, we need to re-attached to the
		// dialog showing states changed event so that we can fire the hiding event at the
		// right time.
		if (m_placementMode == PlacementMode.InPlace && m_isShowing)
		{
			VisualStateGroup dialogShowingStates = GetTemplateChild<VisualStateGroup>("DialogShowingStates");
			if (dialogShowingStates is not null)
			{
				dialogShowingStates.CurrentStateChanged += OnDialogShowingStateChanged;
				m_dialogShowingStateChangedEventHandler.Disposable = Disposable.Create(() => dialogShowingStates.CurrentStateChanged -= OnDialogShowingStateChanged);
				m_tpDialogShowingStates = dialogShowingStates;
			}
		}

		// Lookup value of ContentDialogMinHeight, which is used when adjusting the dialog's layout
		// in response to a visible input pane.
		{
			ResourceDictionary resourceMap = Resources;
			var resourceKey = "ContentDialogMinHeight";
			bool hasKey = resourceMap.HasKey(resourceKey);

			if (hasKey)
			{
				object resource = resourceMap.Lookup(resourceKey);

				var doubleReference = resource as double?;
				m_dialogMinHeight = doubleReference ?? 0.0;
			}
		}

		// Attempt to set the m_tpSmokeLayer field as a FrameworkElement template part, in Popup placement.
		if (m_tpLayoutRootPart is not null)
		{
			ctl::ComPtr<IFrameworkElement> smokeLayerPartAsFE;

			IFC_RETURN(GetTemplatePart<IFrameworkElement>(STR_LEN_PAIR(L"SmokeLayerBackground"), smokeLayerPartAsFE.ReleaseAndGetAddressOf()));

			if (smokeLayerPartAsFE)
			{
				// Unparent smokeLayerPartAsFE in both Popup and InPlace placements. It can be reparented later in ContentDialog::PrepareSmokeLayer() in Popup placement.
				ctl::ComPtr<wfc::IVector<xaml::UIElement*>> layoutRootChildren;

				IFC_RETURN(m_tpLayoutRootPart.Cast<Grid>()->get_Children(&layoutRootChildren));

				UINT32 indexOfSmokeLayerPart = 0;
				BOOLEAN wasFound = FALSE;

				IFC_RETURN(layoutRootChildren->IndexOf(smokeLayerPartAsFE.AsOrNull<IUIElement>().Get(), &indexOfSmokeLayerPart, &wasFound));

				if (wasFound)
				{
					IFC_RETURN(layoutRootChildren->RemoveAt(indexOfSmokeLayerPart));

					if (m_placementMode == PlacementMode::TransplantedRootInPopup || m_placementMode == PlacementMode::EntireControlInPopup)
					{
						// When m_tpSmokeLayer was previously set by a PrepareSmokeLayer call while m_tpLayoutRootPart was still unknown,
						// HostDialogWithinPopup needs to be called again to set up m_tpSmokeLayer, m_tpSmokeLayerPopup and m_tpPopup.
						// For instance, m_tpSmokeLayer needs to be sized and parented to m_tpSmokeLayerPopup and m_tpPopup needs to
						// redefine its TransitionCollection. HostDialogWithinPopup will be called in the imminent OnDialogSizeChanged call
						// where reparenting is allowed again.
						m_prepareSmokeLayerAndPopup = m_tpSmokeLayer != nullptr;
						SetPtrValueWithQIOrNull(m_tpSmokeLayer, smokeLayerPartAsFE.Get());

						m_isSmokeLayerATemplatePart = true;
					}
				}
			}
		}

		UpdateVisualState();
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (m_isShowing)
		{
			return base.ArrangeOverride(finalSize);
		}
		else
		{
			return default;
		}
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);

		if (m_templateVersion == TemplateVersion.Unsupported)
		{
			return;
		}

		bool fullSizeDesired = FullSizeDesired;

		// Orientation
		if (m_templateVersion == TemplateVersion.PhoneBlue)
		{
			DisplayInformation spDisplayInformation = DisplayInformation.GetForCurrentView();

			var orientation = DisplayOrientations.None;
			orientation = spDisplayInformation.CurrentOrientation;

			// Note: When ContentDialog supports desktop windows, we may want to take the
			// width/height of the application window into account. For phone we only need
			// to consider device orientation.

			string? newStateName = null;
			switch (orientation)
			{
				case DisplayOrientations.Landscape:
				case DisplayOrientations.LandscapeFlipped:
					newStateName = "Landscape";
					break;
				case DisplayOrientations.Portrait:
				case DisplayOrientations.PortraitFlipped:
					newStateName = "Portrait";
					break;
				default:
					MUX_ASSERT(false);
					break;
			}

			GoToState(useTransitions, newStateName);
		}

		if (m_templateVersion >= TemplateVersion.Redstone2)
		{
			// ButtonsVisibilityStates
			{
				string primaryText = PrimaryButtonText;

				string secondaryText = SecondaryButtonText;

				string closeText = CloseButtonText;

				bool hasPrimary = !string.IsNullOrEmpty(primaryText);
				bool hasSecondary = !string.IsNullOrEmpty(secondaryText);
				bool hasClose = !string.IsNullOrEmpty(closeText);

				string buttonVisibilityState = "NoneVisible";
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
				string defaultButtonState = "NoDefaultButton";

				var defaultButton = DefaultButton;

				if (defaultButton != ContentDialogButton.None)
				{
					var focusedElement = this.GetFocusedElement();

					bool isFocusInCommandArea = m_tpCommandSpacePart.IsAncestorOf(focusedElement);

					// If focus is not in the command area, set the default button visualization just based on the property value.
					// If focus is in the command area, set the default button visualization only if it has focus.
					if (defaultButton == ContentDialogButton.Primary)
					{
						if (!isFocusInCommandArea || m_tpPrimaryButtonPart == focusedElement)
						{
							defaultButtonState = "PrimaryAsDefaultButton";
						}
					}
					else if (defaultButton == ContentDialogButton.Secondary)
					{
						if (!isFocusInCommandArea || m_tpSecondaryButtonPart == focusedElement)
						{
							defaultButtonState = "SecondaryAsDefaultButton";
						}
					}
					else if (defaultButton == ContentDialogButton.Close)
					{
						if (!isFocusInCommandArea || m_tpCloseButtonPart == focusedElement)
						{
							defaultButtonState = "CloseAsDefaultButton";
						}
					}
				}

				GoToState(useTransitions, defaultButtonState);
			}
		}

		if (m_templateVersion >= TemplateVersion.Redstone3)
		{
			// DialogShowingStates
			if (m_placementMode == PlacementMode.InPlace)
			{
				GoToState(true, m_isShowing && !m_hideInProgress ? "DialogShowing" : "DialogHidden");
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
		}

		// On PhoneBlue, the dialog did not move out of the way of the input pane.
		if (m_templateVersion > TemplateVersion.PhoneBlue)
		{
			AdjustVisualStateForInputPane();
		}
	}

	void DetermineTemplateVersion()
	{
		if (m_tpButton1HostPart is not null &&
			m_tpButton2HostPart is not null &&
			m_tpLayoutRootPart is not null &&
			m_tpContainerPart is not null &&
			m_tpContentPanelPart is not null &&
			m_tpContentPart is not null &&
			m_tpTitlePart is not null &&
			m_tpBackgroundElementPart is not null &&
			m_tpCommandSpacePart is null &&
			m_tpDialogSpacePart is null &&
			m_tpContentScrollViewerPart is null &&
			m_tpPrimaryButtonPart is null &&
			m_tpSecondaryButtonPart is null &&
			m_tpCloseButtonPart is null &&
			m_tpScaleTransformPart is null)
		{
			m_templateVersion = TemplateVersion.PhoneBlue;
		}
		else if (
			m_tpButton1HostPart is not null &&
			m_tpButton2HostPart is not null &&
			m_tpLayoutRootPart is not null &&
			m_tpContainerPart is not null &&
			m_tpContentPart is not null &&
			m_tpTitlePart is not null &&
			m_tpBackgroundElementPart is not null &&
			m_tpCommandSpacePart is not null &&
			m_tpDialogSpacePart is not null &&
			m_tpContentScrollViewerPart is not null &&
			m_tpPrimaryButtonPart is null &&
			m_tpSecondaryButtonPart is null &&
			m_tpCloseButtonPart is null &&
			m_tpScaleTransformPart is null)
		{
			m_templateVersion = TemplateVersion.Threshold;
		}
		else if (
			m_tpContainerPart is not null &&
			m_tpLayoutRootPart is not null &&
			m_tpBackgroundElementPart is not null &&
			m_tpContentScrollViewerPart is not null &&
			m_tpTitlePart is not null &&
			m_tpContentPart is not null &&
			m_tpCommandSpacePart is not null &&
			m_tpPrimaryButtonPart is not null &&
			m_tpSecondaryButtonPart is not null &&
			m_tpCloseButtonPart is not null &&
			m_tpButton1HostPart is null &&
			m_tpButton2HostPart is null &&
			m_tpScaleTransformPart is null)
		{
			m_templateVersion = TemplateVersion.Redstone2;
		}
		else if (
			m_tpContainerPart is not null &&
			m_tpLayoutRootPart is not null &&
			m_tpBackgroundElementPart is not null &&
			m_tpContentScrollViewerPart is not null &&
			m_tpTitlePart is not null &&
			m_tpContentPart is not null &&
			m_tpCommandSpacePart is not null &&
			m_tpPrimaryButtonPart is not null &&
			m_tpSecondaryButtonPart is not null &&
			m_tpCloseButtonPart is not null &&
			m_tpScaleTransformPart is not null &&
			m_tpButton1HostPart is null &&
			m_tpButton2HostPart is null)
		{
			m_templateVersion = TemplateVersion.Redstone3;
		}
	}

	public void Hide()
	{
		if (m_isShowing)
		{
			HideInternal(ContentDialogResult.None);
		}
	}

	private void HideInternal(ContentDialogResult result)
	{
		if (m_tpCurrentAsyncOperation is not null && !m_hideInProgress)
		{
			ContentDialogClosingEventArgs args;

			EnsureDeferralManagers();

			ulong deferralGeneration = 0;
			bool isAlreadyInUse = false;
			m_spClosingDeferralManager.Prepare(&deferralGeneration, &isAlreadyInUse);

			MUX_ASSERT(!isAlreadyInUse);

			m_hideInProgress = true;

			bool doFireClosing = ShouldFireClosing();
			if (doFireClosing)
			{
				m_spClosingDeferralManager = new DeferralFactoryManager<ContentDialogClosingDeferral>(deferralGeneration, &args);

				args.Result = result;

				ClosingEventSourceType* pEventSource = null;
				GetClosingEventSourceNoRef(&pEventSource);

				pEventSource.Raise(this, args);
			}

			ctl.WeakRefPtr wrThis;
			ctl.AsWeak(this, &wrThis);
			(m_spClosingDeferralManager.ContinueWith([wrThis, args, result]() mutable

			{
				var contentDialog = wrThis as IContentDialog > ();
				if (contentDialog)
				{
					bool isCanceled = false;
					if (args)
					{
						isCanceled = args.Cancel;
					}

					contentDialog.HideAfterDeferralWorker(!!isCanceled, result);
				}
				return S_OK;
			}));
		}
	}

	private void HideAfterDeferralWorker(bool isCanceled, ContentDialogResult result)
	{
		bool isPopupOpen = false;
		if (m_placementMode != PlacementMode.InPlace)
		{
			isPopupOpen = m_tpPopup.IsOpen;
		}

		// For the popup hosted scenarios, cancel hiding the dialog only if the popup is still open.  It
		// might not be open if an app manually closed the popup after searching for it in the
		// the visual tree.
		// For the inline scenarios, since there's no popup, always respect the cancel flag.
		if (isCanceled && (isPopupOpen || m_placementMode == PlacementMode.InPlace))
		{
			m_hideInProgress = false;
		}
		else
		{
			ContentDialogMetadata metadata;
			if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
			{
				metadata = xamlRoot.GetContentDialogMetadata();
			}

#if DBG
	        bool isOpen = false;
	        metadata.IsOpen(this, &isOpen);
	        MUX_ASSERT(isOpen);
#endif

			var asyncOperationNoRef = (ContentDialogShowAsyncOperation*)(m_tpCurrentAsyncOperation);
			asyncOperationNoRef.SetResults(result);

			// Try to restore focus back to the original focused element. It is important to
			// do this before the Popup is closed and destroyed, because otherwise, the FocusManager
			// would try to set focus to the first focusable element of the page on Desktop, before
			// getting to this point.
			if (m_spFocusedElementBeforeContentDialogShows?.Target is DependencyObject dependencyObject)
			{
				this.SetFocusedElement(
					dependencyObject,
					FocusState.Programmatic,
					false /*animateIfBringIntoView*/);
			}

			if (m_placementMode == PlacementMode.InPlace)
			{
				UpdateVisualState();
			}
			else
			{

				FrameworkElement popupChild = m_placementMode == PlacementMode.EntireControlInPopup ? this : m_tpLayoutRootPart;
				popupChild.Unloaded += OnPopupChildUnloaded;
				m_popupChildUnloadedEventHandler.Disposable = Disposable.Create(() => popupChild.Unloaded -= OnPopupChildUnloaded);

				// Make these elements non-interactable while the closing transitions plays.
				m_tpSmokeLayer.IsHitTestVisible = false;

				if (m_tpLayoutRootPart is not null)
				{
					m_tpLayoutRootPart.IsHitTestVisible = false;
				}

				m_tpPopup.IsOpen = false;
			}

			metadata.RemoveOpenDialog(this);

			BackButtonIntegration_UnregisterListener(this);

			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Hide, this);
		}

		// For the popup hosted scenario, the OnFinishedClosing() method is usually called in the
		// popup's closed event handler, but only if the popup was closed as a result of a hide
		// action.  If the popup was closed via another means (such as the app finding the popup
		// in the tree and closing it manually), then catch that case and call OnFinishedClosing()
		// here.
		if (m_placementMode != PlacementMode.InPlace && !isPopupOpen)
		{
			OnFinishedClosing();
		}
	}

	public IAsyncOperation<ContentDialogResult> ShowAsync() =>
		ShowAsyncWithPlacement(ContentDialogPlacement.Popup);

	public IAsyncOperation<ContentDialogResult> ShowAsyncWithPlacement(ContentDialogPlacement placement)
	{
		using var cleanupOnFailure = Disposable.Create(() =>
		{
			m_placementMode = PlacementMode.Undetermined;
			m_tpPopup = null;
			m_tpCurrentAsyncOperation = null;
		});


		bool ifWasWindowed = m_isWindowed;

		m_isWindowed = placement == ContentDialogPlacement.UnconstrainedPopup ? true : false;

		if (m_tpPopup && ifWasWindowed != m_isWindowed)
		{
			IFC_RETURN(DiscardPopup());
		}

		// Validate we have a unique, non-ambiguous visualtree on which to place the ContentDialog
		{
			VisualTree uniqueVisualTreeUnused = VisualTree.GetUniqueVisualTreeNoRef(this, null /*positionReferenceElement*/, null);
		}

		// reset previous focused element
		m_spFocusedElementBeforeContentDialogShows = null;

		// If the control is in the visual tree then we transplant the LayoutRoot, otherwise
		// we place the entire control in the popup.
		if (m_placementMode == PlacementMode.Undetermined)
		{
			m_placementMode = PlacementMode.EntireControlInPopup;

			if (IsInLiveTree)
			{
				// Make sure the template has been applied so that m_templateVersion is accurate.
				ApplyTemplate();

				m_placementMode =
					(placement == ContentDialogPlacement.InPlace && m_templateVersion >= TemplateVersion.Redstone3 ?
						PlacementMode.InPlace : PlacementMode.TransplantedRootInPopup);
			}
		}

		ContentDialogMetadata metadata;
		if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
		{
			metadata = xamlRoot.GetContentDialogMetadata();
		}
		else
		{
			throw new InvalidOperationException("XamlRoot was not set");
		}

		// See if there is already an open dialog and return an error if that is the case.
		// For InPlace dialogs, multiple can be shown at the same time, provided that they
		// are each under different parents.
		{
			DependencyObject? parent = null;
			if (m_placementMode == PlacementMode.InPlace)
			{
				parent = Parent;

				// A dialog can only be shown InPlace if it is in the live tree, so it should have a parent.
				MUX_ASSERT(parent);
			}

			bool hasOpenDialog = metadata.HasOpenDialog(parent);

			if (m_tpCurrentAsyncOperation is not null || hasOpenDialog)
			{
				ErrorHelper.OriginateErrorUsingResourceID(E_ASYNC_OPERATION_NOT_STARTED, ERROR_CONTENTDIALOG_MULTIPLE_OPEN);
			}
		}

		Microsoft.WRL.ComPtr<ContentDialogShowAsyncOperation> newAsyncOperation;
		(Microsoft.WRL.MakeAndInitialize<ContentDialogShowAsyncOperation>(
			&newAsyncOperation,

			.InterlockedIncrement(&ContentDialog.z_ulUniqueAsyncActionId),
			null));

		newAsyncOperation.StartOperation(this);

		// Set the default result for light-dismiss triggered scenarios.
		newAsyncOperation.SetResults(ContentDialogResult.None);
		m_tpCurrentAsyncOperation = newAsyncOperation;

		m_isShowing = true;
		m_hideInProgress = false;
		m_skipClosingEventOnHide = false;
		m_hasPreparedContent = false;

		if (m_placementMode == PlacementMode.InPlace)
		{
			// Support for inline dialogs depends on visual states added in RS3.
			MUX_ASSERT(m_templateVersion >= TemplateVersion.Redstone3);

			// If the dialog had previously been open, then this should have been cleared after
			// it finished closing.
			MUX_ASSERT(m_dialogShowingStateChangedEventHandler.Disposable is null);

			// This is done here rather than in OnApplyTemplate to avoid some startup perf impact because
			// querying for this part will fault the VSM.
			VisualStateGroup? dialogShowingStates = GetTemplateChild<VisualStateGroup>("DialogShowingStates");
			if (dialogShowingStates is not null)
			{
				m_dialogShowingStateChangedEventHandler.AttachEventHandler(dialogShowingStates, std.bind(&ContentDialog.OnDialogShowingStateChanged, this, _1, _2));
				m_tpDialogShowingStates = dialogShowingStates;
			}

			PrepareContent();
		}
		else
		{
			// This will be set to false once the popup has opened.  If an app calls
			// hide before the popup has opened, we don't fire the closing event.
			m_skipClosingEventOnHide = true;

			// For dialogs that haven't been loaded yet (ones that are being opened in the popup
			// in their entirety), wait until they are loaded before preparing their content.
			// Template version is determined in OnApplyTemplate, so we can test against that
			// to see whether the control has been loaded before ShowAsync() was called.
			if (m_templateVersion != TemplateVersion.Unsupported)
			{
				PrepareContent();
			}

			// Make sure the template has been applied so that m_tpSmokeLayer can potentially be found in the control template.
			ApplyTemplate();

			HostDialogWithinPopup(false /*wasSmokeLayerFoundAsTemplatePart*/);


			// Defer actually opening the ContentDialog's popup by a tick to enable
			// transitions to play.  They don't play if they target an element that
			// has been removed from and re-inserted back into the tree in the same tick,
			// which is the case for the LayoutRoot part when m_placementMode == TransplantedRootInPopup
			// or for the whole control when m_placementMode == EntireControlInPopup.
			(DXamlCore.Current.GetXamlDispatcherNoRef().RunAsync(
				MakeCallback(ContentDialog(this), &ContentDialog.DeferredOpenPopup)));
		}

		if (DXamlCore.Current.GetHandle().BackButtonSupported())
		{
			BackButtonIntegration_RegisterListener(this);
		}

		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Show, this);

		metadata.AddOpenDialog(this);

		returnValue = newAsyncOperation;

		// Everything completed successfully, so no need to let our scope-exit cleanup object execute.
		cleanupOnFailure.release();

		return returnValue;
	}

	private void OnCommandButtonClicked(
		TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventSource,
		ICommand command,
		object commandParameter,
		ContentDialogResult result)
	{
		MUX_ASSERT(m_tpCurrentAsyncOperation is not null);

		if (!m_hideInProgress)
		{
			EnsureDeferralManagers();

			ulong deferralGeneration = 0;
			bool isAlreadyInUse = false;
			m_spButtonClickDeferralManager.Prepare(out deferralGeneration, out isAlreadyInUse);
			if (isAlreadyInUse)
			{
				return;
			}

			ContentDialogButtonClickEventArgs args = new ContentDialogButtonClickEventArgs(m_spButtonClickDeferralManager, deferralGeneration);

			clickEventSource?.Invoke(this, args);

			ManagedWeakReference wrThis = WeakReferencePool.RentSelfWeakReference(this);

			m_spButtonClickDeferralManager.ContinueWith((wrThis, args, command, commandParameter, result) =>
			{

				if (wrThis.Target is ContentDialog contentDialog)
				{
					bool isCanceled = false;
					isCanceled = args.Cancel;
					if (!isCanceled)
					{
						if (command is not null)
						{
							// SYNC_CALL_TO_APP DIRECT - This next line may directly call out to app code.
							command.Execute(commandParameter);
						}

						contentDialog.HideInternal(result);
					}
				}
			});
		}
	}

	private void EnsureDeferralManagers()
	{
		if (m_spClosingDeferralManager is null)
		{
			DeferralManager<ContentDialogClosingDeferral> spClosingDeferralManager = new();
			m_spClosingDeferralManager = spClosingDeferralManager;
		}

		if (m_spButtonClickDeferralManager is null)
		{
			DeferralManager<ContentDialogButtonClickDeferral> spButtonClickDeferralManager = new();
			m_spButtonClickDeferralManager = spButtonClickDeferralManager;
		}
	}

	private void ResetAndPrepareContent()
	{
		if (m_templateVersion != TemplateVersion.Unsupported &&
			m_tpCurrentAsyncOperation is not null &&
			m_hasPreparedContent)
		{
			m_hasPreparedContent = false;
			PrepareContent();
		}
	}

	// wasSmokeLayerFoundAsTemplatePart is set to True when this method is called after m_tpSmokeLayer was found as a template part and m_isSmokeLayerATemplatePart was set to True.
	private void HostDialogWithinPopup(bool wasSmokeLayerFoundAsTemplatePart)
	{
		MUX_ASSERT(m_placementMode == PlacementMode.TransplantedRootInPopup || m_placementMode == PlacementMode.EntireControlInPopup);

		PrepareSmokeLayer();
		PreparePopup(wasSmokeLayerFoundAsTemplatePart);
		m_tpPopup.ShouldConstrainToRootBounds = !m_isWindowed;

		if (!wasSmokeLayerFoundAsTemplatePart)
		{
			// Make sure the automation name is in-sync with the title.
			SetPopupAutomationProperties();

			if (m_placementMode == PlacementMode.TransplantedRootInPopup)
			{
				if (!m_isLayoutRootTransplanted)
				{
					// We verify some parts of the template here and simply
					// no-op if it looks too odd. (if child isn't the layoutroot)
					UIElement spChildElt = m_tpContainerPart.Child;
					if (spChildElt == m_tpLayoutRootPart)
					{
						m_tpContainerPart.Child = null;

						// We place our popup in the children collection so it can receive
						// all the text property inheritance goodness from ContentControl.
						m_tpContainerPart.Child = m_tpPopup;
					}

					// Even if we have an incorrect template set this value
					// to preserve the logical tests other parts of ContentDialog
					// perform.
					m_isLayoutRootTransplanted = true;
				}
			}
		}

		m_tpPopup.Child = m_placementMode == PlacementMode.EntireControlInPopup ? this : m_tpLayoutRootPart;
	}

	private void PreparePopup(bool wasSmokeLayerFoundAsTemplatePart)
	{
		MUX_ASSERT(m_tpSmokeLayer is not null);

		if (m_tpPopup is null || wasSmokeLayerFoundAsTemplatePart)
		{
			Popup popup;
			if (wasSmokeLayerFoundAsTemplatePart)
			{
				popup = m_tpPopup;
			}
			else
			{
				popup = new();

				popup.IsContentDialog = true;
				popup.Opened += OnPopupOpened;
				m_popupOpenedHandler.Disposable = Disposable.Create(() => popup.Opened -= OnPopupOpened);
			}

			MUX_ASSERT(m_tpSmokeLayer is not null);

			// Setup the open / close transitions
			TransitionCollection transitionCollection = new();

			ContentDialogOpenCloseThemeTransition contentDialogOpenCloseTransition = new();

			contentDialogOpenCloseTransition.SetSmokeLayer(m_tpSmokeLayer);

			Transition openCloseTransition = contentDialogOpenCloseTransition;
			transitionCollection.Add(openCloseTransition);

			popup.ChildTransitions = transitionCollection;

			if (!wasSmokeLayerFoundAsTemplatePart)
			{
				// Set IsDialog property to True for popup
				AutomationProperties.SetIsDialog(popup, true);

				m_tpPopup = popup;
			}
		}
	}

	private void PrepareSmokeLayer()
	{
		// Prepare a second popup to show a semi-transparent
		// smoke layer behind the dialog.
		if (!m_tpSmokeLayerPopup)
		{
			Popup popupForOverlay;
			popupForOverlay = new();
			SetPtrValueWithQI(m_tpSmokeLayerPopup, popupForOverlay);
		}

		if (m_tpSmokeLayer is null || m_isSmokeLayerATemplatePart)
		{
			if (m_tpSmokeLayer is null)
			{
				Rectangle overlay = new();
				m_tpSmokeLayer = overlay;
			}

			Rect windowBounds = DXamlCore.Current.GetContentBoundsForElement(this);

			m_tpSmokeLayer.Width = windowBounds.Width;
			m_tpSmokeLayer.Height = windowBounds.Height;

			m_tpSmokeLayerPopup.Child = m_tpSmokeLayer;
		}

		if (!m_isSmokeLayerATemplatePart)
		{
			Brush brush = CreateSmokeLayerBrush();
			m_tpSmokeLayer.Fill = brush;
		}
	}

	// only in Desktop Window app when custom titlebar is being used
	// disable custom titlebar temporarily when smoke layer is displayed
	private void UpdateCanDragStatusWindowChrome(bool dragEnabled)
	{
		// check if window chrome exists or not
		// if window chrome exists then this app is running in island or desktop window mode
		var xamlRoot = XamlRoot.GetForElement(this);
		if (xamlRoot is not null)
		{
			UIElement publicRootVisual = xamlRoot.VisualTree.PublicRootVisual;
			ctl::ComPtr<DependencyObject> peer;
			IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(publicRootVisual, &peer));
			var windowChrome = peer as WindowChrome; // TODO: We don't have WindowChrome always

			if (windowChrome && windowChrome->IsChromeActive())
			{
				windowChrome->UpdateCanDragStatus(dragEnabled);
			}
		}
	}

	private Brush? CreateSmokeLayerBrush()
	{
		MUX_ASSERT(!m_isSmokeLayerATemplatePart);

		Brush? brush = null;

		ResourceDictionary resourceDictionary = Resources;

		// Querying this from code-behind does not allow it to change with theme changes (light/dark/HC).
		// We also can't just refresh the brush whenever it opens because resource lookups from
		// code-behind don't take into account the current theme of the control; they instead use
		// the theme dictionary that corresponds to the app's theme.
		// The correct solution for this is to specify the overlay as a template part and use
		// a {ThemeResource} markup extension. That solution has been implemented in a more recent release.
		// See the use of m_isSmokeLayerATemplatePart for details.
		string themeBrushName = "SystemControlPageBackgroundMediumAltMediumBrush";

		var hasKey = resourceDictionary.HasKey(themeBrushName);

		if (hasKey)
		{
			object resource = resourceDictionary.Lookup(themeBrushName);
			brush = resource as Brush;
		}

		return brush;
	}

	private void DeferredOpenPopup()
	{
		// Since opening the popups is deferred by a tick to allow layout transitions to play
		// correctly, we could hit a situation where a dialog is shown and then immediately
		// hidden within the same tick.  In this scenario, we don't want to open the popups
		// after the dialog has been hidden so make sure the dialog is still showing before
		// actually opening them.
		if (m_isShowing)
		{
			var tree = VisualTree.GetUniqueVisualTreeNoRef(this, null /*positionReferenceElement*/, null, &tree);

			if (m_tpSmokeLayerPopup is not null)
			{
				(CPopup*)(m_tpSmokeLayerPopup.Cast<DirectUI.Popup>().GetHandle()).SetAssociatedVisualTree(tree);
				m_tpSmokeLayerPopup.IsOpen = true;
				UpdateCanDragStatusWindowChrome(false); //disable dragging in custom titlebar
			}

			(CPopup*)(m_tpPopup.Cast<DirectUI.Popup>().GetHandle()).SetAssociatedVisualTree(tree);
			m_tpPopup.IsOpen = true;
		}
	}

	private void PrepareContent()
	{
		MUX_ASSERT(m_templateVersion != TemplateVersion.Unsupported);

		if (!m_hasPreparedContent)
		{
			if (m_placementMode == PlacementMode.TransplantedRootInPopup)
			{
				// We set the width/height of the container to 0 to ensure that the
				// inverse transform we apply to Popup behaves correctly when in RTL mode,
				// otherwise it would transform from the top right visual point and incorrectly
				// position the popup.
				m_tpContainerPart.Width = 0.0;
				m_tpContainerPart.Height = 0.0;
			}
			else if (m_placementMode == PlacementMode.InPlace && m_isLayoutRootTransplanted)
			{
				// If a dialog that had previously been shown in a popup is now being shown in place,
				// restore the layout root that had been transplanted out of the dialog's tree.
				m_tpContainerPart.Child = m_tpLayoutRootPart;
				m_tpContainerPart.Width = DoubleUtil.NaN;
				m_tpContainerPart.Height = DoubleUtil.NaN;

				m_isLayoutRootTransplanted = false;
			}

			// For Pre-Redstone2 templates, we need to build the buttons in code
			// behind because they don't exist as template parts.
			if (m_templateVersion < TemplateVersion.Redstone2)
			{
				BuildAndConfigureButtons();
				AttachButtonEvents();
			}

			UpdateVisualState();
			UpdateTitleSpaceVisibility();

			if (m_placementMode != PlacementMode.InPlace)
			{
				SizeAndPositionContentInPopup();
			}

			// Cast a shadow
			if (ThemeShadow.IsDropShadowMode)
			{
				// Under drop shadows, ContentDialog has a larger shadow than normal
				ApplyElevationEffect(m_tpBackgroundElementPart, 0 /* depth */, 128 /* baseElevation */);
			}
			else
			{
				ApplyElevationEffect(m_tpBackgroundElementPart);
			}

			m_hasPreparedContent = true;
		}
	}

	private void SizeAndPositionContentInPopup()
	{
		if (m_tpPopup is null || m_templateVersion == TemplateVersion.Unsupported)
		{
			return;
		}

		double xOffset = 0;
		double yOffset = 0;

		Rect windowBounds = DXamlCore.Current.GetContentBoundsForElement(this);

		// Get the layout bounds to calculate the ContentDialog position and size
		Rect adjustedLayoutBounds = DXamlCore.Current.GetContentLayoutBoundsForElement(this);

		// If smoke layer has a Y offset (due to being shifted down for making space for window chrome caption buttons)
		// add that to calculations in centering the popup
		Thickness margin = m_tpSmokeLayer.Margin;
		double smokeLayerYOffset = margin.Top;

		double dialogMaxHeight = MaxHeight;

		double dialogMaxWidth = MaxWidth;

		var flowDirection = FlowDirection;

		bool fullSizeDesired = FullSizeDesired;

		FrameworkElement spBackgroundAsFE = m_tpBackgroundElementPart;

		double popupWidth = spBackgroundAsFE.ActualWidth;

		double popupHeight = spBackgroundAsFE.ActualHeight;

		if (m_templateVersion < TemplateVersion.Redstone3 &&
			m_templateVersion > TemplateVersion.PhoneBlue &&
			m_layoutAdjustmentsForInputPaneStoryboard is null)
		{
			spBackgroundAsFE.VerticalAlignment = fullSizeDesired ?
				VerticalAlignment.Stretch :
				VerticalAlignment.Top;
		}

		if (m_templateVersion == TemplateVersion.PhoneBlue)
		{
			var spRowDefs = m_tpLayoutRootPart.RowDefinitions;

			var spContentRow = spRowDefs[0];

			// Collapse and expand parts of the grid
			// depending on mode.
			if (fullSizeDesired)
			{
				GridLength starGridLength = new GridLength(1.0, GridUnitType.Star);
				spContentRow.Height = starGridLength;
			}
			else
			{
				GridLength autoGridLength = new GridLength(1.0, GridUnitType.Auto);
				spContentRow.Height = autoGridLength;
			}
		}

		// Here we shamelessly contour ourselves to the Layout system and the whims of
		// the implementers of Popup. The following truths must be known to understand
		// this code:
		// - ContentDialog has two modes, LayouRoot and EntireControl. When it is in the visual
		//   tree it cannot add itself to the popup, it's already the child of another UI element.
		//   As a result we transplant a specific part of the control template (LayoutRoot) to the
		//   popup.
		//   When it is not in the visual tree there's no way to apply the control template. We add
		//   the entire control to the visual tree in this scenario.
		// - Popup is added to ContentDialog's children when ContentDialog is in LayoutRoot mode to
		//   allow inherited properties to work.
		// - When Popup has no parent it positions its contents in the top left of the screen. When it
		//   has a parent it positions its contents at the top left of its parent. As a result when we are
		//   in LayoutRoot mode we must apply an inverse transform of the top left of the Popup's parent's
		//   position.
		// - When a Popup has no parent it does not flip in place for RTL mode. As a result we have to apply
		//   a horizontal offset equal to the width of the popup (in our case the screen width) to the Popup
		//   when in EntireControl mode.
		// - When a Popup has a parent it determines it position using the top left corner even in RTL mode.
		//   as a result we make sure the container is sized to zero pixels.
		// - When a parented Popup is in RTL mode its position is determined by its right edge and not the left edge.
		// - The Arrange pass is busted for RTL mode in Popup so don't expect any autosizing
		//   to work correctly. We force feed Popup its Width and Height instead of using Stretch.
		if (m_templateVersion >= TemplateVersion.Redstone3 && m_placementMode == PlacementMode.EntireControlInPopup)
		{
			Height = windowBounds.Height;
			Width = windowBounds.Width;
		}
		else if (m_tpLayoutRootPart is not null)
		{
			m_tpLayoutRootPart.Height = windowBounds.Height - smokeLayerYOffset;
			m_tpLayoutRootPart.Width = windowBounds.Width;
		}

		if (m_templateVersion > TemplateVersion.PhoneBlue)
		{
			// Apply the available width/height with the layout width/height
			double availableWidth = adjustedLayoutBounds.Width;
			double availableHeight = adjustedLayoutBounds.Height;

			if (m_templateVersion >= TemplateVersion.Redstone3)
			{
				xOffset = (flowDirection == FlowDirection.LeftToRight ? 0 : availableWidth);

				// Set inner margin based on display regions
				Thickness layoutInnerMargin = GetDialogInnerMargin(adjustedLayoutBounds);
				var layoutRoot = m_tpLayoutRootPart;
				if (layoutRoot is not null)
				{
					layoutRoot.Padding = layoutInnerMargin;
				}
			}
			else
			{
				// Center the popup horizontally and vertically
				double dialogMinHeight = 0;
				double commandSpaceHeight = 0;
				double nonContentSpaceHeight = 0;
				double pageTop = 0;
				Thickness borderThickness = default;
				Thickness contentScrollViewerMargin = default;
				Thickness dialogSpacePadding = default;

				FrameworkElement spContentScrollViewerAsFE;
				FrameworkElement spCommandSpaceAsFE;

				borderThickness = m_tpBackgroundElementPart.BorderThickness;
				spCommandSpaceAsFE = m_tpCommandSpacePart;
				spContentScrollViewerAsFE = m_tpContentScrollViewerPart;

				if (m_templateVersion == TemplateVersion.Redstone2)
				{
					// In RS2, we set MinHeight on the background template part
					// instead of the ContentDialog itself.
					dialogMinHeight = spBackgroundAsFE.MinHeight;
				}
				else
				{
					dialogMinHeight = MinHeight;
				}

				// Initialize the ContentDialog horizontal and vertical position.
				// The window and layout bounds base on the screen coordinate which is the same
				// on the desktop, but phone can be different because the layout bound left/top
				// is applying the system tray's width/height in case of opaque status.
				xOffset = Math.Max(0.0f, adjustedLayoutBounds.X - windowBounds.X);
				yOffset = Math.Max(0.0f, adjustedLayoutBounds.Y - windowBounds.Y);

				// Set the page top position that excludes the system tray
				pageTop = yOffset;

				// Set the ContentDialog horizontal position at the center from the available width
				xOffset +=
					flowDirection == FlowDirection.LeftToRight ?
					(float)((availableWidth - popupWidth) / 2) :
					adjustedLayoutBounds.Width - (float)((availableWidth - popupWidth) / 2);

				if (popupWidth > availableWidth)
				{
					spBackgroundAsFE.Width = availableWidth;
					popupWidth = availableWidth;
				}

				// Limit the scroll viewer max height in order to prevent it being on top of CommandSpace.
				commandSpaceHeight = spCommandSpaceAsFE.ActualHeight;

				contentScrollViewerMargin = spContentScrollViewerAsFE.Margin;

				dialogSpacePadding = m_tpDialogSpacePart.Padding;

				// Calculate the height that is excluded from the content space height.
				nonContentSpaceHeight =
					commandSpaceHeight +
					borderThickness.Top + borderThickness.Bottom +
					contentScrollViewerMargin.Top + contentScrollViewerMargin.Bottom +
					dialogSpacePadding.Top + dialogSpacePadding.Bottom;

				if (fullSizeDesired)
				{
					spContentScrollViewerAsFE.Height = Math.Max(0.0, Math.Min(dialogMaxHeight, (double)(availableHeight) - nonContentSpaceHeight));
				}
				else
				{
					spContentScrollViewerAsFE.MaxHeight = Math.Max(0.0, Math.Min(dialogMaxHeight, (double)(availableHeight) - nonContentSpaceHeight));
				}

				// Align the dialog to the center.
				yOffset += (float)((availableHeight - popupHeight) / 2);
			}
		}

		// When the ContentDialog is in the visual tree, the popup offset has added
		// to it the top-left point of where layout measured and arranged it to.
		// Since we want ContentDialog to be an overlay, we need to subtract off that
		// point in order to ensure the ContentDialog is always being displayed in
		// window coordinates instead of local coordinates.
		if (m_placementMode == PlacementMode.TransplantedRootInPopup)
		{
			GeneralTransform transformToRoot = m_tpPopup.TransformToVisual(null);
			var offsetFromRoot = transformToRoot.TransformPoint(new Point(0, 0));

			if (m_templateVersion == TemplateVersion.PhoneBlue && flowDirection == FlowDirection.RightToLeft)
			{
				xOffset -= windowBounds.Width - offsetFromRoot.X;
			}
			else
			{
				xOffset =
					flowDirection == FlowDirection.LeftToRight ?
					(xOffset - offsetFromRoot.X) :
					(xOffset - offsetFromRoot.X) * -1;
			}

			yOffset = yOffset - offsetFromRoot.Y + smokeLayerYOffset;
		}
		// The V1 template, and only the V1 template, requires us to
		// add on the window bounds to position things correctly in RTL.
		else if (m_templateVersion == TemplateVersion.PhoneBlue && flowDirection == FlowDirection.RightToLeft)
		{
			xOffset += windowBounds.Width;
		}

		// Set the ContentDialog left and top position.
		m_tpPopup.HorizontalOffset = xOffset;
		m_tpPopup.VerticalOffset = yOffset;
	}

	private Thickness GetDialogInnerMargin(Rect adjustedLayoutBounds)
	{
		var innerMargin = new Thickness(0, 0, 0, 0);
		int regionCount = 0;

		if (m_simulateRegions)
		{
			regionCount = 2;
		}
		else
		{
			// Find out if the API is available (currently behind a velocity key)
			var isPresent = ApiInformation.IsMethodPresent("Windows.UI.ViewManagement.ApplicationView", "GetDisplayRegions");

			if (isPresent)
			{
				// Get regions for current view
				// Get Display Regions doesn't work on Win32 Apps, because there is no
				// application view.
				if (ApplicationView.GetForCurrentView() is { } applicationView)
				{
					try
					{
						var regions = applicationView.GetDisplayRegions();
						regionCount = regions.Count;
					}
					catch
					{
						// WinUI bug: APIs currently return a failure when there is only one display region.
						return innerMargin;
					}
				}
			}
		}

		if (regionCount == 2)
		{
			// Get the position of the focused element to determine which region it's in
			Point focusedPosition = GetFocusedElementPosition();

			if (adjustedLayoutBounds.Width > adjustedLayoutBounds.Height)
			{
				// Regions are split left/right
				var offsetWidth = adjustedLayoutBounds.Width / 2;

				if (focusedPosition.X < offsetWidth)
				{
					// Dialog should be positioned on the left
					innerMargin.Right = offsetWidth;
				}
				else
				{
					// Dialog should be positioned on the right
					innerMargin.Left = offsetWidth;
				}
			}
			else
			{
				// Regions are split top/bottom
				var offsetHeight = adjustedLayoutBounds.Height / 2;

				if (focusedPosition.Y < offsetHeight)
				{
					// Dialog should be positioned at the top
					innerMargin.Bottom = offsetHeight;
				}
				else
				{
					// Dialog should be positioned on the bottom
					innerMargin.Top = offsetHeight;
				}
			}
		}

		return innerMargin;
	}

	private Point GetFocusedElementPosition()
	{
		Point focusedPosition = default;

		UIElement? focusedElement = null;

		if (m_spFocusedElementBeforeContentDialogShows.Target is DependencyObject focusedElementAsDO)
		{
			focusedElement = focusedElementAsDO as UIElement;

			if (focusedElement is null)
			{
				DependencyObject focusedDO = focusedElementAsDO;
				TextElement focusedTextElement = focusedDO as TextElement;

				if (focusedTextElement is not null)
				{
					FrameworkElement containingFE = focusedTextElement.GetContainingFrameworkElement();

					if (containingFE is not null)
					{
						DependencyObject containingDO = DXamlCore.Current.GetPeer(containingFE);
						focusedElement = containingDO as UIElement;
					}
				}
			}
		}

		if (focusedElement is not null)
		{
			GeneralTransform transform = focusedElement.TransformToVisual(null);

			focusedPosition = transform.TransformPoint(default);
		}

		return focusedPosition;
	}

	private void OnPopupOpened(object sender, object args)
	{
		IAsyncInfo spAsyncInfo;
		AsyncStatus status = AsyncStatus.Started;

		m_tpCurrentAsyncOperation.As(&spAsyncInfo);
		status = spAsyncInfo.Status;
		if (status != AsyncStatus.Canceled)
		{
			UpdateVisualState();
			AttachEventHandlersForOpenDialog();

			// Now that the popup is opened, allow an app to cancel the hiding the dialog.
			m_skipClosingEventOnHide = false;

			ContentDialogOpenedEventArgs spArgs;
			spArgs = new();

			OpenedEventSourceType* pEventSource = null;
			GetOpenedEventSourceNoRef(&pEventSource);

			pEventSource.Raise(this, spArgs);
		}
	}

	private void OnPopupChildUnloaded(object sender, RoutedEventArgs args)
	{
		MUX_ASSERT(m_placementMode == PlacementMode.EntireControlInPopup || m_placementMode == PlacementMode.TransplantedRootInPopup);

		if (m_tpCurrentAsyncOperation is null)
		{
			// If m_tpCurrentAsyncOperation is null, then that means that we've already handled the popup closing.
			// This can happen in the circumstance where we close the popup in the handler for a button click -
			// for example, if we remove the ContentDialog from the visual tree in the handler by navigating
			// to another page, this will occur.
			return;
		}

		if (m_hideInProgress)
		{
			m_hideInProgress = false;
			OnFinishedClosing();
		}
		else
		{
			// If the app directly closed the popup, go through the closing actions for
			// ContentDialog.
			// This isn't great, as using the Cancel flag on the Closing event args will
			// have no effect.
			m_skipClosingEventOnHide = true;
			HideInternal(ContentDialogResult.None);
		}
	}

	private void OnBackButtonPressedImpl(out bool handled)
	{
		ExecuteCloseAction();

		handled = true;
	}

	private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
	{
		MUX_ASSERT(m_templateVersion == TemplateVersion.PhoneBlue);

		var state = args.WindowActivationState;

		if (state == WindowActivationState.Deactivated)
		{
			m_skipClosingEventOnHide = true;
			HideInternal(ContentDialogResult.None);
		}
	}

	private void OnFinishedClosing()
	{
		var asyncOperationNoRef = m_tpCurrentAsyncOperation; // TODO:MZ: .Cast<ContentDialogShowAsyncOperation>();

		m_isShowing = false;

		UpdateVisualState();

		if (m_placementMode != PlacementMode.InPlace)
		{
			// No longer need the handler so detach it.
			m_popupChildUnloadedEventHandler.Disposable = null; // TODO:MZ: DetachEventHandler(m_placementMode == PlacementMode.EntireControlInPopup ? this : m_tpLayoutRootPart as FrameworkElement > ());

			m_tpSmokeLayerPopup.IsOpen = false;
			UpdateCanDragStatusWindowChrome(true); //re-enable dragging in custom titlebar

			// Break circular reference with ContentDialog.
			m_tpPopup.Child = null;
		}

		DetachEventHandlersForOpenDialog();

		var result = asyncOperationNoRef.GetResults();

		RaiseClosedEvent(result);

		// We use a new deferral manager each time the ContentDialog is shown because the
		// dialog may be forcibly closed and then reshown while a button click deferral is pending.
		// In that case if the deferral is ever completed it no longer has anything to do, and until
		// it is completed its manager will not be able to start any new deferrals.
		m_spButtonClickDeferralManager.Disconnect();
		m_spButtonClickDeferralManager.Reset();

		if (m_dialogShowingStateChangedEventHandler.Disposable is not null)
		{
			MUX_ASSERT(m_placementMode == PlacementMode.InPlace);
			m_dialogShowingStateChangedEventHandler.Disposable = null; // TODO:MZ: DetachEventHandler(m_tpDialogShowingStates);
			m_tpDialogShowingStates.Clear();
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			// Now that we've finished closing, make these interactable again.  They were temporarily
			// disabled while the closing transition played.
			m_tpSmokeLayer.IsHitTestVisible = true;

			if (m_tpLayoutRootPart is not null)
			{
				m_tpLayoutRootPart.IsHitTestVisible = true;
			}
		}

		// Reset this so that it can be re-evaluated the next time the dialog is opened.
		m_placementMode = PlacementMode.Undetermined;

		// We clear the tracker pointer here to allow for callers
		// to perform an additional ShowAsync from the completion
		// handler.
		IAsyncOperation<ContentDialogResult> asyncOperationRef;
		m_tpCurrentAsyncOperation.CopyTo(asyncOperationRef.ReleaseAndGetAddressOf());

		m_tpCurrentAsyncOperation.Clear();
		asyncOperationNoRef.CoreFireCompletion();
	}

	private void AttachButtonEvents()
	{
		var primaryButton = GetButtonHelper(ContentDialogButton.Primary);
		if (primaryButton is not null && m_epPrimaryButtonClickHandler.Disposable is null)
		{
			void primaryButtonClickHandler(object sender, RoutedEventArgs args)
			{
				ICommand command = PrimaryButtonCommand;
				object commandParameter = PrimaryButtonCommandParameter;

				OnCommandButtonClicked(PrimaryButtonClick, command, commandParameter, ContentDialogResult.Primary);
			}
			primaryButton.Click += primaryButtonClickHandler;
			m_epPrimaryButtonClickHandler.Disposable = Disposable.Create(() => primaryButton.Click -= primaryButtonClickHandler);
		}

		var secondaryButton = GetButtonHelper(ContentDialogButton.Secondary);
		if (secondaryButton is not null && m_epSecondaryButtonClickHandler.Disposable is null)
		{
			void secondaryButtonClickHandler(object sender, RoutedEventArgs args)
			{
				ICommand command = SecondaryButtonCommand;

				object commandParameter = SecondaryButtonCommandParameter;

				OnCommandButtonClicked(SecondaryButtonClick, command, commandParameter, ContentDialogResult.Secondary);
			}
			secondaryButton.Click += secondaryButtonClickHandler;
			m_epSecondaryButtonClickHandler.Disposable = Disposable.Create(() => secondaryButton.Click -= secondaryButtonClickHandler);
		}

		var closeButton = GetButtonHelper(ContentDialogButton.Close);
		if (closeButton is not null && m_epCloseButtonClickHandler.Disposable is null)
		{
			void closeButtonClickHandler(object sender, RoutedEventArgs args)
			{
				ICommand command = CloseButtonCommand;

				object commandParameter = CloseButtonCommandParameter;

				OnCommandButtonClicked(CloseButtonClick, command, commandParameter, ContentDialogResult.None);
			}
			closeButton.Click += closeButtonClickHandler;
			m_epCloseButtonClickHandler.Disposable = Disposable.Create(() => closeButton.Click -= closeButtonClickHandler);
		}
	}

	private ButtonBase? GetButtonHelper(ContentDialogButton buttonType)
	{
		MUX_ASSERT(m_templateVersion != TemplateVersion.Unsupported);

		ButtonBase? button = null;

		if (m_templateVersion < TemplateVersion.Redstone2 && buttonType != ContentDialogButton.Close)
		{
			// For Pre-Redstone2 templates, the buttons are constructed in code-behind as needed
			// and hosted within borders, so query our border parts to find the actual buttons.
			UIElement child;

			string primaryText = PrimaryButtonText;

			string secondaryText = SecondaryButtonText;

			if (!string.IsNullOrEmpty(primaryText) && !string.IsNullOrEmpty(secondaryText))
			{
				var buttonHost = (buttonType == ContentDialogButton.Primary ? m_tpButton1HostPart : m_tpButton2HostPart);
				child = buttonHost.Child;
				button = child as ButtonBase;
			}
			else if ((!string.IsNullOrEmpty(primaryText) && buttonType == ContentDialogButton.Primary) || (!string.IsNullOrEmpty(secondaryText) && buttonType == ContentDialogButton.Secondary))
			{
				child = m_tpButton2HostPart.Child;
				button = child as ButtonBase;
			}
		}
		else
		{
			// For Redstone2+ templates, the buttons are simply template parts, so just return those.
			switch (buttonType)
			{
				case ContentDialogButton.Primary:
					button = m_tpPrimaryButtonPart;
					break;

				case ContentDialogButton.Secondary:
					button = m_tpSecondaryButtonPart;
					break;

				case ContentDialogButton.Close:
					button = m_tpCloseButtonPart;
					break;
			}
		}

		return button;
	}

	private ButtonBase? GetDefaultButtonHelper()
	{
		MUX_ASSERT(m_templateVersion >= TemplateVersion.Redstone2);

		var defaultButton = DefaultButton;

		return GetButtonHelper(defaultButton);
	}

	private void BuildAndConfigureButtons()
	{
		MUX_ASSERT(m_templateVersion < TemplateVersion.Redstone2);

		bool hasVisibleButtons = false;

		ButtonBase? primaryButton = null;
		ButtonBase? secondaryButton = null;

		DetachButtonEvents();

		Border? hostThatContainedFocusedButton = null;

		// Determine which button, if any, has focus
		{
			var previouslyFocusedObject = this.GetFocusedElement();

			if (previouslyFocusedObject is not null)
			{
				UIElement elementInFirstHost;
				UIElement elementInSecondHost;

				elementInFirstHost = m_tpButton1HostPart.Child;
				elementInSecondHost = m_tpButton2HostPart.Child;

				if (previouslyFocusedObject == elementInFirstHost)
				{
					hostThatContainedFocusedButton = m_tpButton1HostPart;
				}
				else if (previouslyFocusedObject == elementInSecondHost)
				{
					hostThatContainedFocusedButton = m_tpButton2HostPart;
				}
			}
		}

		// Clear our button containers
		{
			m_tpButton1HostPart.Child = null;
			m_tpButton2HostPart.Child = null;
		}

		// Build our buttons
		{
			string primaryText = PrimaryButtonText;
			if (!string.IsNullOrEmpty(primaryText))
			{
				primaryButton = CreateButton(primaryText);

				bool isEnabled = IsPrimaryButtonEnabled;
				primaryButton.IsEnabled = isEnabled;

				if (m_templateVersion > TemplateVersion.PhoneBlue)
				{
					primaryButton.HorizontalAlignment = HorizontalAlignment.Stretch;
					primaryButton.VerticalAlignment = VerticalAlignment.Stretch;
				}

				primaryButton.ElementSoundMode = ElementSoundMode.FocusOnly;
			}

			string secondaryText = SecondaryButtonText;
			if (!string.IsNullOrEmpty(secondaryText))
			{
				secondaryButton = CreateButton(secondaryText);

				bool isEnabled = IsSecondaryButtonEnabled;
				secondaryButton.IsEnabled = isEnabled;

				if (m_templateVersion > TemplateVersion.PhoneBlue)
				{
					secondaryButton.HorizontalAlignment = HorizontalAlignment.Stretch;
					secondaryButton.VerticalAlignment = VerticalAlignment.Stretch;
				}

				secondaryButton.ElementSoundMode = ElementSoundMode.FocusOnly;
			}
		}

		PopulateButtonContainer(primaryButton, secondaryButton);

		hasVisibleButtons = (primaryButton is not null || secondaryButton is not null);

		// Update the CommandSpace visibility
		{
			var visiblity = (hasVisibleButtons ? Visibility.Visible : Visibility.Collapsed);

			m_tpButton1HostPart.Visibility = visiblity;
			m_tpButton2HostPart.Visibility = visiblity;

		}

		// Set the focus back to where it was, if needed
		if (hostThatContainedFocusedButton is not null)
		{
			UIElement buttonToFocusAsUIE;
			buttonToFocusAsUIE = hostThatContainedFocusedButton.Child;
			if (buttonToFocusAsUIE is not null)
			{
				buttonToFocusAsUIE.Focus(FocusState.Programmatic);
			}
		}
	}

	private ButtonBase CreateButton(string text)
	{
		MUX_ASSERT(m_templateVersion < TemplateVersion.Redstone2);

		Button button = new();

		button.Content = text;
		button.HorizontalAlignment = HorizontalAlignment.Stretch;
		return button;
	}

	private void PopulateButtonContainer(ButtonBase? primaryButton, ButtonBase? secondaryButton)
	{
		MUX_ASSERT(m_templateVersion < TemplateVersion.Redstone2);

		if (secondaryButton is not null && primaryButton is not null)
		{
			m_tpButton2HostPart.Child = secondaryButton;
			m_tpButton1HostPart.Child = primaryButton;
		}
		else if (secondaryButton is not null)
		{
			m_tpButton2HostPart.Child = secondaryButton;
		}
		else if (primaryButton is not null)
		{
			m_tpButton2HostPart.Child = primaryButton;
		}
	}

	private void DetachButtonEvents()
	{
		// By assigning our EventPtrs to empty ones, we're effectively clearing them since
		// the next time the old ones get invoked, they'll unregister themselves.
		if (m_epPrimaryButtonClickHandler.Disposable is not null)
		{
			m_epPrimaryButtonClickHandler.Disposable = null;
		}

		if (m_epSecondaryButtonClickHandler.Disposable is not null)
		{
			m_epSecondaryButtonClickHandler.Disposable = null;
		}

		if (m_epCloseButtonClickHandler.Disposable is not null)
		{
			m_epCloseButtonClickHandler.Disposable = null;
		}
	}

	private void UpdateTitleSpaceVisibility()
	{
		if (m_tpTitlePart is not null)
		{
			var spTitleAsInspectable = Title;
			var spTitleTemplate = TitleTemplate;

			var visibility = (spTitleAsInspectable is not null || spTitleTemplate is not null) ?
				Visibility.Visible : Visibility.Collapsed;

			m_tpTitlePart.Visibility = visibility;
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

	private void OnLayoutRootProcessKeyboardAccelerators(UIElement pSender, ProcessKeyboardAcceleratorEventArgs pArgs)
	{
		// If we're already in the middle of processing accelerators, we don't need to do anything.  See the comment below about
		// how we can get ourselves into this situation.
		if (m_isProcessingKeyboardAccelerators)
		{
			return;
		}
		UIElement pLayoutRoot = m_tpLayoutRootPart;

		// Even if TryInvokeKeyboardAccelerator fails, we want to always mark the args as handled for ContentDialog to ensure the dialog appears
		// as if it's modal.
		VirtualKey key = pArgs.Key;
		using var alwaysHandledGuard = Disposable.Create(() =>
		{
			if (key != VirtualKey.Escape && key != VirtualKey.Enter)
			{
				// We will not set the property if key is already handled through TryInvokde code flow.
				bool bAlreadyHandled = pArgs.Handled;
				if (!bAlreadyHandled)
				{
					// As we are marking each key handled though it's not actually, we will set HandledShouldNotImpedeTextInput
					// property true. InputManager will use this flag to skip SetKeyDownHandled on key, which
					// will allow it as an input to the text box.
					pArgs.HandledShouldNotImpedeTextInput = true;
					pArgs.Handled = true;
				}
			}
		});
		if (pLayoutRoot is not null)
		{
			// This handler is being called from the layout root - so we shouldn't just call TryInvokeKeyboardAccelerators on the layout root itself,
			// as that would cause the event on the layout root to be raised again and we'd be in an infinite loop here.
			// We could fix this in two ways - by iterating over all the children of the layout root and calling TryInvoke on those or we could just
			// use this flag to keep us from re-entering this handler.  The flag is simpler, and good enough for now.
			m_isProcessingKeyboardAccelerators = true;
			using var reentrancyGuard = Disposable.Create(() => m_isProcessingKeyboardAccelerators = false);
			pLayoutRoot.TryInvokeKeyboardAccelerator(pArgs);
		}
	}

	private bool ShouldFireClosing()
	{
		if (m_skipClosingEventOnHide)
		{
			return false;
		}

		var spAsyncInfo = m_tpCurrentAsyncOperation;
		var status = spAsyncInfo.Status;
		if (status == AsyncStatus.Canceled)
		{
			// If the async operation was canceled, the dialog is irrevocably being closed, so
			// we shouldn't fire.
			return false;
		}

		return true;
	}

	private void AttachEventHandlersForOpenDialog()
	{

		if (m_templateVersion == TemplateVersion.PhoneBlue)
		{
			var displayInformation = DisplayInformation.GetForCurrentView();
			if (displayInformation is not null)
			{
				void orientationChangedHandler(DisplayInformation sender, object args)
				{
					if (!m_hideInProgress)
					{
						UpdateVisualState();
						ResetAndPrepareContent();
					}
				};
				displayInformation.OrientationChanged += orientationChangedHandler;
				m_epOrientationChangedHandler.Disposable = Disposable.Create(() => displayInformation.OrientationChanged -= orientationChangedHandler);
			}
		}

		// Only support dismissing the dialog when the window is deactivated for dialogs using the phone-blue template
		// for backwards compatability.
		if (m_templateVersion == TemplateVersion.PhoneBlue)
		{
			Window? currentWindowNoRef = null;
			currentWindowNoRef = DXamlCore.Current.GetAssociatedWindowNoRef(this);
			if (currentWindowNoRef is not null)
			{
				// Note: The weak ref protection shouldn't be needed here. Keeping it for now to avoid
				// risky changes late in the ship cycle.
				var weakInstance = WeakReferencePool.RentWeakReference(this, this);

				void OnWindowActivated(object sender, object args)
				{
					var instance = weakInstance.AsOrNull<IContentDialog>();
					if (instance)
					{
						IFC_RETURN(instance.Cast<ContentDialog>()->OnWindowActivated(sender, args));
					}
				}

				currentWindowNoRef.Activated += OnWindowActivated;
				m_windowActivatedHandler.Disposable = Disposable.Create(() => currentWindowNoRef.Activated -= OnWindowActivated);
			}
		}
	}

	private void OnLayoutRootKeyDown(object sender, KeyRoutedEventArgs args) =>
		ProcessLayoutRootKey(true /*isKeyDown*/, args);

	private void OnLayoutRootKeyUp(object sender, KeyRoutedEventArgs args) =>
		ProcessLayoutRootKey(false /*isKeyDown*/, args);

	private void ProcessLayoutRootKey(bool isKeyDown, KeyRoutedEventArgs args)
	{
		if (m_templateVersion > TemplateVersion.PhoneBlue)
		{
			var key = VirtualKey.None;

			key = args.Key;

			switch (key)
			{
				case VirtualKey.Escape:
					{
						var originalKey = VirtualKey.None;

						originalKey = args.OriginalKey;

						if ((!isKeyDown && originalKey == VirtualKey.GamepadB) ||
							(isKeyDown && originalKey == VirtualKey.Escape))
						{
							ExecuteCloseAction();
							args.Handled = true;
						}
						break;
					}
				case VirtualKey.Enter:
					{
						if (isKeyDown && m_templateVersion >= TemplateVersion.Redstone2)
						{
							var defaultButton = GetDefaultButtonHelper();

							if (defaultButton is not null)
							{
								bool isDefaultButtonEnabled = false;
								isDefaultButtonEnabled = defaultButton.IsEnabled;
								if (isDefaultButtonEnabled)
								{
									defaultButton.ProgrammaticClick();
									args.Handled = true;
								}
							}
						}
						break;
					}
			}
		}
	}

	private void DetachEventHandlers()
	{
		DetachButtonEvents();

		if (m_tpLayoutRootPart is { } layoutRoot)
		{
			if (m_epLayoutRootPointerReleasedHandler.Disposable is not null)
			{
				m_epLayoutRootPointerReleasedHandler.Disposable = null;
			}

			if (m_epLayoutRootLoadedHandler.Disposable is not null)
			{
				m_epLayoutRootLoadedHandler.Disposable = null;
			}

			if (m_epLayoutRootKeyDownHandler.Disposable is not null)
			{
				m_epLayoutRootKeyDownHandler.Disposable = null;
			}

			if (m_epLayoutRootKeyUpHandler.Disposable is not null)
			{
				m_epLayoutRootKeyUpHandler.Disposable = null;
			}

			if (m_epLayoutRootGotFocusHandler.Disposable is not null)
			{
				m_epLayoutRootGotFocusHandler.Disposable = null;
			}

			if (m_epLayoutRootProcessKeyboardAcceleratorsHandler.Disposable is not null)
			{
				m_epLayoutRootProcessKeyboardAcceleratorsHandler.Disposable = null;
			}
		}

		if (m_tpBackgroundElementPart is { } backgroundElement)
		{
			if (m_dialogSizeChangedHandler.Disposable is not null)
			{
				m_dialogSizeChangedHandler.Disposable = null;
			}
		}

		if (m_tpDialogShowingStates is { } dialogShowingStates)
		{
			if (m_dialogShowingStateChangedEventHandler.Disposable is not null)
			{
				m_dialogShowingStateChangedEventHandler.Disposable = null;
			}
		}
	}

	private void DetachEventHandlersForOpenDialog()
	{
		if (m_epOrientationChangedHandler.Disposable is not null)
		{
			ctl::ComPtr<wgrd::IDisplayInformationStatics> displayInformationStatics;

			IFC_RETURN(ctl::GetActivationFactory(wrl_wrappers::HStringReference(
			RuntimeClass_Windows_Graphics_Display_DisplayInformation).Get(),
				&displayInformationStatics));

			if (displayInformationStatics)
			{
				ctl::ComPtr<wgrd::IDisplayInformation> displayInformation;

				IFC_RETURN(displayInformationStatics->GetForCurrentView(&displayInformation));

				if (displayInformation)
				{
					IFC_RETURN(m_epOrientationChangedHandler.DetachEventHandler(displayInformation.Get()));
				}
			}
		}

		if (DXamlCore dxamlCore = DXamlCore::GetCurrent())
		{
			Window* currentWindowNoRef = nullptr;
			IFC_RETURN(dxamlCore->GetAssociatedWindowNoRef(this, &currentWindowNoRef));
			if (m_windowActivatedHandler && currentWindowNoRef)
			{
				IFC_RETURN(m_windowActivatedHandler.DetachEventHandler(ctl::iinspectable_cast(currentWindowNoRef)));
			}
		}
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		Size currentXamlRootSize = default;
		if (XamlRoot.GetForElement(this) is { } xamlRoot)
		{
			currentXamlRootSize = xamlRoot.Size;
		}
		else
		{
			return throw new InvalidOperationException("XamlRoot is not set");
		}

		if (m_tpSmokeLayer)
		{
			m_tpSmokeLayer.Width = currentXamlRootSize.Width;
			m_tpSmokeLayer.Height = currentXamlRootSize.Height;
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			ResetContentProperties();
			SizeAndPositionContentInPopup();
		}
	}

	private void OnDialogSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (m_prepareSmokeLayerAndPopup is not null)
		{
			// HostDialogWithinPopup needs to be called again to set up m_tpSmokeLayerPopup and m_tpPopup with the new m_tpSmokeLayer.
			m_prepareSmokeLayerAndPopup = false;
			HostDialogWithinPopup(true /*wasSmokeLayerFoundAsTemplatePart*/);
		}

		UpdateVisualState();

		// In case of PhoneBlue template, ensures the button position whether it stays on the content or
		// CommandBar by resetting the content.
		// In case of non-PhoneBlue template, do not reset the content by changing the size since the clicked
		// button lost PointerCapture that couldn't raise the Click event on the button. The size
		// changing can be triggered by showing or hiding the SIP status change in non-PhoneBlue template.
		// For example, Sip hiding on the MoneyPenny's landscape mode.
		if (m_templateVersion >= TemplateVersion.PhoneBlue)
		{
			ResetAndPrepareContent();
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			SizeAndPositionContentInPopup();
		}
	}

	private void OnDialogShowingStateChanged(object sender, VisualStateChangedEventArgs args)
	{
		MUX_ASSERT(m_placementMode == PlacementMode.InPlace);

		// Fire the opened or closed events after visual transitions have played.
		if (m_isShowing && !m_hideInProgress)
		{
			SetInitialFocusElement();
			RaiseOpenedEvent();
		}
		else
		{
			MUX_ASSERT(m_hideInProgress);

			m_hideInProgress = false;
			OnFinishedClosing();
		}
	}

	//private void
	//GetPlainText(
	//	out string* strPlainText)
	//{
	//	object spTitle;
	//	*strPlainText = null;

	//	get_Title(&spTitle);

	//	if (spTitle)
	//	{
	//		FrameworkElement.GetStringFromObject(spTitle, strPlainText);
	//	}
	//	else
	//	{
	//		// If we have no title, we'll fall back to the default implementation,
	//		// which retrieves our content as plain text (e.g., if our content is a string,
	//		// it returns that; if our content is a TextBlock, it returns its Text value, etc.)
	//		ContentDialogGenerated.GetPlainText(strPlainText);

	//		// If we get the plain text from the content, then we want to truncate it,
	//		// in case the resulting automation name is very long.
	//		Popup.TruncateAutomationName(strPlainText);
	//	}

	//	return S_OK;
	//}

	private void NotifyInputPaneStateChange(InputPaneState inputPaneState, Rect inputPaneBounds)
	{
		if (m_templateVersion >= TemplateVersion.Threshold)
		{
			UpdateVisualState();
		}

		if (m_placementMode != PlacementMode.InPlace)
		{
			ResetContentProperties();
			SizeAndPositionContentInPopup();
		}
	}

	private void ResetContentProperties()
	{
		if (m_templateVersion < TemplateVersion.Redstone3)
		{
			// Reset the content properties to recalculate the new width and height
			// position with the original border thickness.
			if (m_tpContentScrollViewerPart)
			{
				m_tpContentScrollViewerPart.Cast<ScrollViewer>().Height = DoubleUtil.NaN;
			}

			if (m_tpBackgroundElementPart)
			{
				m_tpBackgroundElementPart.Cast<Border>().Width = DoubleUtil.NaN;
			}

			// We need to Update the Layout after we reset the values, this ensures that we will use the correct values to adjust
			// the ContentDialog size and position.
			UpdateLayout();
		}
	}

	private void SetPopupAutomationProperties()
	{
		// Bug 15664046: m_tpPopup is expected to be null in some scenarios, like if the dialog is InPlace.
		if (m_tpPopup is null)
		{
			return;
		}

		// If a Title string exists, make it the default AutomationProperties.Name string
		// for the Popup if the developer does not explicitly set one (see GetPlainText on
		// any FrameworkElement controls).
		string defaultAutomationName = GetPlainText();
		m_tpPopup.SetDefaultAutomationName(defaultAutomationName);

		// We'll explicitly pass on AutomationProperties.Name to the popup as well,
		// since GetPlainText is only the default plain-text to return if AutomationProperties.Name
		// is not set.
		string automationName = AutomationProperties.GetName(this);
		AutomationProperties.SetName(m_tpPopup, automationName);

		// Update the automation Id as well.
		string automationId = AutomationProperties.GetAutomationId(this);
		AutomationProperties.SetAutomationId(m_tpPopup, automationId);
	}

	private void SetInitialFocusElement()
	{
		bool wasFocusSet = false;

		if (m_templateVersion > TemplateVersion.PhoneBlue)
		{
			// Save the focused element in order to give focus back to that once the ContentDialog dismisses.
			var previouslyFocusedObject = this.GetFocusedElement();

			if (previouslyFocusedObject is not null)
			{
				m_spFocusedElementBeforeContentDialogShows = WeakReferencePool.RentWeakReference(this, previouslyFocusedObject);
			}
		}

		// Try to set focus to the first focusable element in the content area.
		if (m_tpContentPart is not null)
		{
			var firstFocusableDO = FocusManager_GetFirstFocusableElement(m_tpContentPart);

			if (firstFocusableDO is not null)
			{
				var pFocusManager = VisualTree.GetFocusManagerForElement(this);

				using InitialFocusSIPSuspender setInitalFocusTrue = new InitialFocusSIPSuspender(pFocusManager);
				this.SetFocusedElement(
					firstFocusableDO,
					FocusState.Programmatic,
					false /*animateIfBringIntoView*/);
			}
		}

		// If not set, try to focus the default button.
		if (!wasFocusSet && m_templateVersion >= TemplateVersion.Redstone2)
		{
			var defaultButton = GetDefaultButtonHelper();
			if (defaultButton is not null)
			{
				(DependencyObject.SetFocusedElement(defaultButton.Cast<ButtonBase>(),
					FocusState.Programmatic, false /*animateIfBringIntoView*/);
			}
		}

		// If not set, try to focus the first focusable command button.
		if (!wasFocusSet && m_tpCommandSpacePart is not null)
		{
			var firstFocusableDO = FocusManager_GetFirstFocusableElement(m_tpCommandSpacePart);

			if (firstFocusableDO is not null)
			{
				DependencyObject firstFocusableDOPeer;
				DXamlCore.Current.GetPeer(firstFocusableDO, &firstFocusableDOPeer);

				(DependencyObject.SetFocusedElement(firstFocusableDOPeer,
					FocusState.Programmatic, false /*animateIfBringIntoView*/);
			}
		}
	}

	private void ExecuteCloseAction()
	{
		bool didInvokeClose = false;

		string closeButtonText = CloseButtonText;

		// If we have a clickable close button, then invoke it, otherwise just
		// return a result of None.
		if (!string.IsNullOrEmpty(closeButtonText))
		{
			var closeButton = GetButtonHelper(ContentDialogButton.Close);
			if (closeButton is not null)
			{
				bool isCloseButtonEnabled = false;
				isCloseButtonEnabled = m_tpCloseButtonPart.IsEnabled;
				if (isCloseButtonEnabled)
				{
					closeButton.ProgrammaticClick();
					didInvokeClose = true;
				}
			}
		}

		// If there was no close button to invoke, the just call hide.
		if (!didInvokeClose)
		{
			HideInternal(ContentDialogResult.None);
		}
	}

	private void AdjustVisualStateForInputPane()
	{
		MUX_ASSERT(m_templateVersion > TemplateVersion.PhoneBlue);
		MUX_ASSERT(m_tpLayoutRootPart is not null);
		MUX_ASSERT(m_tpBackgroundElementPart is not null);
		MUX_ASSERT(m_tpContentScrollViewerPart is not null);

		if (XamlOneCoreTransforms.IsEnabled)
		{
			// TODO: 12179953 : XAML agrees on coordinate space with input pane
			// For now we disable input rect occlusion in strict mode
			return;
		}

		Rect inputPaneRect = DXamlCore.Current.GetInputPaneOccludeRect(this);

		if (m_isShowing && inputPaneRect.Height > 0)
		{
			// The rect we get is in screen coordinates, so translate it into client
			// coordinates by subtracting our window's origin point (itself translated
			// into screen coords) from it.
			{
				Point point = DXamlCore.Current.ClientToScreen();

				inputPaneRect.X -= point.X;
				inputPaneRect.Y -= point.Y;
			}

			static Rect getElementBounds(FrameworkElement element)
			{
				GeneralTransform transform = element.TransformToVisual(null);

				double width = 0;
				width = element.ActualWidth;

				double height = 0;
				height = element.ActualHeight;

				return transform.TransformBounds(new(0, 0, width, height));
			}

			Rect layoutRootBounds = getElementBounds(m_tpLayoutRootPart!);

			Rect dialogBounds = getElementBounds(m_tpBackgroundElementPart!);

			// If the input pane overlaps the dialog (including a 12px bottom margin), the dialog will get translated
			// up so that is not occluded, while also preserving a 12px margin between the bottom of the dialog
			// and the top of the input pane (see redlines).
			// We achieve this by aligning the dialog to the bottom of its parent panel, if not full-size, and
			// then setting a bottom padding on the parent panel creating a reserved area that corresponds to the
			// intersection of the parent panel's bounds and the input pane's bounds.
			if (inputPaneRect.Y < (dialogBounds.Y + dialogBounds.Height + ContentDialog_SIP_Bottom_Margin))
			{
				Thickness layoutRootPadding = default;
				var contentVerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				bool setDialogVisibility = false;
				var dialogVerticalAlignment = VerticalAlignment.Center;

				layoutRootPadding = new Thickness(0, 0, 0, layoutRootBounds.Height - Math.Max(inputPaneRect.Y - layoutRootBounds.Y, (float)(m_dialogMinHeight)) + ContentDialog_SIP_Bottom_Margin);

				bool fullSizeDesired = FullSizeDesired;
				if (!fullSizeDesired)
				{
					dialogVerticalAlignment = VerticalAlignment.Bottom;
					setDialogVisibility = true;
				}

				// Apply our layout adjustments using a storyboard so that we don't stomp over template or user
				// provided values.  When we stop the storyboard, it will restore the previous values.
				Storyboard storyboard = CreateStoryboardForLayoutAdjustmentsForInputPane(layoutRootPadding, contentVerticalScrollBarVisibility, setDialogVisibility, dialogVerticalAlignment);

				storyboard.Begin();
				storyboard.SkipToFill();
				m_layoutAdjustmentsForInputPaneStoryboard = storyboard;
			}
		}
		else if (m_layoutAdjustmentsForInputPaneStoryboard is not null)
		{
			m_layoutAdjustmentsForInputPaneStoryboard.Stop();
			m_layoutAdjustmentsForInputPaneStoryboard = null;
		}
	}

	private Storyboard CreateStoryboardForLayoutAdjustmentsForInputPane(
		Thickness layoutRootPadding,
		ScrollBarVisibility contentVerticalScrollBarVisiblity,
		bool setDialogVerticalAlignment,
		VerticalAlignment dialogVerticalAlignment)
	{
		Storyboard storyboardLocal = new();

		var storyboardChildren = storyboardLocal.Children;

		// LayoutRoot Padding
		{
			ObjectAnimationUsingKeyFrames objectAnimation = new();

			CoreImports.Storyboard_SetTarget(objectAnimation, m_tpBackgroundElementPart);
			Storyboard.SetTargetProperty(objectAnimation, "Margin");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame = new();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = layoutRootPadding;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		// ContentScrollViewer VerticalScrollBarVisibility
		{
			ObjectAnimationUsingKeyFrames objectAnimation;
			objectAnimation = new();

			CoreImports.Storyboard_SetTarget(objectAnimation, m_tpContentScrollViewerPart);
			Storyboard.SetTargetProperty(objectAnimation, "VerticalScrollBarVisibility");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame = new();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = contentVerticalScrollBarVisiblity;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		// BackgroundElement VerticalAlignment
		if (setDialogVerticalAlignment)
		{
			ObjectAnimationUsingKeyFrames objectAnimation;
			objectAnimation = new();

			CoreImports.Storyboard_SetTarget(objectAnimation, m_tpBackgroundElementPart);
			Storyboard.SetTargetProperty(objectAnimation, "VerticalAlignment");

			var objectKeyFrames = objectAnimation.KeyFrames;

			DiscreteObjectKeyFrame discreteObjectKeyFrame;
			discreteObjectKeyFrame = new();

			KeyTime keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

			discreteObjectKeyFrame.KeyTime = keyTime;
			discreteObjectKeyFrame.Value = dialogVerticalAlignment;

			objectKeyFrames.Add(discreteObjectKeyFrame);
			storyboardChildren.Add(objectAnimation);
		}

		return storyboardLocal;
	}

	private void RaiseOpenedEvent()
	{
		ContentDialogOpenedEventArgs args = new();

		Opened?.Invoke(this, args);
	}

	private void RaiseClosedEvent(ContentDialogResult result)
	{
		ContentDialogClosedEventArgs args = new(result);

		Closed?.Invoke(this, args);
	}

	// For testing purposes only. Invoked by IXamlTestHooks.SimulateRegionsForContentDialog implementation.
	private void SimulateRegionsForContentDialog()
	{
		m_simulateRegions = true;
		SizeAndPositionContentInPopup();
	}

	private void SetButtonPropertiesFromCommand(ContentDialogButton buttonType, ICommand? oldCommand = null)
	{
		DependencyProperty commandPropertyIndex;
		DependencyProperty textPropertyIndex;

		MUX_ASSERT(buttonType != ContentDialogButton.None);
		var button = GetButtonHelper(buttonType);

		if (button is not null)
		{
			var buttonNoRef = (ButtonBase)button;

			if (buttonType == ContentDialogButton.Primary)
			{
				commandPropertyIndex = PrimaryButtonCommandProperty;
				textPropertyIndex = PrimaryButtonTextProperty;
			}
			else if (buttonType == ContentDialogButton.Secondary)
			{
				commandPropertyIndex = SecondaryButtonCommandProperty;
				textPropertyIndex = SecondaryButtonTextProperty;
			}
			else /* buttonType == ContentDialogButton.Close */
			{
				commandPropertyIndex = CloseButtonCommandProperty;
				textPropertyIndex = CloseButtonTextProperty;
			}

			if (oldCommand is not null)
			{
				ICommand oldCommandComPtr = oldCommand;
				var oldCommandAsUICommand = oldCommandComPtr as XamlUICommand;

				if (oldCommandAsUICommand is not null)
				{
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, textPropertyIndex);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, buttonNoRef, KeyboardAcceleratorsProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, buttonNoRef, AccessKeyProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, buttonNoRef, AutomationProperties.HelpTextProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, buttonNoRef, ToolTipService.ToolTipProperty);
				}
			}

			var newCommandAsI = GetValue(commandPropertyIndex) as ICommand;

			if (newCommandAsI is not null)
			{
				ICommand newCommand = newCommandAsI;
				var newCommandAsUICommand = newCommand as XamlUICommand;

				if (newCommandAsUICommand is not null)
				{
					CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, this, textPropertyIndex);
					CommandingHelpers.BindToKeyboardAcceleratorsIfUnset(newCommandAsUICommand, buttonNoRef);
					CommandingHelpers.BindToAccessKeyIfUnset(newCommandAsUICommand, buttonNoRef);
					CommandingHelpers.BindToDescriptionPropertiesIfUnset(newCommandAsUICommand, buttonNoRef);
				}
			}
		}
	}

	private void DiscardPopup()
	{
		if (auto popup = m_tpPopup.GetSafeReference())
		{
			if (m_popupOpenedHandler)
			{
				IFC_RETURN(m_popupOpenedHandler.DetachEventHandler(popup.Get()));
			}
		}
		m_tpPopup.Clear();
	}

	override ::put_XamlRootImpl(_In_ xaml::IXamlRoot* pValue)
	{
		IFC_RETURN(__super::put_XamlRootImpl(pValue));

	VisualTree* tree = VisualTree::GetForElementNoRef(GetHandle());

		if (m_tpSmokeLayerPopup)
		{
			static_cast<CPopup*>(m_tpSmokeLayerPopup.Cast<DirectUI::Popup>()->GetHandle())->SetAssociatedVisualTree(tree);
}
		if (m_tpPopup)
		{
			static_cast<CPopup*>(m_tpPopup.Cast<DirectUI::Popup>()->GetHandle())->SetAssociatedVisualTree(tree);
		}
		return S_OK;
	}
}
