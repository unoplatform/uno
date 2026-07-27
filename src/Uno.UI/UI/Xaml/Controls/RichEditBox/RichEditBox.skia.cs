#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	// Uno-specific functional implementation of RichEditBox for Skia targets.
	//
	// This wires the control onto the shared managed text rendering surface (TextBoxView /
	// DisplayBlock, the same one TextBox uses through ITextBoxViewHost) and a functional Text Object
	// Model (RichEditTextDocument) with a character-formatting run model that is projected onto the
	// DisplayBlock's inlines (see RichEditBox.rendering.skia.cs).
	//
	// Standard RTF/streams, inline images, structured MathML with core math layout, paragraph
	// layout/list projection, browser editing, and TextBox-style touch/multi-tap selection are
	// supported. Advanced OpenType math glyph assembly remains outside this managed text engine.
	public partial class RichEditBox : ITextBoxViewHost, ITextSelectionGripperHost, IFocusRequestOriginHandler
	{
		private TextBoxView? _textBoxView;
		private TextSelectionGripperPresenter? _gripperPresenter;
		private ContentControl? _contentElement;
		private ContentPresenter? _headerPresenter;
		private UIElement? _placeholderTextPresenter;
		private global::Microsoft.UI.Text.RichEditTextDocument? _document;
		private bool _isInitializing = true;
		private bool _propertyChangedCallbacksRegistered;
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

		/// <summary>
		/// Gets an object that facilitates programmatic access to the text and formatting properties
		/// of the content of the <see cref="RichEditBox"/>.
		/// </summary>
		public global::Microsoft.UI.Text.RichEditTextDocument Document => _document ??= new global::Microsoft.UI.Text.RichEditTextDocument(this);

		/// <summary>
		/// Gets an object that enables you to access and modify the text in a rich edit control.
		/// </summary>
		public global::Microsoft.UI.Text.RichEditTextDocument TextDocument => Document;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			DetachImeGeometryTracking();

			// Ensures we don't keep a reference to a TextBoxView that exists in a previous template.
			_gripperPresenter?.Hide();
			_gripperPresenter = null;
			_textBoxView = null;

			_placeholderTextPresenter = GetTemplateChild(TextBoxConstants.PlaceHolderPartName) as UIElement;
			_contentElement = GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;
			_headerPresenter = GetTemplateChild(TextBoxConstants.HeaderContentPartName) as ContentPresenter;

			if (_contentElement is { })
			{
				_contentElement.SetProtectedCursor(Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.IBeam));
			}

			UpdateTextBoxView();
			InitializeTextBoxViewProperties();
			RegisterPropertyChangedCallbacks();
			RegisterPointerPressedHandler();

			UpdateHeaderPresenterVisibility();
			UpdatePlaceholderTextPresenterVisibility(GetPlainTextLength() == 0);
			UpdateDescriptionVisibility(initialization: true);

			_isInitializing = false;

			UpdateVisualState();
			DispatchUpdateScrolling();
			AttachImeGeometryTracking();
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

		private void RegisterPropertyChangedCallbacks()
		{
			if (_propertyChangedCallbacksRegistered)
			{
				return;
			}

			_propertyChangedCallbacksRegistered = true;

			// Ported intent from RichEditBox_Partial.cpp OnPropertyChanged2: keep the header and
			// placeholder presenters in sync when the relevant properties change after templating.
			RegisterPropertyChangedCallback(HeaderProperty, (s, _) => ((RichEditBox)s).OnHeaderChanged());
			RegisterPropertyChangedCallback(HeaderTemplateProperty, (s, _) => ((RichEditBox)s).OnHeaderChanged());
			RegisterPropertyChangedCallback(PlaceholderTextProperty, (s, _) => ((RichEditBox)s).OnPlaceholderTextChanged());
			RegisterPropertyChangedCallback(DescriptionProperty, (s, _) => ((RichEditBox)s).UpdateDescriptionVisibility(initialization: false));
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

		private void OnHeaderChanged()
		{
			if (!_isInitializing)
			{
				UpdateHeaderPresenterVisibility();
			}
		}

		private void OnPlaceholderTextChanged()
		{
			if (!_isInitializing)
			{
				UpdatePlaceholderTextPresenterVisibility(GetPlainTextLength() == 0);
			}
			Uno.Helpers.UIElementAccessibilityHelper.NotifyTextControlStateChanged(this);
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

			var textChange = PrepareTextChangedNotification(isContentChanging);

			RenderDocument();
			UpdatePlaceholderTextPresenterVisibility(GetPlainTextLength() == 0);
			(FrameworkElementAutomationPeer.FromElement(this) as RichEditBoxAutomationPeer)?.OnDocumentAccessibilityChanged();

			OnDocumentTextChangedInteractive();
			DispatchUpdateScrolling();
			QueueTextChangedNotification(textChange);
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

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
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

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
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
			AttachImeGeometryTracking();
			DispatchUpdateScrolling();
		}

		private protected override void OnUnloaded()
		{
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

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);
			UpdateVisualState();
		}

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

		internal override void UpdateVisualState(bool useTransitions = true)
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else if (FocusState != FocusState.Unfocused || _forceFocusedVisualState)
			{
				VisualStateManager.GoToState(this, "Focused", useTransitions);
			}
			else if (_isPointerOver)
			{
				VisualStateManager.GoToState(this, "PointerOver", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", useTransitions);
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

#if SUPPORTS_RTL
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == FrameworkElement.FlowDirectionProperty)
			{
				_textBoxView?.SetFlowDirection();
			}
		}
#endif

		#region ITextBoxViewHost

		string ITextBoxViewHost.Text => GetPlainTextContent();

		ContentControl? ITextBoxViewHost.ContentElement => _contentElement;

		FontFamily ITextBoxViewHost.FontFamily => _document?.IsMathMode == true
			? new FontFamily(global::Microsoft.UI.Text.RichEditTextDocument.MathFontFamilyName)
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
			&& (this as IDependencyObjectStoreProvider)?.Store
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

		void ITextSelectionGripperHost.QueueGripperSelectionFlyout(PointerRoutedEventArgs args)
			=> QueueUpdateSelectionFlyoutVisibility(args.Pointer.PointerDeviceType, args.GetCurrentPoint(this).Position);

		void ITextSelectionGripperHost.OnGripperTapped(PointerRoutedEventArgs args)
			=> TouchTap(args.GetCurrentPoint(_textBoxView!.DisplayBlock).Position, wasFocused: true);

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
			if (Document.HasPendingDisplayUpdates)
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

		#endregion
	}
}
