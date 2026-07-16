#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Internal;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;
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
	// Standard RTF/streams, inline images, MathML interchange, and per-paragraph alignment are
	// supported. Full OpenType math layout, non-alignment paragraph layout, and advanced touch
	// selection UI remain follow-ups.
	public partial class RichEditBox : ITextBoxViewHost
	{
		private TextBoxView? _textBoxView;
		private ContentControl? _contentElement;
		private ContentPresenter? _headerPresenter;
		private UIElement? _placeholderTextPresenter;
		private global::Microsoft.UI.Text.RichEditTextDocument? _document;
		private bool _isInitializing = true;
		private bool _propertyChangedCallbacksRegistered;
		private bool _isPointerOver;

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

			// Ensures we don't keep a reference to a TextBoxView that exists in a previous template.
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

			UpdateHeaderPresenterVisibility();
			UpdatePlaceholderTextPresenterVisibility(string.IsNullOrEmpty(GetPlainTextContent()));
			UpdateDescriptionVisibility(initialization: true);

			_isInitializing = false;

			UpdateVisualState();
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
			view.SetTextAlignment();
			view.UpdateFont();
			view.DisplayBlock.IsSpellCheckEnabled = IsSpellCheckEnabled;
			view.UpdateProperties();
			UpdateSelectionHighlightColor();
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
				UpdatePlaceholderTextPresenterVisibility(string.IsNullOrEmpty(GetPlainTextContent()));
			}
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
			UpdatePlaceholderTextPresenterVisibility(string.IsNullOrEmpty(GetPlainTextContent()));

			OnDocumentTextChangedInteractive();
			QueueTextChangedNotification(textChange);
		}

		internal void OnDocumentMathModeChanged() => RenderDocument();

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			_forceFocusedVisualState = false;
			_textBoxView?.OnFocusStateChanged(FocusState);
			UpdateSelectionHighlightColor();
			UpdateVisualState();
			if (!IsReadOnly)
			{
				StartCaret();
				StartImeSession();
			}
			else
			{
				UpdateDisplaySelection();
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			_forceFocusedVisualState = ShouldForceFocusedVisualState();
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

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);
			UpdateVisualState();
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
		}

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			base.OnFontFamilyChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
		}

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			base.OnFontStyleChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
		}

		private protected override void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue)
		{
			base.OnFontStretchChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
		}

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			base.OnFontWeightChanged(oldValue, newValue);
			_textBoxView?.UpdateFont();
		}

		private void UpdateSelectionHighlightColor()
		{
			if (_textBoxView is not { } view)
			{
				return;
			}

			var brush = FocusState == FocusState.Unfocused
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

		// TODO Uno: Run the real input pipeline (BeforeTextChanging/TextChanging/coercion) once the
		// shared editing engine is available. For now the text is passed through unchanged.
		string ITextBoxViewHost.ProcessTextInput(string newText) => newText;

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
	}
}
