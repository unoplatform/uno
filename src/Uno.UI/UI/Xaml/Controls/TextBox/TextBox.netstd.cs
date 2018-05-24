using Uno.Extensions;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.UI.Text;
using Uno.Logging;
using Uno.UI.UI.Xaml.Documents;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : Control
	{
		private TextBoxView _textBoxView;
		private ContentControl _contentElement;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Ensures we don't keep a reference to a textBoxView that exists in a previous template
			_textBoxView = null;

			_placeHolder = this.GetTemplateChild(TextBoxConstants.PlaceHolderPartName) as IFrameworkElement;
			_contentElement = this.GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;

			if (_contentElement is ScrollViewer scrollViewer)
			{
				// We disable scrolling because the inner TextBoxView provides its own scrolling
				scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
				scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
				scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			}

			if (this.GetTemplateChild(TextBoxConstants.DeleteButtonPartName) is Button button)
			{
				_deleteButton = new WeakReference<Button>(button);
			}

			UpdateTextBoxView();
			InitializeProperties();
		}

		protected override bool IsDelegatingFocusToTemplateChild() => true; // _textBoxView
		protected override bool RequestFocus(FocusState state) => FocusTextView();
		partial void OnTextClearedPartial() => FocusTextView();
		protected virtual bool FocusTextView() => FocusManager.Focus(_textBoxView);

		partial void OnAcceptsReturnChangedPartial(DependencyPropertyChangedEventArgs e)
		{

		}


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

					_textBoxView = new TextBoxView(this, isMultiline: true)
						.Binding("Text", new Data.Binding()
						{
							Path = "Text",
							Source = this,
							Mode = BindingMode.TwoWay
						});

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
				else
				{
					if (_textBoxView != null && !_textBoxView.IsMultiline)
					{
						return;
					}

					_textBoxView = new TextBoxView(this, isMultiline: false)
						.Binding("Text", new Data.Binding()
						{
							Path = "Text",
							Source = this,
							Mode = BindingMode.TwoWay
						});

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
			}
		}

		protected void SetIsPassword(bool isPassword)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SetIsPassword(isPassword);
			}
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
			_textBoxView?.SetForeground(newValue);
		}

		partial void UpdateFontPartial(object sender)
		{
			_textBoxView?.SetFontSize(FontSize);
			_textBoxView?.SetFontStyle(FontStyle);
			_textBoxView?.SetFontWeight(FontWeight);
			_textBoxView?.SetFontFamily(FontFamily);
		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetTextWrapping(TextWrapping);
		}

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetTextAlignment(TextAlignment);
		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetAttribute("spellcheck", IsSpellCheckEnabled.ToString());
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
	}
}