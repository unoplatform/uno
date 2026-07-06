#nullable enable

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
	// TODO Uno: Pointer-driven caret placement/drag-selection, IME composition, and rich clipboard
	// (Copy/Cut/Paste) arrive in subsequent increments. Paragraph formatting, the remaining
	// ITextRange/ITextSelection breadth, RTF/streams, embedded images and MathML are also follow-ups.
	// Interactive keyboard editing (caret, selection, typing/navigation/undo) lives in
	// RichEditBox.editing.skia.cs. See plan for the sequencing.
	public partial class RichEditBox : ITextBoxViewHost
	{
		private TextBoxView? _textBoxView;
		private ContentControl? _contentElement;
		private ContentPresenter? _headerPresenter;
		private UIElement? _placeholderTextPresenter;
		private global::Microsoft.UI.Text.RichEditTextDocument? _document;
		private bool _isInitializing = true;
		private bool _propertyChangedCallbacksRegistered;

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
			RegisterPropertyChangedCallbacks();

			UpdateHeaderPresenterVisibility();
			UpdatePlaceholderTextPresenterVisibility(string.IsNullOrEmpty(GetPlainTextContent()));

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

		/// <summary>Returns the current plain-text content held by the TOM document.</summary>
		internal string GetPlainTextContent() => _document?.PlainText ?? string.Empty;

		/// <summary>
		/// Called by <see cref="global::Microsoft.UI.Text.RichEditTextDocument"/> after the document
		/// text changes so the control can re-render and refresh dependent visuals.
		/// </summary>
		internal void OnDocumentTextChanged()
		{
			RenderDocument();
			UpdatePlaceholderTextPresenterVisibility(string.IsNullOrEmpty(GetPlainTextContent()));

			// Raise TextChanging + TextChanged before the interactive selection sync (which may raise
			// SelectionChanging/SelectionChanged), matching WinUI's text-before-selection ordering for a
			// single edit.
			RaiseTextChangedIfNeeded();

			OnDocumentTextChangedInteractive();
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			UpdateVisualState();
			StartCaret();
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			UpdateVisualState();
			StopCaret();
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);
			UpdateVisualState();
		}

		private void UpdateVisualState()
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", true);
			}
			else if (FocusState != FocusState.Unfocused)
			{
				VisualStateManager.GoToState(this, "Focused", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}

		#region ITextBoxViewHost

		string ITextBoxViewHost.Text => GetPlainTextContent();

		ContentControl? ITextBoxViewHost.ContentElement => _contentElement;

		// TODO Uno: Run the real input pipeline (BeforeTextChanging/TextChanging/coercion) once the
		// shared editing engine is available. For now the text is passed through unchanged.
		string ITextBoxViewHost.ProcessTextInput(string newText) => newText;

		// RichEditBox does not yet drive interactive IME composition; the shared DisplayBlock reads
		// these to decide whether to render a composition underline (none for now).
		bool ITextBoxViewHost.IsComposing => false;

		int ITextBoxViewHost.CompositionUnderlineStart => 0;

		int ITextBoxViewHost.CompositionUnderlineLength => 0;

		bool ITextBoxViewHost.IsTextAlignmentSetToDefault =>
			(this as IDependencyObjectStoreProvider)?.Store
				.GetCurrentHighestValuePrecedence(TextAlignmentProperty) is DependencyPropertyValuePrecedences.DefaultValue;

		#endregion
	}
}
