// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/RichEditBox_Partial.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Initializes a new instance.
	//
	//---------------------------------------------------------------------------
	/// <summary>Initializes a new instance of the RichEditBox class.</summary>
	public RichEditBox()
	{
		m_isInitializing = true;
		m_isAnimatingHeight = false;
#if HAS_UNO
		// The native activation factory invokes Initialize after construction.
		InitializeRichEditBoxCore();
		Initialize();
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Releases resources held by an instance.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: No finalizer is needed for the empty native destructor.
	// RichEditBox::~RichEditBox()
	// {
	// }
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerEntered(pArgs);
		OnPointerEnteredCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerExited(pArgs);
		OnPointerExitedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerPressed(pArgs);
		OnPointerPressedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerMoved(pArgs);
		OnPointerMovedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerReleased(pArgs);
		OnPointerReleasedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerCaptureLost(pArgs);
		OnPointerCaptureLostCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnPointerCanceled(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerCanceled(pArgs);
#if HAS_UNO
		OnPointerCaptureLostCore(pArgs);
#else
		// TODO Uno: The managed pointer adapter replaces the native event bridge.
		// IFC(TextBox::RaiseNative(this, pArgs, KnownEventIndex::UIElement_PointerCanceled));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs pArgs)
	{
		base.OnDoubleTapped(pArgs);
		OnDoubleTappedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnTapped(TappedRoutedEventArgs pArgs)
	{
		base.OnTapped(pArgs);
		OnTappedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnRightTapped(RightTappedRoutedEventArgs pArgs)
	{
		base.OnRightTapped(pArgs);
		OnRightTappedCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnHolding(HoldingRoutedEventArgs pArgs)
	{
		base.OnHolding(pArgs);
		OnHoldingCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs pArgs)
	{
		base.OnManipulationStarted(pArgs);
#if !HAS_UNO
		// TODO Uno: ScrollViewer/InteractionTracker own managed manipulations; there is no native RichEdit peer.
		// IFC(TextBox::RaiseNative(this, pArgs, KnownEventIndex::UIElement_ManipulationStarted));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs pArgs)
	{
		base.OnManipulationCompleted(pArgs);
#if !HAS_UNO
		// TODO Uno: ScrollViewer/InteractionTracker own managed manipulations; there is no native RichEdit peer.
		// IFC(TextBox::RaiseNative(this, pArgs, KnownEventIndex::UIElement_ManipulationCompleted));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
	{
		base.OnKeyUp(pArgs);
		OnKeyUpCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
	{
		base.OnKeyDown(pArgs);
		OnKeyDownCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		base.OnGotFocus(pArgs);
		OnGotFocusCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		base.OnLostFocus(pArgs);
		OnLostFocusCore(pArgs);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Create RichEditBoxAutomationPeer to represent the RichEditBox.
	//
	//---------------------------------------------------------------------------
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new RichEditBoxAutomationPeer(this);

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns a plain text string to provide a default AutomationProperties.Name
	//      in the absence of an explicitly defined one
	//
	//---------------------------------------------------------------------------
	internal override string GetPlainText()
	{
		var spHeader = Header;
		var strPlainText = string.Empty;
		var pLength = 0;
		if (spHeader is not null)
		{
			strPlainText = FrameworkElement.GetStringFromObject(spHeader);
			pLength = strPlainText?.Length ?? 0;
		}

		if (pLength == 0)
		{
			strPlainText = PlaceholderText;
		}

		return strPlainText ?? string.Empty;
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, passes control to core layer.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: Unicode insertion is routed through OnPostKeyDown and IImeSessionHost, not a native peer.
	// _Check_return_ HRESULT RichEditBox::OnCharacterReceivedImpl(_In_ xaml_input::ICharacterReceivedRoutedEventArgs* pArgs)
	// {
	//     IFC_RETURN(TextBox::RaiseNative(this, ctl::as_iinspectable(pArgs), KnownEventIndex::UIElement_CharacterReceived));
	//     return S_OK;
	// }
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler override, raises event in the core layer
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: The managed dependency-property callbacks and OnFont* overrides forward inherited changes.
	// _Check_return_ HRESULT RichEditBox::OnInheritedPropertyChanged(_In_ IInspectable* pArgs)
	// {
	//     HRESULT hr = S_OK;
	//     IFC(RichEditBoxGenerated::OnInheritedPropertyChanged(pArgs));
	//     IFC(TextBox::RaiseNative(this, pArgs, KnownEventIndex::Control_InheritedPropertyChanged));
	// Cleanup:
	//     RRETURN(hr);
	// }
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      IsEnabled property changed override, raises event in the core layer
	//
	//---------------------------------------------------------------------------
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
	{
		base.OnIsEnabledChanged(pArgs);
		UpdateVisualState();
	}

	private RichEditTextDocument GetDocumentImpl() => GetDocument();

	private RichEditTextDocument GetTextDocumentImpl() => GetDocumentImpl();

	private void AddCandidateWindowBoundsChanged(TypedEventHandler<RichEditBox, CandidateWindowBoundsChangedEventArgs>? pValue)
	{
		// This may fail when text services are not functioning
		// TODO: https://task.ms/1887013
#if HAS_UNO
		EnableManagedCandidateWindowBoundsTracking();
		_candidateWindowBoundsChanged += pValue;
#else
		// TODO Uno: Native TSF candidate tracking is replaced by IImeTextBoxExtension.
		// IGNOREHR(static_cast<CTextBoxBase*>(GetHandle())->EnableCandidateWindowBoundsTracking(EventHandle(KnownEventIndex::RichEditBox_CandidateWindowBoundsChanged)));
		// return RichEditBoxGenerated::add_CandidateWindowBoundsChanged(pValue, ptToken);
#endif
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Navigates a hyperlink by invoking the Launcher
	//
	//------------------------------------------------------------------------
	private void HandleHyperlinkNavigation(string pLinkText)
	{
		var spUri = new Uri(pLinkText, UriKind.Absolute);
#if HAS_UNO
		// The Uno launcher is asynchronous; the adapter observes failures and provides the existing test seam.
		_ = LaunchLinkAsync(spUri);
#else
		// TODO Uno: Launcher::TryInvokeLauncher is represented by Windows.System.Launcher in Uno.
		// IFC_RETURN(Launcher::TryInvokeLauncher(spUri.Get()));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      OnApplyTemplate callback from core, keeps templated parts around
	//
	//---------------------------------------------------------------------------
	private void OnApplyTemplateHandler()
	{
		//
		// Get the framework peer.
		//
		var pRichEditBoxNoRef = this;
		try
		{
			//
			// Cleanup any state from a previous template.
			//
			pRichEditBoxNoRef.ReleaseTemplateParts();
			var spPlaceholderTextPresenter = GetTemplateChild("PlaceholderTextContentPresenter") as UIElement;
			pRichEditBoxNoRef.SetPlaceholderTextPresenter(spPlaceholderTextPresenter);
			pRichEditBoxNoRef.UpdateHeaderPresenterVisibility();

			var spTextDocument = pRichEditBoxNoRef.Document;
			var spTextRange = spTextDocument.GetRange(0, 2);
			var storyLength = spTextRange.StoryLength;

			// I'm unable to use IsEmpty() on the native peer to check if the RichEditBox is empty, so instead I use
			// the same logic, checking if storyLength is <= 1.
			pRichEditBoxNoRef.UpdatePlaceholderTextPresenterVisibility(storyLength <= 1);
		}
		finally
		{
			pRichEditBoxNoRef.m_isInitializing = false;
		}
	}

	// static
	private void OnTextChangingHandler(bool fTextChanged)
	{
#if !HAS_UNO
		// TODO Uno: A managed instance is its own peer, so native peer lookup/COM casts are unnecessary.
		// IFC_RETURN(DXamlCore::GetCurrent()->TryGetPeer(pNativeRichEditBox, &peer));
		// if (!peer)
		// {
		//     // There is no need to fire the TextChanging event if there is no peer, since no one is listening
		//     return S_FALSE;
		// }
#endif
		m_textChangingEventArgs ??= new RichEditBoxTextChangingEventArgs();
		m_textChangingEventArgs.IsContentChanging = fTextChanged;
		_textChanging?.Invoke(this, m_textChangingEventArgs);
	}

	private bool OnSelectionChangingHandler(int selectionStart, int selectionLength)
	{
		var wasCanceled = false;
		m_selectionChangingEventArgs ??= new RichEditBoxSelectionChangingEventArgs();
		m_selectionChangingEventArgs.SelectionStart = selectionStart;
		m_selectionChangingEventArgs.SelectionLength = selectionLength;
		m_selectionChangingEventArgs.Cancel = false;
		SelectionChanging?.Invoke(this, m_selectionChangingEventArgs);
		wasCanceled = m_selectionChangingEventArgs.Cancel;
		return wasCanceled;
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Prepare handlers.
	//
	//---------------------------------------------------------------------------
	private void Initialize()
	{
#if !HAS_UNO
		// TODO Uno: The C# base constructor performs generated initialization.
		// IFC(RichEditBoxGenerated::Initialize());
#endif
		SizeChanged += OnSizeChanged; // subscribingToSelf
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      SizeChanged Handler
	//
	//---------------------------------------------------------------------------
	private void OnSizeChanged(object pSender, SizeChangedEventArgs pArgs)
	{
		if (m_isAnimatingHeight)
		{
			return;
		}

		// Get the sizes and set the animation values.
		var previousSize = pArgs.PreviousSize;
		var newSize = pArgs.NewSize;

		// Do not animate if the RichEditBox is setting its initial height.
		if (previousSize.Height == 0)
		{
			return;
		}

		// Make sure that we are not currently animating and that we do not animate a subpixel
		// Height change.
		if (Math.Abs(newSize.Height - previousSize.Height) >= 1)
		{
			// Skip the animation if this control does not have focus.
			if (XamlRoot is not { } xamlRoot || !ReferenceEquals(FocusManager.GetFocusedElement(xamlRoot), this))
			{
				return;
			}

			// Skip the animation if the current height is different from NaN.
			var spSenderAsFE = (FrameworkElement)pSender;
			var startHeight = spSenderAsFE.Height;
			if (!double.IsNaN(startHeight))
			{
				return;
			}

			// Skip the animation when TextWrapping is turned off and AcceptsReturn is false.
			var textWrapping = TextWrapping;
			var acceptsReturn = AcceptsReturn;
			if (textWrapping == TextWrapping.NoWrap && !acceptsReturn)
			{
				return;
			}

			// Variables.
			// Set the duration for the animation.
			var durationTime = 1000000L; // 100 ms.
			var duration = new Duration(TimeSpan.FromTicks(durationTime));

			// Get the statics
			// Create the animation and the storyboard.
			var spDoubleAnimation = new DoubleAnimation();
			var spStoryboard = new Storyboard();

			// Enable dependent animation, as we are animating height.
			spDoubleAnimation.EnableDependentAnimation = true;
			spDoubleAnimation.Duration = duration;

			// Add the from/to properties to the animation.
			spDoubleAnimation.From = previousSize.Height;
			spDoubleAnimation.To = newSize.Height;

			// Set the targets.
			Storyboard.SetTargetProperty(spDoubleAnimation, "Height");
			Storyboard.SetTarget(spDoubleAnimation, spSenderAsFE);

			// Add the double animation to the storyboard.
			spStoryboard.Children.Add(spDoubleAnimation);

			// Add a completed handler.
			EventHandler<object> completed = (sender, _) => OnHeightAnimationCompleted(spStoryboard, spSenderAsFE);
			spStoryboard.Completed += completed;
			m_storyboardCompletedToken.Disposable = Disposable.Create(() => spStoryboard.Completed -= completed);
#if HAS_UNO
			_heightAnimation = spStoryboard;
#endif

			// Start the storyboard and block animation events.
			spStoryboard.Begin();
			m_isAnimatingHeight = true;

			// Prevent the caret from being brought into view until the animation is complete.
			DisableEnsureRectVisible();

			// Prevent this event from continuing to bubble up because we are animating from the
			// "old" value.
#if !HAS_UNO
			// TODO Uno: SizeChanged does not bubble in the managed layout engine.
			// IFC(static_cast<SizeChangedEventArgs*>(pArgs)->SetHandled(TRUE));
#endif
		}
	}

	private void OnHeightAnimationCompleted(Storyboard pSender, FrameworkElement pSenderAsFE)
	{
		try
		{
			// Set the height to NaN, as the "completion height" will
			// force the textbox to keep its current size.  This allows
			// the textbox to expand.
#if HAS_UNO
			// Remove Uno's filling animation precedence before restoring Auto height.
			pSender.Stop();
			_heightAnimation = null;
#endif
			pSenderAsFE.Height = double.NaN;
			m_storyboardCompletedToken.Disposable = null;
			UpdateDisplaySelection();
			RaisePendingBringLastVisibleRectIntoView(true /* forceIntoView */, false /* focusChanged */);
		}
		finally
		{
			// Allow height to animate on a new size change (we
			// didn't get this event from the animation size change).
			m_isAnimatingHeight = false;
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Callback from core, toggles the visiblity of Placeholder Text
	//      whenever text is changed.
	//
	//---------------------------------------------------------------------------
	private void ShowPlaceholderTextHandler(bool isEnabled)
	{
		//
		// Get the framework peer.
		//
		UpdatePlaceholderTextPresenterVisibility(isEnabled);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Releases resources associated with the current template.
	//
	//---------------------------------------------------------------------------
	private void ReleaseTemplateParts()
	{
		m_tpHeaderPresenter = null;
		m_tpPlaceholderTextPresenter = null;
#if HAS_UNO
		OnManagedPlaceholderPresenterChanged(null);
#endif
#if !HAS_UNO
		// TODO Uno: Feature_HeaderPlacement is not part of the supported WinUI contract.
		// m_requiredHeaderPresenter.Clear();
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Updates the visibility of the Header property. If Header and Header
	//      Template are not set, it should collapse the property.
	//
	//---------------------------------------------------------------------------
	private void UpdateHeaderPresenterVisibility()
	{
		var spHeaderTemplate = HeaderTemplate;
		var spHeader = Header;
#if HAS_UNO
		// Control's helper cannot query deferred native names; inspect the existing namescope without realizing a stub.
		m_tpHeaderPresenter ??= FindMaterializedHeaderPresenter();
#endif
		ConditionallyGetTemplatePartAndUpdateVisibility(
			"HeaderContentPresenter",
			spHeader is not null || spHeaderTemplate is not null,
			ref m_tpHeaderPresenter);
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Updates PlaceholderText visibility whenever text is updated
	//
	//---------------------------------------------------------------------------
	private void UpdatePlaceholderTextPresenterVisibility(bool isEnabled)
	{
		if (m_tpPlaceholderTextPresenter is not null)
		{
			TextBoxPlaceholderTextHelper.UpdatePlaceholderTextPresenterVisibility(
				this, m_tpPlaceholderTextPresenter, isEnabled);
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Handles the custom property changed event and calls OnPropertyChanged2
	//      Methods.
	//
	//---------------------------------------------------------------------------
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// We will ignore property changes during initialization and take care of them when we have a template.
		if (!m_isInitializing)
		{
			if (args.Property == HeaderProperty || args.Property == HeaderTemplateProperty)
			{
				UpdateHeaderPresenterVisibility();
				InvalidateView();
			}
			else if (args.Property == PlaceholderTextProperty)
			{
				//UpdatePlaceholder visibility here
				UpdatePlaceholderTextPresenterVisibility(
					TextBoxPlaceholderTextHelper.ShouldMakePlaceholderTextVisible(
						m_tpPlaceholderTextPresenter, this));
			}
		}
#if HAS_UNO
		OnManagedPropertyChanged(args);
#endif
	}

	/// <summary>Gets a list of linguistic alternatives for the current text.</summary>
	/// <returns>The asynchronous operation that returns the linguistic alternatives.</returns>
	public IAsyncOperation<IReadOnlyList<string>> GetLinguisticAlternativesAsync()
	{
#if HAS_UNO
		return AsyncOperation.FromTask(GetLinguisticAlternativesCoreAsync);
#else
		// TODO Uno: TextAlternativesOperation uses Windows TSF; IImeTextBoxExtension supplies managed alternatives.
		// Microsoft::WRL::ComPtr<TextAlternativesOperation> spLoadMoreItemsOperation;
		// IFC_RETURN(Microsoft::WRL::MakeAndInitialize<TextAlternativesOperation>(&spLoadMoreItemsOperation));
		// IFC_RETURN(spLoadMoreItemsOperation->Init(static_cast<CTextBoxBase*>(GetHandle())));
		// IFC_RETURN(spLoadMoreItemsOperation->Start());
		// IFC_RETURN(spLoadMoreItemsOperation.CopyTo(ppReturnValue));
		// return S_OK;
#endif
	}

	private bool OnContextMenuOpeningHandler(double cursorLeft, double cursorTop)
	{
		var contextMenuOpeningEventArgs = new ContextMenuEventArgs(cursorLeft, cursorTop);
		ContextMenuOpening?.Invoke(this, contextMenuOpeningEventArgs);
		return contextMenuOpeningEventArgs.Handled;
	}

	private void QueueUpdateSelectionFlyoutVisibility()
	{
#if HAS_UNO
		var peerWeakRef = new WeakReference<RichEditBox>(this);
		DispatcherQueue.TryEnqueue(() =>
		{
			if (peerWeakRef.TryGetTarget(out var peer))
			{
				peer.UpdateSelectionFlyoutVisibility();
			}
		});
#else
		// TODO Uno: The managed weak callback replaces TextControlHelper's native peer dispatcher.
		// return TextControlHelper::QueueUpdateSelectionFlyoutVisibility<RichEditBox>(pNativeRichEditBox);
#endif
	}

	private void UpdateSelectionFlyoutVisibility() => UpdateSelectionFlyoutVisibilityCore();
}
