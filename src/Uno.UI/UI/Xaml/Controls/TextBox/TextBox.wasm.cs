using Uno.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : Control
	{
		private TextBoxView _textBoxView;

		internal TextBoxView TextBoxView => _textBoxView;

		private protected override bool IsDelegatingFocusToTemplateChild() => true; // _textBoxView
		partial void OnDeleteButtonClickPartial() => FocusTextView();
		internal bool FocusTextView() => FocusManager.FocusNative(_textBoxView);

		private void UpdateTextBoxView()
		{
			if (_contentElement != null)
			{
				if (AcceptsReturn || TextWrapping != TextWrapping.NoWrap)
				{
					if (_textBoxView != null && _textBoxView.IsMultiline)
					{
						return;
					}

					_textBoxView = new TextBoxView(this, isMultiline: true);

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
				else
				{
					if (_textBoxView != null && !_textBoxView.IsMultiline)
					{
						return;
					}

					_textBoxView = new TextBoxView(this, isMultiline: false);

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
			}
		}

		partial void InitializePropertiesPartial()
		{
			if (_header != null)
			{
				AddHandler(PointerReleasedEvent, (PointerEventHandler)OnHeaderClick, true);
			}
		}

		partial void OnTappedPartial()
		{
			FocusTextView();
		}

		private void OnHeaderClick(object sender, object args)
		{
			FocusTextView();
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
			_textBoxView?.SetForeground(newValue);
		}

		partial void UpdateFontPartial()
		{
			if (_textBoxView == null)
			{
				return;
			}

			_textBoxView.SetFontSize(FontSize);
			_textBoxView.SetFontStyle(FontStyle);
			_textBoxView.SetFontWeight(FontWeight);
			_textBoxView.SetFontFamily(FontFamily);
		}

		partial void OnTextWrappingChangedPartial()
		{
			_textBoxView?.SetTextWrappingAndTrimming(textWrapping: TextWrapping, textTrimming: null);
		}

		partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
		{
			_textBoxView?.SetTextAlignment(TextAlignment);
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush)
		{
			if (_textBoxView != null)
			{
				if (brush != null)
				{
					var color = brush.ColorWithOpacity;

					Color foregroundColor = Colors.White;

					// Check highlight color luminance to choose if black or white foreground is more appropriate
					if (color.Luminance > 0.5)
					{
						foregroundColor = Colors.Black;
					}

					SetSelectionHighlight(color, foregroundColor);
				}
				else
				{
					UnsetSelectionHighlight();
				}
			}
		}

		partial void OnIsSpellCheckEnabledChangedPartial(bool newValue)
		{
			_textBoxView?.SetAttribute("spellcheck", IsSpellCheckEnabled.ToString());
		}

		partial void OnIsEnabledChangedPartial(IsEnabledChangedEventArgs e)
		{
			ApplyEnabled(e.NewValue);
		}

		partial void OnIsReadonlyChangedPartial() => UpdateTextBoxViewIsReadOnly();

		partial void OnIsTabStopChangedPartial() => UpdateTextBoxViewIsReadOnly();

		private void UpdateTextBoxViewIsReadOnly()
		{
			var isNativeReadOnly = IsReadOnly || !IsTabStop;
			_textBoxView?.SetIsReadOnly(isNativeReadOnly);
		}

		partial void OnInputScopeChangedPartial(InputScope newValue)
		{
			ApplyInputScope(newValue);
		}

		private void ApplyEnabled(bool? isEnabled = null) => _textBoxView?.SetEnabled(isEnabled ?? IsEnabled);

		private void ApplyInputScope(InputScope scope) => _textBoxView?.SetInputScope(scope);

		partial void SelectPartial(int start, int length)
		{
			_textBoxView?.Select(start, length);
		}

		partial void SelectAllPartial() => Select(0, Text.Length);

		private protected override void OnContextFlyoutChanged(FlyoutBase oldValue, FlyoutBase newValue)
		{
			base.OnContextFlyoutChanged(oldValue, newValue);

			_textBoxView?.UpdateContextMenuEnabling();
		}

		public int SelectionStart
		{
			get => _textBoxView?.SelectionStart ?? 0;
			set => _textBoxView.Maybe(tbv => tbv.SelectionStart = value);
		}

		public int SelectionLength
		{
			get
			{
				if (_textBoxView == null)
				{
					return 0;
				}

				return _textBoxView.SelectionEnd - _textBoxView.SelectionStart;
			}
			set
			{
				if (_textBoxView == null)
				{
					return;
				}

				_textBoxView.SelectionEnd = _textBoxView.SelectionStart + value;
			}
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs e)
		{
			base.OnPointerMoved(e);
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);
		}

		protected override void OnRightTapped(RightTappedRoutedEventArgs e)
		{
			base.OnRightTapped(e);
		}
	}
}
