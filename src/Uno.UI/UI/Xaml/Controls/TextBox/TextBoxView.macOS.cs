using CoreGraphics;
using System;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Uno.UI.Controls;
using Foundation;
using System.Collections;
using System.Linq;
using AppKit;
using _TextField = AppKit.NSTextField;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBoxView : _TextField, ITextBoxView, DependencyObject, IFontScalable
	{
		private TextBoxViewDelegate _delegate;
		private readonly WeakReference<TextBox> _textBox;

		public TextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);

			InitializeBinder();
			Initialize();
		}


		private void OnEditingChanged(object sender, EventArgs e)
		{
			OnTextChanged();
		}

		internal void OnChanged()
		{
			OnTextChanged();
		}

		public string Text
		{
			get => base.StringValue;

			set
			{
				// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
				value = value ?? string.Empty;
				if (base.StringValue != value)
				{
					base.StringValue = value;
					OnTextChanged();
				}
			}
		}

		private void OnTextChanged()
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(Text);
				SetTextNative(text);
			}
		}

		public void SetTextNative(string text) => Text = text;

		private void Initialize()
		{
			Delegate = _delegate = new TextBoxViewDelegate(_textBox, new WeakReference<TextBoxView>(this))
			{
				IsKeyboardHiddenOnEnter = true
			};

			DrawsBackground = false;
			Bezeled = false;
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			return IFrameworkElementHelper.SizeThatFits(this, base.SizeThatFits(size));
		}

		public void UpdateFont()
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				var newFont = NSFontHelper.TryGetFont((float)textBox.FontSize, textBox.FontWeight, textBox.FontStyle, textBox.FontFamily);

				if (newFont != null)
				{
					base.Font = newFont;
					this.InvalidateMeasure();
				}
			}
		}

		public Brush Foreground
		{
			get { return (Brush)GetValue(ForegroundProperty); }
			set { SetValue(ForegroundProperty, value); }
		}

		public bool HasMarkedText => throw new NotImplementedException();

		public nint ConversationIdentifier => throw new NotImplementedException();

		public NSRange MarkedRange => throw new NotImplementedException();

		public NSRange SelectedRange => throw new NotImplementedException();

		public NSString[] ValidAttributesForMarkedText => null;

		public static DependencyProperty ForegroundProperty { get ; } =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(TextBoxView),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBoxView)s).OnForegroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		public void OnForegroundChanged(Brush oldValue, Brush newValue)
		{
			var textBox = _textBox.GetTarget();

			if (textBox != null)
			{
				var scb = newValue as SolidColorBrush;

				if (scb != null)
				{
					this.TextColor = scb.Color;
				}
			}

			UpdateCaretColor();
		}

		private void UpdateCaretColor()
		{
			if (CurrentEditor is NSTextView textField && Foreground is SolidColorBrush scb)
			{
				textField.InsertionPointColor = scb.Color;
			}
		}

		public void RefreshFont()
		{
			UpdateFont();
		}

		public override bool BecomeFirstResponder()
		{
			UpdateCaretColor();
			return base.BecomeFirstResponder();
		}

		public void InsertText(NSObject insertString)
		{
			throw new NotImplementedException();
		}

		public void SetMarkedText(NSObject @string, NSRange selRange)
		{
			throw new NotImplementedException();
		}

		public void UnmarkText()
		{
			throw new NotImplementedException();
		}

		public NSAttributedString GetAttributedSubstring(NSRange range)
		{
			throw new NotImplementedException();
		}

		public CGRect GetFirstRectForCharacterRange(NSRange range)
		{
			throw new NotImplementedException();
		}

		public nuint GetCharacterIndex(CGPoint point)
		{
			throw new NotImplementedException();
		}
	}
}
