#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// Skia uses the shared managed text surface while preserving RichEditBox's document semantics.
	partial class RichEditBox : ITextBoxViewHost, ITextSelectionGripperHost, IFocusRequestOriginHandler
	{
		Control ITextBoxViewHost.Owner => this;

		private TextBoxView? _textBoxView;
		private TextSelectionGripperPresenter? _gripperPresenter;
		private ContentControl? _contentElement;
		private global::Microsoft.UI.Text.RichEditTextDocument? _document;
		private bool _pointerPressedHandlerRegistered;
		private bool _isPointerOver;
		private FocusState _imeFocusOrigin;
		private bool _imeFocusRequestInProgress;
		private bool _imeWasFocusedBeforeRequest;
		private bool _pendingUpdateScrolling;
		private int? _pendingScrollingTargetIndex;
		private int? _bringIntoViewTargetIndex;
		private ScrollViewer? _imeScrollViewer;
		private bool _isImeLayoutTrackingAttached;
		private Storyboard? _heightAnimation;
		private readonly SerialDisposable _placeholderTextChangedSubscription = new();
		private global::Windows.Foundation.TypedEventHandler<RichEditBox, CandidateWindowBoundsChangedEventArgs>? _candidateWindowBoundsChanged;
		private DisabledFormattingAccelerators _enabledFormattingAccelerators =
			DisabledFormattingAccelerators.Bold | DisabledFormattingAccelerators.Italic | DisabledFormattingAccelerators.Underline;

		private void ApplyManagedTemplate()
		{
			DetachImeGeometryTracking();
			var focusState = FocusState;
			if (focusState != FocusState.Unfocused)
			{
				_textBoxView?.OnFocusStateChanged(FocusState.Unfocused);
			}

			// Ensures we don't keep a reference to a TextBoxView that exists in a previous template.
			_gripperPresenter?.Hide();
			_gripperPresenter = null;
			_textBoxView = null;

			_contentElement = GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;

			if (_contentElement is { })
			{
				_contentElement.SetProtectedCursor(Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.IBeam));
			}

			UpdateTextBoxView();
			InitializeTextBoxViewProperties();
			RegisterPointerPressedHandler();

			OnApplyTemplateHandler();
			UpdateDescriptionVisibility(initialization: true);

			UpdateVisualState();
			DispatchUpdateScrolling();
			AttachImeGeometryTracking();
			if (focusState != FocusState.Unfocused)
			{
				ActivateImeForFocusOrigin(focusState);
			}
		}

		private void UpdateTextBoxView()
		{
			_textBoxView ??= new TextBoxView(this);
			if (_contentElement != null)
			{
				var displayBlock = _textBoxView.DisplayBlock;
				if (_contentElement.Content != displayBlock)
				{
					_contentElement.Content = displayBlock;
				}
				_gripperPresenter ??= new TextSelectionGripperPresenter(this);

				RenderDocument();
			}
		}

		private void InitializeTextBoxViewProperties()
		{
			if (_textBoxView is not { } view)
			{
				return;
			}

			view.SetWrapping();
			UpdateTextWrappingScrollMode();
			view.SetTextAlignment();
			view.SetReadingOrder();
			view.SetColorFontEnabled();
			view.UpdateFont();
			view.DisplayBlock.IsSpellCheckEnabled = IsSpellCheckEnabled;
			view.UpdateProperties();
			UpdateSelectionHighlightColor();
		}

		private void UpdateTextWrappingScrollMode()
		{
			if (_contentElement is ScrollViewer scrollViewer)
			{
				scrollViewer.HorizontalScrollBarVisibility = TextWrapping == TextWrapping.NoWrap
					? ScrollBarVisibility.Auto
					: ScrollBarVisibility.Disabled;
			}
		}

		private void RegisterPointerPressedHandler()
		{
			if (_pointerPressedHandlerRegistered)
			{
				return;
			}

			_pointerPressedHandlerRegistered = true;
			AddHandler(PointerPressedEvent, new PointerEventHandler(OnPointerPressedHandledEventsToo), handledEventsToo: true);
		}

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description is null)
			{
				return;
			}

			if (FindName("DescriptionPresenter") is ContentPresenter presenter)
			{
				presenter.Visibility = Description is null ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		/// <summary>Returns the current plain-text content held by the TOM document.</summary>
		internal string GetPlainTextContent() => _document?.PlainText ?? string.Empty;

		internal int GetPlainTextLength() => _document?.TextLength ?? 0;

		internal string GetPlainTextSlice(int start, int length)
			=> _document?.GetTextInRange(start, start + length) ?? string.Empty;

		/// <summary>
		/// Called by <see cref="global::Microsoft.UI.Text.RichEditTextDocument"/> after the document
		/// text changes so the control can re-render and refresh dependent visuals.
		/// </summary>
		internal void OnDocumentTextChanged(bool isContentChanging)
		{
			// If the text changed by something other than the active IME composition, cancel it first
			// (guarded so composition-internal edits don't self-cancel).
			CancelCompositionOnExternalChange();

			OnContentChanged(isContentChanging);

			RenderDocument();
			(FrameworkElementAutomationPeer.FromElement(this) as RichEditBoxAutomationPeer)?.OnDocumentAccessibilityChanged();

			OnDocumentTextChangedInteractive();
			DispatchUpdateScrolling();
			ImeSessionCoordinator.UpdateSession(this, ImeSessionUpdate.TextAndSelection);
		}

		internal void OnDocumentMathModeChanged()
		{
			RenderDocument();
			DispatchUpdateScrolling();
		}

		internal void OnDocumentCaretTypeChanged() => UpdateDisplaySelection();

		internal override void UpdateFocusState(FocusState focusState)
		{
			var wasFocused = FocusState != FocusState.Unfocused;
			if (!_imeFocusRequestInProgress)
			{
				_imeFocusOrigin = focusState;
			}
			base.UpdateFocusState(focusState);
			if (!_imeFocusRequestInProgress &&
				wasFocused &&
				focusState != FocusState.Unfocused &&
				!IsReadOnly)
			{
				ActivateImeForFocusOrigin(focusState);
			}
		}

		void IFocusRequestOriginHandler.OnFocusRequesting(FocusState focusState)
		{
			_imeFocusRequestInProgress = true;
			_imeWasFocusedBeforeRequest = FocusState != FocusState.Unfocused;
			_imeFocusOrigin = focusState;
		}

		void IFocusRequestOriginHandler.OnFocusRequested(FocusState focusState, bool succeeded)
		{
			_imeFocusRequestInProgress = false;
			if (succeeded &&
				_imeWasFocusedBeforeRequest &&
				FocusState != FocusState.Unfocused &&
				!IsReadOnly)
			{
				ActivateImeForFocusOrigin(focusState);
			}
			_imeWasFocusedBeforeRequest = false;
		}

		private void OnGotFocusManaged(RoutedEventArgs e)
		{
			_forceFocusedVisualState = false;
			UpdateSelectionHighlightColor();
			UpdateVisualState();
			if (!IsReadOnly)
			{
				StartCaret();
				StartImeSession();
			}
			else
			{
				_textBoxView?.OnFocusStateChanged(_imeFocusOrigin, suppressSoftwareKeyboard: true);
				UpdateDisplaySelection();
			}
		}

		private void OnLostFocusManaged(RoutedEventArgs e)
		{
			_forceFocusedVisualState = ShouldForceFocusedVisualState();
			if (_forceFocusedVisualState
				&& ShouldHideGrippersOnFlyoutOpening()
				&& CaretMode is RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing
					or RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing)
			{
				CaretMode = RichEditCaretDisplayMode.ThumblessCaretShowing;
			}
			_textBoxView?.OnFocusStateChanged(FocusState);
			UpdateSelectionHighlightColor();
			UpdateVisualState();
			if (!_forceFocusedVisualState)
			{
				EndImeSession();
				StopCaret();
				TextControlFlyoutHelper.CloseIfOpen(SelectionFlyout);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			OnManagedPlaceholderPresenterChanged(m_tpPlaceholderTextPresenter);
			ShowPlaceholderTextHandler(IsEmpty());
			AttachImeGeometryTracking();
			DispatchUpdateScrolling();
		}

		private protected override void OnUnloaded()
		{
			_placeholderTextChangedSubscription.Disposable = null;
			StopHeightAnimation();
			EndImeSession();
			DetachImeGeometryTracking();
			_gripperPresenter?.Hide();
			CaretMode = RichEditCaretDisplayMode.ThumblessCaretHidden;
			base.OnUnloaded();
		}

		private void AttachImeGeometryTracking()
		{
			if (!_isImeLayoutTrackingAttached)
			{
				LayoutUpdated += OnImeLayoutUpdated;
				_isImeLayoutTrackingAttached = true;
			}

			var scrollViewer = _contentElement as ScrollViewer;
			if (ReferenceEquals(_imeScrollViewer, scrollViewer))
			{
				return;
			}

			if (_imeScrollViewer is not null)
			{
				_imeScrollViewer.ViewChanged -= OnImeScrollViewerViewChanged;
			}

			_imeScrollViewer = scrollViewer;
			if (_imeScrollViewer is not null)
			{
				_imeScrollViewer.ViewChanged += OnImeScrollViewerViewChanged;
			}
		}

		private void DetachImeGeometryTracking()
		{
			if (_isImeLayoutTrackingAttached)
			{
				LayoutUpdated -= OnImeLayoutUpdated;
				_isImeLayoutTrackingAttached = false;
			}

			if (_imeScrollViewer is not null)
			{
				_imeScrollViewer.ViewChanged -= OnImeScrollViewerViewChanged;
				_imeScrollViewer = null;
			}
		}

		private void OnImeLayoutUpdated(object? sender, object args)
			=> ImeSessionCoordinator.UpdateSession(this, ImeSessionUpdate.TextAndSelection);

		private void OnImeScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
			=> ImeSessionCoordinator.UpdateSession(this, ImeSessionUpdate.TextAndSelection);

		protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e)
		{
			base.OnBringIntoViewRequested(e);

			if ((e.TargetElement is null || e.TargetElement == this)
				&& FocusState != FocusState.Unfocused
				&& !Document.HasPendingDisplayUpdates
				&& _contentElement is ScrollViewer { VerticalScrollMode: ScrollMode.Disabled }
				&& _textBoxView?.DisplayBlock is { } displayBlock)
			{
				var caret = _bringIntoViewTargetIndex ?? GetActiveSelectionIndex();
				var caretRect = displayBlock.ParsedText.GetRectForIndex(caret);
				caretRect = caretRect with
				{
					Width = Math.Max(TextBlock.CaretThickness, caretRect.Width),
				};
				e.TargetRect = displayBlock.TransformToVisual(this).TransformBounds(caretRect);
			}
		}

		protected override void OnFontSizeChanged(double oldValue, double newValue)
		{
			base.OnFontSizeChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
			DispatchUpdateScrolling();
		}

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			base.OnFontFamilyChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
			DispatchUpdateScrolling();
		}

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			base.OnFontStyleChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
			DispatchUpdateScrolling();
		}

		private protected override void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue)
		{
			base.OnFontStretchChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
			DispatchUpdateScrolling();
		}

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			base.OnFontWeightChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
			DispatchUpdateScrolling();
		}

		private void UpdateSelectionHighlightColor()
		{
			if (_textBoxView is not { } view)
			{
				return;
			}

			var brush = FocusState == FocusState.Unfocused && !_forceFocusedVisualState
				? SelectionHighlightColorWhenNotFocused ?? SelectionHighlightColor
				: SelectionHighlightColor;
			view.OnSelectionHighlightColorChanged(brush ?? DefaultBrushes.SelectionHighlightColor);
			UpdateDisplaySelection();
		}

		#region ITextBoxViewHost

		string ITextBoxViewHost.Text => GetPlainTextContent();

		TextAlignment ITextBoxViewHost.TextAlignment => GetAlignment();

		ContentControl? ITextBoxViewHost.ContentElement => _contentElement;

		FontFamily ITextBoxViewHost.FontFamily => _document?.IsMathMode == true
			? new FontFamily(global::Microsoft.UI.Text.RichEditTextDocument.MathRenderingFontFamilyName)
			: FontFamily;

		string ITextBoxViewHost.ProcessTextInput(string newText, int selectionStart, int selectionLength)
		{
			TryUpdateTextFromNative(newText, selectionStart, selectionLength);
			return GetPlainTextContent();
		}

		// Interactive IME composition state lives in RichEditBox.IME.skia.cs; the shared DisplayBlock
		// reads these to render the composition underline over the active (unresolved) preedit region.
		bool ITextBoxViewHost.IsComposing => IsComposing;

		int ITextBoxViewHost.CompositionUnderlineStart => _compositionStartIndex + _compositionResolvedLength;

		int ITextBoxViewHost.CompositionUnderlineLength => Math.Max(0, _compositionLength - _compositionResolvedLength);

		// When the paragraph model projects a uniform alignment onto the DisplayBlock
		// (see ApplyParagraphAlignment), report the alignment as explicitly set so the shared TextBlock
		// uses DisplayBlock.TextAlignment instead of deferring to the default. Otherwise fall back to the
		// control-level TextAlignment DP precedence.
		bool ITextBoxViewHost.IsTextAlignmentSetToDefault =>
			_paragraphAlignmentOverride is null
			&& ((DependencyObject)this)
				.GetCurrentHighestValuePrecedence(TextAlignmentProperty) is DependencyPropertyValuePrecedences.DefaultValue;

		#endregion

		#region ITextSelectionGripperHost

		TextBlock ITextSelectionGripperHost.GripperTextSurface => _textBoxView!.DisplayBlock;

		Rect ITextSelectionGripperHost.GripperClipBounds => this.GetAbsoluteBoundsRect();

		GripperMode ITextSelectionGripperHost.GripperMode => CaretMode switch
		{
			RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing => GripperMode.EndOnly,
			RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing => GripperMode.Both,
			_ => GripperMode.Hidden,
		};

		int ITextSelectionGripperHost.SelectionLowerIndex => _selection.start;

		int ITextSelectionGripperHost.SelectionUpperIndex => _selection.start + _selection.length;

		void ITextSelectionGripperHost.SetGripperSelection(int start, int end)
			=> SetInteractiveSelection(start, end - start);

		void ITextSelectionGripperHost.MoveGripperCaret(int index)
			=> SetInteractiveSelection(index, 0);

		void ITextSelectionGripperHost.ScrollForGripper(bool isEndGripper)
			=> UpdateScrollingToIndex(isEndGripper
				? _selection.start + _selection.length
				: _selection.start);

		void ITextSelectionGripperHost.OnGripperPressed()
			=> DismissSelectionFlyoutForPointerPress();

		void ITextSelectionGripperHost.RequestGripperContextMenu(PointerRoutedEventArgs args)
		{
			var contextArgs = new ContextRequestedEventArgs();
			contextArgs.SetGlobalPoint(args.GetCurrentPoint(null).Position);
			OnContextRequested(this, contextArgs);
		}

		void ITextSelectionGripperHost.QueueGripperSelectionFlyout(PointerRoutedEventArgs args, bool allowEmptySelection)
			=> QueueUpdateSelectionFlyoutVisibility(args.Pointer.PointerDeviceType, args.GetCurrentPoint(this).Position);

		void ITextSelectionGripperHost.OnGripperTapped(PointerPoint press, int anchorIndex)
			=> TouchTapAtIndex(anchorIndex);

		private int GetActiveSelectionIndex()
			=> _selection.selectionEndsAtTheStart
				? _selection.start
				: _selection.start + _selection.length;

		private void UpdateScrolling()
			=> UpdateScrollingToIndex(GetActiveSelectionIndex());

		private void DispatchUpdateScrolling()
		{
			if (_pendingUpdateScrolling)
			{
				return;
			}

			_pendingUpdateScrolling = true;
			if (!DispatcherQueue.TryEnqueue(() =>
			{
				_pendingUpdateScrolling = false;
				if (_pendingScrollingTargetIndex is { } targetIndex)
				{
					_pendingScrollingTargetIndex = null;
					UpdateScrollingToIndex(targetIndex);
				}
				else
				{
					UpdateScrolling();
				}
			}))
			{
				_pendingUpdateScrolling = false;
			}
		}

		private void UpdateScrollingToIndex(int index)
		{
			if (Document.HasPendingDisplayUpdates || !m_ensureRectVisibleEnabled)
			{
				_pendingScrollingTargetIndex = index;
				return;
			}

			if (_contentElement is not ScrollViewer scrollViewer || _textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return;
			}

			var caretRect = displayBlock.ParsedText.GetRectForIndex(index) with { Width = TextBlock.CaretThickness };
			var horizontalOffset = Math.Min(scrollViewer.HorizontalOffset, caretRect.Left);
			horizontalOffset = Math.Max(horizontalOffset, Math.Ceiling(caretRect.Right - scrollViewer.ViewportWidth + TextBlock.CaretThickness));
			var verticalOffset = Math.Min(scrollViewer.VerticalOffset, caretRect.Top);
			verticalOffset = Math.Max(verticalOffset, caretRect.Bottom - scrollViewer.ViewportHeight);
			scrollViewer.ChangeView(horizontalOffset, verticalOffset, null);

			if (FocusState != FocusState.Unfocused && scrollViewer.VerticalScrollMode == ScrollMode.Disabled)
			{
				_bringIntoViewTargetIndex = index;
				try
				{
					StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
				}
				finally
				{
					_bringIntoViewTargetIndex = null;
				}
			}
		}

		internal (CaretWithStemAndThumb start, CaretWithStemAndThumb end)? SelectionGrippersForTesting
			=> _gripperPresenter?.VisibleGrippersForTesting;

		internal bool IsHeightAnimationRunningForTesting => m_isAnimatingHeight;

		private void InvalidateView() => _textBoxView?.DisplayBlock.InvalidateMeasure();

		private UIElement? FindMaterializedHeaderPresenter()
		{
			if (GetTemplateRoot() is not UIElement root)
			{
				return null;
			}
			if (NameScope.GetNameScope(root)?.FindName("HeaderContentPresenter") is UIElement presenter
				&& presenter is not ElementStub)
			{
				return presenter;
			}

			// Runtime-loaded templates can expose realized parts through the tree rather than a registered namescope.
			return FindRealizedPart(root);

			UIElement? FindRealizedPart(UIElement element)
			{
				if (element is ElementStub
					|| element.GetTemplatedParent() is { } parent && !ReferenceEquals(parent, this))
				{
					return null;
				}
				if (element is FrameworkElement { Name: "HeaderContentPresenter" })
				{
					return element;
				}
				for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
				{
					if (VisualTreeHelper.GetChild(element, i) is UIElement child
						&& FindRealizedPart(child) is { } found)
					{
						return found;
					}
				}
				return null;
			}
		}

		private void OnManagedPlaceholderPresenterChanged(UIElement? presenter)
		{
			_placeholderTextChangedSubscription.Disposable = null;
			if (presenter is TextBlock textBlock)
			{
				// TemplateBinding updates the child after the owner's OnPropertyChanged2 callback.
				var token = textBlock.RegisterPropertyChangedCallback(
					TextBlock.TextProperty, (_, _) => ShowPlaceholderTextHandler(IsEmpty()));
				_placeholderTextChangedSubscription.Disposable = Disposable.Create(
					() => textBlock.UnregisterPropertyChangedCallback(TextBlock.TextProperty, token));
			}
		}

		private void StopHeightAnimation()
		{
			_heightAnimation?.Stop();
			_heightAnimation = null;
			m_storyboardCompletedToken.Disposable = null;
			m_isAnimatingHeight = false;
			EnableEnsureRectVisible();
		}

		private void OnManagedPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == FlowDirectionProperty)
			{
				_textBoxView?.SetFlowDirection();
			}
			else if (args.Property == DescriptionProperty && !m_isInitializing)
			{
				UpdateDescriptionVisibility(initialization: false);
			}
			else if (args.Property == PlaceholderTextProperty)
			{
				Uno.Helpers.UIElementAccessibilityHelper.NotifyTextControlStateChanged(this);
			}
		}

		private void EnableManagedCandidateWindowBoundsTracking()
		{
			try
			{
				ImeSessionCoordinator.Initialize();
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
			{
				typeof(RichEditBox).LogWarn()?.Warn("Candidate-window tracking is unavailable.", error);
			}
		}

		private void SetLinkCursor(InputSystemCursorShape shape)
			=> _contentElement?.SetProtectedCursor(InputSystemCursor.Create(shape));

		#endregion
	}
}
